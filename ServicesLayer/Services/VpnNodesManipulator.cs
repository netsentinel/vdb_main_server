using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Models.NodeApi;
using System.Net.Http.Json;
using System.Collections.ObjectModel;

namespace ServicesLayer.Services;

/* Данный Singleton-сервис занимается выполнением запросов к VPN-нодам. 
 * Основные действия - добавить и удалить пир
 * 
 * Статус ноды определяется запросом к эндпоинту /api/status. Данный эндпоинт
 * МОЖЕТ вернуть HmacSha512 ключа авторизации из заголовка Authorize Basic KEY. 
 * Это является дополнительным слоем засчеты в связи с использованием self-signed x509,
 * хотя и весьма наивным. Данный сервис будет требовать данный Hmac, если секретный
 * ключ подписи был задан в файле секретов.
 * 
 * Запросы выыполняются стандартным классом HttpClient. Для каждого клиента
 * переопределен HttpClientHandler таким образом, чтобы не выбрасывалось исключение
 * при работе с self-signed сертификатами. Для запроса к каждой ноде создается отдельный
 * экземпляр клиента, которому задаётся Authorize заголовок.
 * 
 * Также определен JsonSerializerOptions _defaultJsonOptions, который должен использоваться
 * ВЕЗДЕ, где идет парсинг или сериализация, ибо дефолтные параметры не работают с camelCase,
 * при том, и что сука характерно, именно этот camelCase является дефолтным вариантом нейминга
 * сериализации ASP. Т.е. по дефотлу HttpClient из .NET не может прочитать тело ответа, который
 * был сгенерирован ASP.NET.
 * 
 * Уникальным идентификатором ноды служит её имя. Имя используется в словаре, из которого
 * извлекаются как данные ноды, так и статус, так и клиент для ноды.
 */
public sealed class VpnNodesManipulator
{
	#region consts and fields

	private const string protocol = @"https";
	private const string peersControllerPath = @"api/peers";
	private const string statusControllerPath = @"api/status";

	private readonly VpnNodesServiceSettings _settings;
	private readonly ILogger<VpnNodesManipulator> _logger;

	private readonly Dictionary<int, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> _idToNode;
	private readonly HttpClientHandler _httpDefaultHandler;
	private readonly JsonSerializerOptions _defaultJsonOptions;

	#endregion

	#region public properties
	public ReadOnlyDictionary<int, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> IdToNode
		=> _idToNode.AsReadOnly();
	#endregion

	#region constructor 

	public VpnNodesManipulator(SettingsProviderService settingsProvider, ILogger<VpnNodesManipulator> logger)
	{
		logger.LogInformation($"Creating {nameof(NodesCleanupBackgroundService)}...");

		this._settings = settingsProvider.VpnNodesServiceSettings;
		this._logger = logger;

		this._httpDefaultHandler = new HttpClientHandler();
		// TODO: Try to replace with 1.3 in .NET8
		this._httpDefaultHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
		this._httpDefaultHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
		this._httpDefaultHandler.ServerCertificateCustomValidationCallback // allow self-signed TLS-cert
			= (_, _, _, _) => true;

		this._idToNode = settingsProvider.VpnNodeInfos.ToDictionary(x => x.Id, x => {
			var client = new HttpClient(this._httpDefaultHandler);
			client.Timeout = TimeSpan.FromSeconds(15);
			client.DefaultRequestHeaders.Authorization
				= new AuthenticationHeaderValue("Key", x.SecretAccessKeyBase64);
			return (x, new VpnNodeStatus(), client);
		});

		this._defaultJsonOptions = new(JsonSerializerDefaults.Web);

		logger.LogInformation($"Created {nameof(NodesCleanupBackgroundService)}.");
	}

	#endregion

