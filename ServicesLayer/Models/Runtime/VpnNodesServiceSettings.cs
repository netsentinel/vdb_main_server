namespace vdb_main_server_api.Models.Runtime;

public class VpnNodesServiceSettings
{
	public int NodesReviewIntervalSeconds { get; set; } = 60 * 60; // 1 hour
	public bool ReviewNodesOnesAtNight { get; set; } = true; // this tweak overrides previous, use UTC 0:00

}
