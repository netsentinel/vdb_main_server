namespace ServicesLayer.Models.Services;

public class EmailLimitationsResponse
{
	public int TotalPerDayLimit { get; init; }
	public int SendedLast24Hours { get; init; }
	public int CanSendImmediately { get; init; }
}
