using Microsoft.AspNetCore.Http.Headers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using vdb_main_server_api.Models.Database;
using vdb_main_server_api.Models.Runtime;
using vdb_main_server_api.Models.Services;
using vdb_node_api.Models.Api;
using vdb_main_server_api.Services;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Http.Json;

namespace vdb_main_server_api.Services;

public sealed class VpnNodesService : BackgroundService
{
	private const string protocol = "https";
	private const string peersControllerPath = "api/peers";
	private const string statusControllerPath = "api/status";
	private string GetPeersPathForNode(VpnNodeInfo nodeInfo)
		=> $"{protocol}://{nodeInfo.IpAddress}:{nodeInfo.ApiTlsPort}/{peersControllerPath}";
	private string GetStatusPathForNode(VpnNodeInfo nodeInfo)
		=> $"{protocol}://{nodeInfo.IpAddress}:{nodeInfo.ApiTlsPort}/{statusControllerPath}";



	private readonly VpnNodesServiceSettings _settings;
	private readonly ILogger<VpnNodesService> _logger;

	private readonly Dictionary<string, (VpnNodeInfo nodeInfo, VpnNodeStatus nodeStatus, HttpClient httpClient)> _nameToNode;
	private readonly HttpClientHandler _httpDefaultHandler;
	private readonly JsonSerializerOptions _defaultJsonOptions;
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

		_defaultJsonOptions = new(JsonSerializerDefaults.Web);
	}



	public async Task CheckIsNodeAccessibleBackground(string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		testedNode.nodeStatus.IsActive = await CheckIsNodeAccessible(nodeName);
	}
	public async Task<bool> CheckIsNodeAccessible(string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		try
		{
			return (await httpClient.GetAsync(GetStatusPathForNode(nodeInfo))).IsSuccessStatusCode;
		}
		catch (HttpRequestException)
		{
			return false;
		}
		finally
		{
			_logger.LogInformation($"Node \'{nodeName}\' is accessible: {nodeStatus.IsActive}.");
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
			return await httpClient!.GetFromJsonAsync<WgShortPeerInfo[]>(GetPeersPathForNode(nodeInfo), _defaultJsonOptions);
		}
		catch (HttpRequestException)
		{
			_ = CheckIsNodeAccessibleBackground(nodeInfo.Name); // not awaited, fire-and-forget
			return null;
		}
	}
	public async Task<AddPeerResponse?> AddPeerToNode(string peerPubkey, string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		if (!nodeStatus.IsActive) return null;

		try
		{
			var response = await httpClient.PutAsync(GetPeersPathForNode(nodeInfo),
				JsonContent.Create(new PeerActionRequest(peerPubkey), options: _defaultJsonOptions));

			return response.StatusCode != HttpStatusCode.OK ? null :
				await response.Content.ReadFromJsonAsync<AddPeerResponse>(_defaultJsonOptions);
		}
		catch (HttpRequestException)
		{
			_ = CheckIsNodeAccessibleBackground(nodeInfo.Name); // not awaited, fire-and-forget
			return null;
		}
	}
	public async Task<bool> RemovePeerFromNode(string peerPubkey, string nodeName)
	{
		if (!_nameToNode.TryGetValue(nodeName, out var testedNode))
			throw new KeyNotFoundException($"Node with name \'{nodeName}\' was not found.");

		var (nodeInfo, nodeStatus, httpClient) = testedNode;

		if (!nodeStatus.IsActive) return false;

		try
		{
			var response = await httpClient.PatchAsync(GetPeersPathForNode(nodeInfo),
				JsonContent.Create(new PeerActionRequest(peerPubkey), options: _defaultJsonOptions));

			return response.StatusCode == HttpStatusCode.OK;
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
		_ = PingAllNodesAsync(stoppingToken, 60 * 1000); // like every minute

		while (!stoppingToken.IsCancellationRequested)
		{
			foreach (var val in _nameToNode.Values)
			{
				var (nodeInfo, nodeStatus, _) = val;
				nodeStatus.PeersCount = (await GetPeersFromNode(nodeInfo.Name))?.Length ?? 0;
			}
			await Task.Delay(GetDelayFromNowMs(), stoppingToken);
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
