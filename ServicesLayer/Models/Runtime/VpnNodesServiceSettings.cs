namespace ServicesLayer.Models.Runtime;

public class VpnNodesServiceSettings
{
	public int NodesReviewIntervalSeconds { get; init; } = 60 * 60; // 1 hour
	public bool ReviewNodesOnesAtNight { get; init; } = true; // this tweak overrides previous, use UTC 0:00
	public int PingNodesIntervalSeconds { get; init; } = 60;

}
