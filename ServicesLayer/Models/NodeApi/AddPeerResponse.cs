namespace ServicesLayer.Models.NodeApi;

public class AddPeerResponse
{
	public string AllowedIps { get; set; }
	public string InterfacePublicKey { get; set; }

	public AddPeerResponse(string allowedIps, string interfacePublicKey)
	{
		this.AllowedIps = allowedIps;
		this.InterfacePublicKey = interfacePublicKey;
	}
}
