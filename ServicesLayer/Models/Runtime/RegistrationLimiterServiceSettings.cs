namespace ServicesLayer.Models.Runtime;

// TODO: implement into settings.json
public class RegistrationLimiterServiceSettings
{
	public int MaxRegsPerPeriod { get; init; } = 1000; //
	public int PeriodSeconds { get; init; } = 24 * 60 * 60; // 1 day
}
