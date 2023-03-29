namespace vdb_node_api.Models.NodeApi;

public class AddPeerResponse
{
    public string AllowedIps { get; set; }
    public string InterfacePublicKey { get; set; }

    public AddPeerResponse(string allowedIps, string interfacePublicKey)
    {
        AllowedIps = allowedIps;
        InterfacePublicKey = interfacePublicKey;
    }
}
