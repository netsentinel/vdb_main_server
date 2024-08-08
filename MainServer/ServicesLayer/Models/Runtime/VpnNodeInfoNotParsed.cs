namespace ServicesLayer.Models.Runtime;


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

	/* This port is used in case of hosting main server on the same server as the node,
	 * so it cannot be requested to the docker directly. This port is being added to the nginx,
	 * so it can be requested by the domain name.
	 */
	public int? AlternateApiTlsPort { get; init; }
	public int UserAccessLevelRequired { get; init; }
}
