using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Runtime;

namespace ServicesLayer.Services;


// В фоном режиме обновляет статус нод, собственных коллекций не содержит,
// сильно связан с VpnNodesManipulator
public sealed class NodesCleanupBackgroundService : BackgroundService
{
	private readonly VpnNodesManipulator _manipulator;
	private readonly VpnNodesServiceSettings _settings;
	private readonly ILogger<NodesCleanupBackgroundService> _logger;

	public NodesCleanupBackgroundService(VpnNodesManipulator manipulator, SettingsProviderService settingsProvider, ILogger<NodesCleanupBackgroundService> logger)
	{
		logger.LogInformation($"Creating {nameof(NodesCleanupBackgroundService)}...");

		this._manipulator = manipulator;
		this._settings = settingsProvider.VpnNodesServiceSettings;
		this._logger = logger;

		logger.LogInformation($"Created {nameof(NodesCleanupBackgroundService)}.");
	}


	// use 0:00 UTC as update point if 'once at night' mode enabled
	private int GetDelayFromNowMs()
	{
		return this._settings.ReviewNodesOnesAtNight
			? (int)(DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow).TotalMilliseconds
			: this._settings.NodesReviewIntervalSeconds * 1000;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		this._logger.LogInformation($"Starting {nameof(ExecuteAsync)}...");
		this._logger.LogInformation($"Total nodes to be in clean proccess: {this._manipulator.IdToNode.Count}.");

		while(!stoppingToken.IsCancellationRequested) {
			this._logger.LogInformation($"Starting nodes peers review...");
			foreach(var val in this._manipulator.IdToNode.Values) {
				// remember! never let the ExecuteAsync to throw!
				try {
					await this._manipulator.GetPeersFromWithCleanup(val.nodeInfo.Id);
				} catch { }
			}

			var delayMs = this.GetDelayFromNowMs();
			this._logger.LogInformation($"Peers review is delayed for {delayMs / 1000}s. " +
				$"The next review is planned on {DateTime.UtcNow.AddMilliseconds(delayMs).ToString(@"u")} UTC. " +
				$"Once at night mode enabled: {this._settings.ReviewNodesOnesAtNight}.");

			await Task.Delay(delayMs, stoppingToken);
		}

		this._logger.LogInformation($"Exiting {nameof(ExecuteAsync)}.");
	}
}
