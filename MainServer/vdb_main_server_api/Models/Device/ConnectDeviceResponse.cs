using ServicesLayer.Models.NodeApi;

namespace main_server_api.Models.Device;

public class ConnectDeviceResponse : AddPeerResponse
{
	public string AddedPeerPublicKey { get; set; }
	public string ServerIpAddress { get; set; }
	public int WireguardPort { get; set; }

	public ConnectDeviceResponse(AddPeerResponse peerResponse, string peerPubkey, string serverIp, int wgPort)
		: base(peerResponse.AllowedIps, peerResponse.InterfacePublicKey)
	{
		this.AddedPeerPublicKey = peerPubkey;
		this.ServerIpAddress = serverIp;
		this.WireguardPort = wgPort;
	}
	public ConnectDeviceResponse(string allowedIps, string interfacePublicKey, string peerPubkey, string serverIp, int wgPort)
	: base(allowedIps, interfacePublicKey)
	{
		this.AddedPeerPublicKey = peerPubkey;
		this.ServerIpAddress = serverIp;
		this.WireguardPort = wgPort;
	}
}
