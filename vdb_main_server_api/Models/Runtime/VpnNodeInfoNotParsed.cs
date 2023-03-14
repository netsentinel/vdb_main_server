using System.Net;
using vdb_main_server_api.Models.Database;

namespace vdb_main_server_api.Models.Runtime;


public class VpnNodeInfoNotParsed
{
	public string Name { get; init; }
	public string IpAddress { get; init; }
	public string SecretAccessKeyBase64 { get; init; }
	public int WireguardPort { get; init; }
	public int ApiTlsPort { get; init; }
	public int UserAccessLevelRequired { get; init; }
}
