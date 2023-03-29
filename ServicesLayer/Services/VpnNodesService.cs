using Microsoft.AspNetCore.Http.Headers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataAccessLayer.Models;
using vdb_main_server_api.Models.Runtime;
using vdb_main_server_api.Models.Services;
using vdb_node_api.Models.NodeApi;
using vdb_main_server_api.Services;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Http.Json;
using vdb_node_api.Models.NodeApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Collections.ObjectModel;

namespace vdb_main_server_api.Services;


/* Данный Singleton-сервис занимается выполнением запросов к VPN-нодам. 
 * Основные действия - добавить и удалить пир,  получить список пиров и провести ревизию.
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
 * 
 * 
 */
public sealed class VpnNodesService : BackgroundService
{
	private const string protocol = @"https";
	private const string peersControllerPath = @"api/peers";
	private const string statusControllerPath = @"api/status";

	public string GetPeersPathForNode(VpnNodeInfo nodeInfo, bool withCleanup)
		=> $"{protocol}://{nodeInfo.IpAddress}:{nodeInfo.ApiTlsPort}/{peersControllerPath}" + 
			(withCleanup ? @"?withCleanup=true" : string.Empty);
	public string GetStatusPathForNode(VpnNodeInfo nodeInfo)
		=> $"{protocol}://{nodeInfo.IpAddress}:{nodeInfo.ApiTlsPort}/{statusControllerPath}";



	private readonly VpnNodesServiceSettings _settings;
	private readonly ILogger<VpnNodesService> _logger;

	private readonly Dictionary<string, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> _nameToNode;
	private readonly Dictionary<int, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> _idToNode;
	private readonly HttpClientHandler _httpDefaultHandler;
	private readonly JsonSerializerOptions _defaultJsonOptions;

	public ReadOnlyDictionary<string, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> NameToNode 
		=> _nameToNode.AsReadOnly();

	public VpnNodesService(SettingsProviderService settingsProvider, ILogger<VpnNodesService> logger)
	{
		_settings = settingsProvider.VpnNodesServiceSettings;
		_logger = logger;

		System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
		_httpDefaultHandler = new HttpClientHandler();
		_httpDefaultHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
		_httpDefaultHandler.ServerCertificateCustomValidationCallback // allow self-signed TLS-cert
			= (httpRequestMessage, cert, cetChain, policyErrors) => true;

		_nameToNode = settingsProvider.VpnNodeInfos.ToDictionary(x => x.Name, x =>
		{
			var client = new HttpClient(_httpDefaultHandler);
			client.DefaultRequestHeaders.Authorization
				= new AuthenticationHeaderValue("Basic", x.SecretAccessKeyBase64);
			return (x, new VpnNodeStatus(), client);
		});
		_idToNode = _nameToNode.Values.ToDictionary(x => x.nodeInfo.Id);

		_defaultJsonOptions = new(JsonSerializerDefaults.Web);
	}
	public string GetNodeNameById(int id) => _idToNode[id].nodeInfo.Name;



	public async Task CheckIsNodeAccessibleBackground(string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		testedNode.nodeStatus.IsActive = await CheckIsNodeAccessible(nodeName);
		_logger.LogInformation($"Node \'{testedNode.nodeInfo.Name}\' is accessible: {testedNode.nodeStatus.IsActive}.");
	}
	public async Task<bool> CheckIsNodeAccessible(string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		try
		{
			if (nodeInfo.ComputedKeyHmac is null)
			{
				return (await httpClient.GetAsync(GetStatusPathForNode(nodeInfo))).IsSuccessStatusCode;

			} else
			{
				var response = await httpClient.GetFromJsonAsync<SecuredStatusResponse>(GetStatusPathForNode(nodeInfo));
				if(response is null){
					return false;
				} else
				if(!nodeInfo.EnableStatusHmac) {
					return true;
				} else {
					if(response.AuthKeyHmacSha512Base64.Equals(nodeInfo.ComputedKeyHmac)) {
						return true;
					} else {
						_logger.LogError($"Wrong HMAC was received from the node \'{nodeInfo.Name}\'.");
						return false;
					}
				}

				//return response is not null && (
				//	!nodeInfo.EnableStatusHmac || response.AuthKeyHmacSha512Base64.Equals(nodeInfo.ComputedKeyHmac));
			}
		}
		catch (HttpRequestException)
		{
			return false;
		}
	}



