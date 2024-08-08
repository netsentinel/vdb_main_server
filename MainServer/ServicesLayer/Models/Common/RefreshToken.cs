namespace ServicesLayer.Models.Common;

public class RefreshToken
{
	[Obsolete]
	public long Id { get; set; } // being used as an uuid actually

	public int IssuedToUser { get; set; }
	public long Entropy { get; set; }
}
