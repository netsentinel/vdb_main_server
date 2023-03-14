using System.Net;
using vdb_main_server_api.Models.Database;

namespace vdb_main_server_api.Models.Runtime;

public class VpnNodeInfo
{
	public string Name { get; init; }
	public IPAddress IpAddress { get; init; }
	public string SecretAccessKeyBase64 { get; init; }
	public int WireguardPort { get; init; }
	public int ApiTlsPort { get; init; }
	public User.AccessLevels UserAccessLevelRequired { get; init; }

	public VpnNodeInfo(string name, IPAddress ipAddress, string secretAccessKeyBase64, int wireguardPort = 51850, int apiTlsPort = 51851, int userAccessLevelRequired = 0)
	{
		this.Name = name ?? throw new ArgumentNullException(nameof(name));
		this.IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
		this.SecretAccessKeyBase64 = secretAccessKeyBase64 ?? throw new ArgumentNullException(nameof(secretAccessKeyBase64));
		this.WireguardPort = wireguardPort;
		this.ApiTlsPort = apiTlsPort;
		this.UserAccessLevelRequired = (User.AccessLevels)userAccessLevelRequired;
	}

	public VpnNodeInfo(VpnNodeInfoNotParsed notParsed)
	{
		this.Name = notParsed.Name;
		this.IpAddress = IPAddress.Parse(notParsed.IpAddress);
		this.SecretAccessKeyBase64 = notParsed.SecretAccessKeyBase64;
		this.WireguardPort = notParsed.WireguardPort;
		this.ApiTlsPort = notParsed.ApiTlsPort;
		this.UserAccessLevelRequired = (User.AccessLevels)notParsed.UserAccessLevelRequired;
	}

	public VpnNodeInfoNotParsed ToNotParsed()
	{
		return new VpnNodeInfoNotParsed
		{
			Name = this.Name,
			IpAddress = this.IpAddress.ToString(),
			SecretAccessKeyBase64 = this.SecretAccessKeyBase64,
			WireguardPort = this.WireguardPort,
			ApiTlsPort = this.ApiTlsPort,
			UserAccessLevelRequired = (int)this.UserAccessLevelRequired
		};
	}
}
