namespace ServicesLayer.Models.NodeApi;

public class PeerActionRequest
{
	public string PublicKey { get; set; }

	public PeerActionRequest(string publicKey)
	{
		this.PublicKey = publicKey;
	}

	public AddPeerResponse CreateResponse(string allowedIps, string interfacePublicKey)
	{
		return new(allowedIps, interfacePublicKey);
	}
}
