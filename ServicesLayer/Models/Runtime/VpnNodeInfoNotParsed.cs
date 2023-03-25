using System.Net;

namespace vdb_main_server_api.Models.Runtime;


public class VpnNodeInfoNotParsed
{
	public int Id { get; init; }
	public string Name { get; init; }
	public string IpAddress { get; init; }
	public string SecretAccessKeyBase64 { get; init; }
	public string? SecretHmacKeyBase64 { get; init; }
	public bool EnableStatusHmac { get; init; }
	public int WireguardPort { get; init; }
	public int ApiTlsPort { get; init; }
	public int UserAccessLevelRequired { get; init; }
}