	public async Task<WgShortPeerInfo[]?> GetPeersFromNode(string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		if (!nodeStatus.IsActive) return null; // throw or just return null ?

		try
		{
			return await httpClient!.GetFromJsonAsync<WgShortPeerInfo[]>(GetPeersPathForNode(nodeInfo,true), _defaultJsonOptions);
		}
		catch (HttpRequestException)
		{
			_ = CheckIsNodeAccessibleBackground(nodeInfo.Name); // not awaited, fire-and-forget
			return null;
		}
	}

	public async Task<AddPeerResponse?> AddPeerToNode(string peerPubkey, int nodeId)
		=> await AddPeerToNode(peerPubkey, GetNodeNameById(nodeId));
	public async Task<AddPeerResponse?> AddPeerToNode(string peerPubkey, string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		if (!nodeStatus.IsActive) return null;

		try
		{
			var response = await httpClient.PutAsync(GetPeersPathForNode(nodeInfo,false),
				JsonContent.Create(new PeerActionRequest(peerPubkey), options: _defaultJsonOptions));

			return !response.IsSuccessStatusCode ? null :
				await response.Content.ReadFromJsonAsync<AddPeerResponse>(_defaultJsonOptions);
		}
		catch (HttpRequestException)
		{
			_ = CheckIsNodeAccessibleBackground(nodeInfo.Name); // not awaited, fire-and-forget
			return null;
		}
	}

	public async Task<bool> RemovePeerFromNode(string peerPubkey, int nodeId)
		=> await RemovePeerFromNode(peerPubkey, GetNodeNameById(nodeId));
	public async Task<bool> RemovePeerFromNode(string peerPubkey, string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		if (!nodeStatus.IsActive) return false;

		try
		{
			var response = await httpClient.PatchAsync(GetPeersPathForNode(nodeInfo,false),
				JsonContent.Create(new PeerActionRequest(peerPubkey), options: _defaultJsonOptions));

			return response.IsSuccessStatusCode;
		}
		catch (HttpRequestException)
		{
			_ = CheckIsNodeAccessibleBackground(nodeInfo.Name); // not awaited, fire-and-forget
			return false;
		}
	}



	// use 0:00 UTC as update point if 'once at night' mode enabled
	public int GetDelayFromNowMs()
	{
		return _settings.ReviewNodesOnesAtNight ?
			(int)(DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow).TotalMilliseconds
			: _settings.NodesReviewIntervalSeconds * 1000;
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// every minute
		_ = PingAllNodesAsync(stoppingToken, _settings.PingNodesIntervalSeconds * 1000); // not awaited, fire-and-forget

		while (!stoppingToken.IsCancellationRequested)
		{
			foreach (var val in _nameToNode.Values)
			{
				var (nodeInfo, nodeStatus, _) = val;
				nodeStatus.PeersCount = (await GetPeersFromNode(nodeInfo.Name))?.Length ?? 0;
			}

			var delayMs = GetDelayFromNowMs();
			_logger.LogInformation($"Peers review is delayed for {delayMs / 1000}s. " +
				$"Once at night mode enabled: {_settings.ReviewNodesOnesAtNight}.");
			await Task.Delay(delayMs, stoppingToken);
		}
	}
	private async Task PingAllNodesAsync(CancellationToken cancellationToken, int millisecondsInterval)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			foreach (var node in _nameToNode.Values)
			{
				_ = CheckIsNodeAccessibleBackground(node.nodeInfo.Name); // not awaited, fire-and-forget
			}
			await Task.Delay(millisecondsInterval, cancellationToken);
		}
	}
}
