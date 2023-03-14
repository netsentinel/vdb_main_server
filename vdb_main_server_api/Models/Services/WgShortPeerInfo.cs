namespace vdb_main_server_api.Models.Services;

public class WgShortPeerInfo
{
    public string PublicKey { get; set; }
    public string AllowedIps { get; set; }
    public int HandshakeSecondsAgo { get; set; } = int.MaxValue;

    public WgShortPeerInfo(string publicKey, string allowedIps, int handshakeSecondsAgo)
    {
        PublicKey = publicKey;
        AllowedIps = allowedIps;
        HandshakeSecondsAgo = handshakeSecondsAgo;
    }
}

