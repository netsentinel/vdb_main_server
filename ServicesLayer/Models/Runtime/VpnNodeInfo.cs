using DataAccessLayer.Models;
using System.Net;
using System.Security.Cryptography;

namespace ServicesLayer.Models.Runtime;

public class VpnNodeInfo
{
	public int Id { get; init; }
	public string Name { get; init; }
	public IPAddress IpAddress { get; init; }
	public string SecretAccessKeyBase64 { get; init; }
	public bool EnableStatusHmac { get; init; }
	public string? ComputedKeyHmac { get; init; }
	public int WireguardPort { get; init; }
	public int ApiTlsPort { get; init; }
	public int? AlternateApiTlsPort { get; init; }
	public User.AccessLevels UserAccessLevelRequired { get; init; }

	public VpnNodeInfo(int id, string name, IPAddress ipAddress, string secretAccessKeyBase64, bool enableStatusHmac,
		int wireguardPort = 51850, int apiTlsPort = 51851, int userAccessLevelRequired = 0)
	{
		this.Id = id;
		this.Name = name ?? throw new ArgumentNullException(nameof(name));
		this.IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
		this.SecretAccessKeyBase64 = secretAccessKeyBase64 ?? throw new ArgumentNullException(nameof(secretAccessKeyBase64));
		this.EnableStatusHmac = enableStatusHmac;
		this.WireguardPort = wireguardPort;
		this.ApiTlsPort = apiTlsPort;
		this.UserAccessLevelRequired = (User.AccessLevels)userAccessLevelRequired;
	}

	public VpnNodeInfo(VpnNodeInfoNotParsed notParsed)
	{
		this.Id = notParsed.Id;
		this.Name = notParsed.Name;
		this.IpAddress = IPAddress.Parse(notParsed.IpAddress);
		this.SecretAccessKeyBase64 = notParsed.SecretAccessKeyBase64;
		this.EnableStatusHmac = notParsed.EnableStatusHmac;
		this.ComputedKeyHmac = this.EnableStatusHmac && notParsed.SecretHmacKeyBase64 is not null
			? Convert.ToBase64String(HMACSHA512.HashData(
				Convert.FromBase64String(notParsed.SecretHmacKeyBase64),
				Convert.FromBase64String(notParsed.SecretAccessKeyBase64)))
			: null;
		this.WireguardPort = notParsed.WireguardPort;
		this.ApiTlsPort = notParsed.ApiTlsPort;
		this.AlternateApiTlsPort = notParsed.AlternateApiTlsPort;
		this.UserAccessLevelRequired = (User.AccessLevels)notParsed.UserAccessLevelRequired;
	}

	public VpnNodeInfoNotParsed ToNotParsed()
	{
		return new VpnNodeInfoNotParsed {
			Id = this.Id,
			Name = this.Name,
			IpAddress = this.IpAddress.ToString(),
			SecretAccessKeyBase64 = this.SecretAccessKeyBase64,
			WireguardPort = this.WireguardPort,
			ApiTlsPort = this.ApiTlsPort,
			UserAccessLevelRequired = (int)this.UserAccessLevelRequired
		};
	}
}
