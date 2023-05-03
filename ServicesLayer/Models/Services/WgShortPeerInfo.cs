namespace ServicesLayer.Models.Services;

public class WgShortPeerInfo
{
	public string PublicKey { get; set; }
	public string AllowedIps { get; set; }
	public int HandshakeSecondsAgo { get; set; } = int.MaxValue;

	public WgShortPeerInfo(string publicKey, string allowedIps, int handshakeSecondsAgo)
	{
		this.PublicKey = publicKey;
		this.AllowedIps = allowedIps;
		this.HandshakeSecondsAgo = handshakeSecondsAgo;
	}
}

