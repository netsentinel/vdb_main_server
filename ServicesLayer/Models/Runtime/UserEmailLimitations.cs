namespace ServicesLayer.Models.Runtime;
public class UserEmailLimitations
{
	public int MinimalDelayBetweenMailsSenconds { get; set; } = 86400 / 12;
}
