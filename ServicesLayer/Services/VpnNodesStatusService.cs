using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using vdb_main_server_api.Models.Services;
using vdb_main_server_api.Services;
using System.Collections.ObjectModel;
using vdb_main_server_api.Models.Runtime;
using ServicesLayer.Models.Runtime;
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
		_nodesService = nodesService;
		_settings = settingsProvider.VpnNodesStatusServiceSettings;
		_logger = logger;

		Statuses = Array.Empty<PublicNodeInfo>();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if(_settings.ReCacheIntervalSeconds <= 0) {
			_logger.LogInformation($"ExecuteAsync is disabled by settings.");
			return;
		}

		while(!stoppingToken.IsCancellationRequested) {
			var nodes = _nodesService.NameToNode;
			var newList = new List<PublicNodeInfo>(nodes.Count);

			foreach(var (nodeInfo, nodeStatus, httpClient) in nodes.Values) {
				int clientsCount = 0;
				if(nodeStatus.IsActive) {
					try {
						var peers = await httpClient
							.GetFromJsonAsync<string[]>(_nodesService.GetPeersPathForNode(nodeInfo, false));
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

				_logger.LogInformation($"Recached node[{toAdd.Id}]. " +
					$"Active: {toAdd.IsActive}. " +
					$"Clients count: {toAdd.ClientsConnected}.");

				newList.Add(toAdd);
			}

			Statuses = newList;

			await Task.Delay(_settings.ReCacheIntervalSeconds * 1000);
		}
	}
}

