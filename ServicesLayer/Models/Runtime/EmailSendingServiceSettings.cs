namespace ServicesLayer.Models.Runtime;
public class EmailSendingServiceSettings
{
	public string MicroservicePutEndpoint { get; init; } = null!;
	public string MicroserviceGetLimitsEndpoint { get; init; } = null!;
	public string MicroserviceKey { get; init; } = null!;
}