	#region helpers
	private string GetPeersPathForNode(VpnNodeInfo nodeInfo, bool withCleanup)
	=> $"{protocol}://{nodeInfo.IpAddress}:{nodeInfo.ApiTlsPort}/{peersControllerPath}" +
		(withCleanup ? @"?withCleanup=true" : string.Empty);
	private string GetStatusPathForNode(VpnNodeInfo nodeInfo)
		=> $"{protocol}://{nodeInfo.IpAddress}:{nodeInfo.ApiTlsPort}/{statusControllerPath}";
	private string GetAlternatePathForNode(VpnNodeInfo nodeInfo)
		=> $"{protocol}://{nodeInfo.IpAddress}:{nodeInfo.AlternateApiTlsPort}/{statusControllerPath}";

	public (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient) GetNodeById(int nodeId)
	{
		if(!this._idToNode.TryGetValue(nodeId, out var testedNode))
			throw new KeyNotFoundException($"Node with id \'{nodeId}\' was not found.");

		return testedNode;
	}
	#endregion

	#region request-sending functions

	public async Task<WgShortPeerInfo[]?> GetPeersFromNode(int nodeId, bool withCleanup = false)
	{
		var (nodeInfo, nodeStatus, httpClient) = GetNodeById(nodeId);

		try {
			return await httpClient!.GetFromJsonAsync<WgShortPeerInfo[]>(this.GetPeersPathForNode(nodeInfo, withCleanup), this._defaultJsonOptions);
		} catch(HttpRequestException) {
			return null;
		}
	}
	public async Task<AddPeerResponse?> AddPeerToNode(int nodeId, string peerPubkey)
	{
		var (nodeInfo, nodeStatus, httpClient) = GetNodeById(nodeId);

		try {
			var response = await httpClient.PutAsync(this.GetPeersPathForNode(nodeInfo, false),
				JsonContent.Create(new PeerActionRequest(peerPubkey), options: this._defaultJsonOptions));

			return response.IsSuccessStatusCode
				? await response.Content.ReadFromJsonAsync<AddPeerResponse>(this._defaultJsonOptions)
				: null;
		} catch(HttpRequestException) {
			return null;
		}
	}
	public async Task<bool> RemovePeerFromNode(int nodeId, string peerPubkey)
	{
		var (nodeInfo, nodeStatus, httpClient) = GetNodeById(nodeId);

		try {
			var response = await httpClient.PatchAsync(this.GetPeersPathForNode(nodeInfo, false),
				JsonContent.Create(new PeerActionRequest(peerPubkey), options: this._defaultJsonOptions));

			return response.IsSuccessStatusCode;
		} catch(HttpRequestException) {
			return false;
		}
	}
	public async Task<bool> PingNode(int nodeId)
	{
		var (nodeInfo, nodeStatus, httpClient) = GetNodeById(nodeId);

		var path = this.GetStatusPathForNode(nodeInfo);
		this._logger.LogInformation($"Pinging node \'{nodeInfo.Name}\'. Endpoint=\'{path}\'...");
		try {
			if(nodeInfo.ComputedKeyHmac is null) {
				var response = await httpClient.GetAsync(path);
				if(!response.IsSuccessStatusCode) {
					this._logger.LogWarning($"Node \'{nodeInfo.Name}\' responded with an unsuccessful status code: {response.StatusCode}.");
				}

				return response.IsSuccessStatusCode;
			} else {
				var response = await httpClient.GetFromJsonAsync<SecuredStatusResponse>(path);
				if(response is null) {
					return false;
				} else {
					if(response.AuthKeyHmacSha512Base64.Equals(nodeInfo.ComputedKeyHmac)) {
						return true;
					} else {
						this._logger.LogError($"Wrong HMAC was received from the node \'{nodeInfo.Name}\'.");
						return false;
					}
				}
			}
		} catch(Exception ex) {
			this._logger.LogWarning($"Problem accessing the node \'{nodeInfo.Name}\': {ex.Message}. {ex.InnerException?.Message + '.' ?? string.Empty}");
			return false;
		}
	}

	#endregion

	#region Private fields manipulation function
	
	public void SetNodeStatus(int nodeId, VpnNodeStatus status)
	{
		var (_, nodeStatus, _) = GetNodeById(nodeId);
		nodeStatus.IsActive = status.IsActive;
		nodeStatus.PeersCount = status.PeersCount;
	}

	#endregion
}
