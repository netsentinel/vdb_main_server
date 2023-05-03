using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServicesLayer.Models.NodeApi;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Models.Services;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ServicesLayer.Services;


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
	public string GetAlternatePathForNode(VpnNodeInfo nodeInfo)
		=> $"{protocol}://{nodeInfo.IpAddress}:{nodeInfo.AlternateApiTlsPort}/{statusControllerPath}";



	private readonly VpnNodesServiceSettings _settings;
	private readonly ILogger<VpnNodesService> _logger;

	private readonly Dictionary<string, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> _nameToNode;
	private readonly Dictionary<int, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> _idToNode;
	private readonly HttpClientHandler _httpDefaultHandler;
	private readonly JsonSerializerOptions _defaultJsonOptions;

	public ReadOnlyDictionary<string, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> NameToNode
		=> this._nameToNode.AsReadOnly();

	public VpnNodesService(SettingsProviderService settingsProvider, ILogger<VpnNodesService> logger)
	{
		logger.LogInformation($"Creating {nameof(VpnNodesService)}...");

		this._settings = settingsProvider.VpnNodesServiceSettings;
		this._logger = logger;

		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		this._httpDefaultHandler = new HttpClientHandler();
		this._httpDefaultHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
		this._httpDefaultHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
		this._httpDefaultHandler.ServerCertificateCustomValidationCallback // allow self-signed TLS-cert
			= (httpRequestMessage, cert, cetChain, policyErrors) => true;

		this._nameToNode = settingsProvider.VpnNodeInfos.ToDictionary(x => x.Name, x => {
			var client = new HttpClient(this._httpDefaultHandler);
			client.Timeout = TimeSpan.FromSeconds(15);
			client.DefaultRequestHeaders.Authorization
				= new AuthenticationHeaderValue("Basic", x.SecretAccessKeyBase64);
			return (x, new VpnNodeStatus(), client);
		});
		this._idToNode = this._nameToNode.Values.ToDictionary(x => x.nodeInfo.Id);

		this._defaultJsonOptions = new(JsonSerializerDefaults.Web);

		logger.LogInformation($"Created {nameof(VpnNodesService)}.");
	}
	public string GetNodeNameById(int id) => this._idToNode[id].nodeInfo.Name;



	public async Task CheckIsNodeAccessibleBackground(string nodeName)
	{
		if(!this._nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		testedNode.nodeStatus.IsActive = await this.CheckIsNodeAccessible(nodeName, false);
		if(!testedNode.nodeStatus.IsActive)
			testedNode.nodeStatus.IsActive = await this.CheckIsNodeAccessible(nodeName, true);

		this._logger.LogInformation($"Node \'{testedNode.nodeInfo.Name}\' is accessible: {testedNode.nodeStatus.IsActive}.");
	}
	public async Task<bool> CheckIsNodeAccessible(string nodeName, bool useAlternatePort = false)
	{
		if(!this._nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		this._logger.LogInformation($"Starting querying the node \'{nodeName}\' status...");
		var path = useAlternatePort ? this.GetAlternatePathForNode(nodeInfo) : this.GetStatusPathForNode(nodeInfo);
		this._logger.LogInformation($"Node \'{nodeName}\' status path is {path}. Sending...");
		try {
			if(nodeInfo.ComputedKeyHmac is null) {
				this._logger.LogInformation($"Hmac is disabled for node \'{nodeName}\'.");
				var response = await httpClient.GetAsync(path);
				this._logger.LogInformation($"Got response from node \'{nodeName}\'.");
				if(!response.IsSuccessStatusCode) {
					this._logger.LogWarning($"Node responded with an unsuccessful status code: {response.StatusCode}.");
				}

				return response.IsSuccessStatusCode;
			} else {
				this._logger.LogInformation($"Hmac is enabled for node \'{nodeName}\'.");
				var response = await httpClient.GetFromJsonAsync<SecuredStatusResponse>(path);
				if(response is null) {
					return false;
				} else
				if(!nodeInfo.EnableStatusHmac) {
					return true;
				} else {
					if(response.AuthKeyHmacSha512Base64.Equals(nodeInfo.ComputedKeyHmac)) {
						return true;
					} else {
						this._logger.LogError($"Wrong HMAC was received from the node \'{nodeInfo.Name}\'.");
						return false;
					}
				}

				//return response is not null && (
				//	!nodeInfo.EnableStatusHmac || response.AuthKeyHmacSha512Base64.Equals(nodeInfo.ComputedKeyHmac));
			}
		} catch(Exception ex) {
			this._logger.LogWarning($"Problem accessing the node \'{nodeInfo.Name}\': {ex.Message}. {ex.InnerException?.Message + '.' ?? string.Empty}");
			return false;
		}
	}






	public async Task<WgShortPeerInfo[]?> GetPeersFromNode(string nodeName)
	{
		if(!this._nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		if(!nodeStatus.IsActive) return null; // throw or just return null ?

		try {
			return await httpClient!.GetFromJsonAsync<WgShortPeerInfo[]>(this.GetPeersPathForNode(nodeInfo, true), this._defaultJsonOptions);
		} catch(HttpRequestException) {
			_ = this.CheckIsNodeAccessibleBackground(nodeInfo.Name); // not awaited, fire-and-forget
			return null;
		}
	}

	public async Task<AddPeerResponse?> AddPeerToNode(string peerPubkey, int nodeId)
		=> await this.AddPeerToNode(peerPubkey, this.GetNodeNameById(nodeId));
	public async Task<AddPeerResponse?> AddPeerToNode(string peerPubkey, string nodeName)
	{
		if(!this._nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		if(!nodeStatus.IsActive) return null;

		try {
			var response = await httpClient.PutAsync(this.GetPeersPathForNode(nodeInfo, false),
				JsonContent.Create(new PeerActionRequest(peerPubkey), options: this._defaultJsonOptions));

			return !response.IsSuccessStatusCode ? null :
				await response.Content.ReadFromJsonAsync<AddPeerResponse>(this._defaultJsonOptions);
		} catch(HttpRequestException) {
			_ = this.CheckIsNodeAccessibleBackground(nodeInfo.Name); // not awaited, fire-and-forget
			return null;
		}
	}

	public async Task<bool> RemovePeerFromNode(string peerPubkey, int nodeId)
		=> await this.RemovePeerFromNode(peerPubkey, this.GetNodeNameById(nodeId));
	public async Task<bool> RemovePeerFromNode(string peerPubkey, string nodeName)
	{
		if(!this._nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		if(!nodeStatus.IsActive) return false;

		try {
			var response = await httpClient.PatchAsync(this.GetPeersPathForNode(nodeInfo, false),
				JsonContent.Create(new PeerActionRequest(peerPubkey), options: this._defaultJsonOptions));

			return response.IsSuccessStatusCode;
		} catch(HttpRequestException) {
			_ = this.CheckIsNodeAccessibleBackground(nodeInfo.Name); // not awaited, fire-and-forget
			return false;
		}
	}



	// use 0:00 UTC as update point if 'once at night' mode enabled
	public int GetDelayFromNowMs()
	{
		return this._settings.ReviewNodesOnesAtNight ?
			(int)(DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow).TotalMilliseconds
			: this._settings.NodesReviewIntervalSeconds * 1000;
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		this._logger.LogInformation($"Starting {nameof(ExecuteAsync)}...");
		this._logger.LogInformation($"Total nodes to be tracked: {this._nameToNode.Count}.");
		// every minute
		_ = this.PingAllNodesAsync(stoppingToken, this._settings.PingNodesIntervalSeconds * 1000); // not awaited, fire-and-forget

		while(!stoppingToken.IsCancellationRequested) {
			foreach(var val in this._nameToNode.Values) {
				var (nodeInfo, nodeStatus, _) = val;
				nodeStatus.PeersCount = (await this.GetPeersFromNode(nodeInfo.Name))?.Length ?? 0;
			}

			var delayMs = this.GetDelayFromNowMs();
			this._logger.LogInformation($"Peers review is delayed for {delayMs / 1000}s. " +
				$"Once at night mode enabled: {this._settings.ReviewNodesOnesAtNight}.");
			await Task.Delay(delayMs, stoppingToken);
		}

		this._logger.LogInformation($"Exiting {nameof(ExecuteAsync)}.");
	}
	private async Task PingAllNodesAsync(CancellationToken cancellationToken, int millisecondsInterval)
	{
		this._logger.LogInformation($"Starting {nameof(PingAllNodesAsync)}...");

		while(!cancellationToken.IsCancellationRequested) {
			foreach(var node in this._nameToNode.Values) {
				this._logger.LogInformation($"Pinging node \'{node.nodeInfo.Name}\'...");
				_ = this.CheckIsNodeAccessibleBackground(node.nodeInfo.Name); // not awaited, fire-and-forget
			}
			await Task.Delay(millisecondsInterval, cancellationToken);
		}

		this._logger.LogInformation($"Exiting {nameof(PingAllNodesAsync)}.");
	}
}
