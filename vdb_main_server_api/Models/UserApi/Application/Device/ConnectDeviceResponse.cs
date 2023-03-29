using vdb_node_api.Models.NodeApi;

namespace main_server_api.Models.UserApi.Application.Device;

public class ConnectDeviceResponse :AddPeerResponse
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
}
