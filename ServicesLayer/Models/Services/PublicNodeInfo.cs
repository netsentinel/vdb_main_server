namespace vdb_main_server_api.Models.Services;

public class PublicNodeInfo
{
	public int Id { get; init; }
	public string Name { get; init; }
	public string IpAddress { get; init; }
	public int WireguardPort { get; init; }
	public int UserAccessLevelRequired { get; init; }
	public bool IsActive { get; init; }
	public int ClientsConnected { get; init; }
}
