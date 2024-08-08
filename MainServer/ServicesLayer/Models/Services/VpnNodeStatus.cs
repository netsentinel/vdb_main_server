namespace ServicesLayer.Models.Services;

public class VpnNodeStatus
{
	public bool IsActive { get; set; }
	public int PeersCount { get; set; }

	public VpnNodeStatus(bool isActive = false, int peersCount = 0)
	{
		this.IsActive = isActive;
		this.PeersCount = peersCount;
	}
}
