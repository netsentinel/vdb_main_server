using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Models.Services;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace ServicesLayer.Services;

/* Сервис генерирует список нод для публичного API.
 * Помимо известный на момент генерации данных, опрашивает каждую ноду о числе пиров.
 * Любой эндпоинт, использующий данный сервис, крайне рекомендуется к кешированию.
 */
public sealed class NodesPublicInfoService
{
	private readonly VpnNodesManipulator _manipulator;
	private readonly ILogger<NodesPublicInfoService> _logger;

	public NodesPublicInfoService(VpnNodesManipulator manipulator, ILogger<NodesPublicInfoService> logger)
	{
		this._manipulator = manipulator;
		this._logger = logger;
	}

	public async Task<PublicNodeInfo[]> GenerateReport()
	{
		var nodes = this._manipulator.IdToNode;

		var currentIndex = 0;
		var result = new PublicNodeInfo[nodes.Count];

		foreach(var (nodeInfo, nodeStatus, httpClient) in nodes.Values) {
			var toAdd = new PublicNodeInfo {
				Id = nodeInfo.Id,
				Name = nodeInfo.Name,
				IpAddress = nodeInfo.IpAddress.ToString(),
				WireguardPort = nodeInfo.WireguardPort,
				UserAccessLevelRequired = (int)nodeInfo.UserAccessLevelRequired,
				IsActive = nodeStatus.IsActive,
				ClientsConnected = (await _manipulator.GetPeersFromNode(nodeInfo.Id))?.Length ?? 0
			};

			this._logger.LogInformation($"Recached node with Id={toAdd.Id}. " +
				$"Active: {toAdd.IsActive}. " +
				$"Clients: {toAdd.ClientsConnected}.");

			result[currentIndex++] = toAdd;
		}

		return result;
	}
}

