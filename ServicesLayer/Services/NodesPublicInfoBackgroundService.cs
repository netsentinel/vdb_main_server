using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Models.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace ServicesLayer.Services;

/* Сервис генерирует список нод для публичного API.
 * Помимо известный на момент генерации данных, опрашивает каждую ноду о числе пиров.
 */
public sealed class NodesPublicInfoBackgroundService : BackgroundService
{
	private readonly VpnNodesManipulator _manipulator;
	private readonly ILogger<NodesPublicInfoBackgroundService> _logger;

	public NodesPublicInfoBackgroundService(VpnNodesManipulator manipulator, ILogger<NodesPublicInfoBackgroundService> logger)
	{
		this._manipulator = manipulator;
		this._logger = logger;
	}

	private PublicNodeInfo[] _lastReport;
	public ReadOnlyCollection<PublicNodeInfo> LastReport
		=> _lastReport.AsReadOnly();

	private async Task GenerateReportThenSwap()
	{
		this._logger.LogInformation($"Begin generating nodes report.");

		var nodes = this._manipulator.IdToNode;

		var currentIndex = 0;
		var result = new PublicNodeInfo[nodes.Count];

		foreach(var (nodeInfo, nodeStatus, httpClient) in nodes.Values) {
			int clientsCount;
			bool isActive;

			try {
				var response = (await _manipulator.GetPeersFromNode(nodeInfo.Id));
				isActive = response is not null;
				clientsCount = response?.Length ?? 0;
			} catch(Exception ex) {
				this._logger.LogWarning($"Problem accessing node with Id={nodeInfo.Id}: {ex.Message}. {ex.InnerException?.Message}");
				isActive = false;
				clientsCount = 0;
			}

			var toAdd = new PublicNodeInfo {
				Id = nodeInfo.Id,
				Name = nodeInfo.Name,
				IpAddress = nodeInfo.IpAddress.ToString(),
				WireguardPort = nodeInfo.WireguardPort,
				UserAccessLevelRequired = (int)nodeInfo.UserAccessLevelRequired,
				IsActive = isActive,
				ClientsConnected = clientsCount
			};

			this._logger.LogInformation($"Reported node with Id={toAdd.Id}. " +
				$"Active: {toAdd.IsActive}. " +
				$"Clients: {toAdd.ClientsConnected}.");

			result[currentIndex++] = toAdd;
		}

		_lastReport = result;
	}

	// TODO: create special settings for service
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while(!stoppingToken.IsCancellationRequested) {
			await GenerateReportThenSwap();
			await Task.Delay(60 * 1000);
		}
	}
}

