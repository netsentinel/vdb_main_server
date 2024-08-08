using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Models.Services;
using System.Net.Http.Json;

namespace ServicesLayer.Services;

public sealed class VpnNodesStatusService : BackgroundService
{
	private readonly VpnNodesService _nodesService;
	private readonly VpnNodesStatusServiceSettings _settings;
	private readonly ILogger<VpnNodesStatusService> _logger;
	public IEnumerable<PublicNodeInfo> Statuses { get; private set; }

	public VpnNodesStatusService(VpnNodesService nodesService, SettingsProviderService settingsProvider, ILogger<VpnNodesStatusService> logger)
	{
		this._nodesService = nodesService;
		this._settings = settingsProvider.VpnNodesStatusServiceSettings;
		this._logger = logger;

		this.Statuses = Array.Empty<PublicNodeInfo>();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if(this._settings.ReCacheIntervalSeconds <= 0) {
			this._logger.LogInformation($"ExecuteAsync is disabled by settings.");
			return;
		}

		while(!stoppingToken.IsCancellationRequested) {
			var nodes = this._nodesService.NameToNode;
			var newList = new List<PublicNodeInfo>(nodes.Count);

			foreach(var (nodeInfo, nodeStatus, httpClient) in nodes.Values) {
				var clientsCount = 0;
				if(nodeStatus.IsActive) {
					try {
						var peers = await httpClient
							.GetFromJsonAsync<string[]>(this._nodesService.GetPeersPathForNode(nodeInfo, false));
						clientsCount = peers?.Length ?? 0;
					} catch { }
				}

				var toAdd = new PublicNodeInfo {
					Id = nodeInfo.Id,
					Name = nodeInfo.Name,
					IpAddress = nodeInfo.IpAddress.ToString(),
					WireguardPort = nodeInfo.WireguardPort,
					UserAccessLevelRequired = (int)nodeInfo.UserAccessLevelRequired,
					IsActive = nodeStatus.IsActive,
					ClientsConnected = clientsCount
				};

				this._logger.LogInformation($"Recached node[{toAdd.Id}]. " +
					$"Active: {toAdd.IsActive}. " +
					$"Clients count: {toAdd.ClientsConnected}.");

				newList.Add(toAdd);
			}

			this.Statuses = newList;

			await Task.Delay(this._settings.ReCacheIntervalSeconds * 1000);
		}
	}
}

