using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Models.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ServicesLayer.Services;
public class EmailSendingService
{
	private readonly EmailSendingServiceSettings _settings;
	private readonly ILogger _logger;

	private readonly JsonSerializerOptions _defaultJsonOptions;
	private readonly HttpClientHandler _httpDefaultHandler;
	private readonly HttpClient _httpClient;

	public EmailSendingService(EmailSendingServiceSettings settings, ILogger<EmailSendingService> logger)
	{
		this._settings = settings;
		this._logger = logger;

		this._httpDefaultHandler = new HttpClientHandler();
		// TODO: Try to replace with 1.3 in .NET8
		this._httpDefaultHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
		this._httpDefaultHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
		this._httpDefaultHandler.ServerCertificateCustomValidationCallback // allow self-signed TLS-cert
			= (_, _, _, _) => true;

		this._httpClient = new HttpClient(this._httpDefaultHandler);
		this._httpClient.Timeout = TimeSpan.FromSeconds(10);
		this._httpClient.DefaultRequestHeaders.Authorization
			= new AuthenticationHeaderValue("Key", settings.MicroserviceKey);

		this._defaultJsonOptions = new(JsonSerializerDefaults.Web);
	}

	public async Task<HttpStatusCode> Send(SendMailRequest mail)
	{
		_logger.LogInformation($"Sending email. Sending HttpRequest to the " +
			$"microservice at \'{this._settings.MicroservicePutEndpoint}\'.");

		var result = await this._httpClient.PostAsync(this._settings.MicroservicePutEndpoint,
			JsonContent.Create(mail, options: this._defaultJsonOptions));
		return result.StatusCode;
	}
	public async Task<EmailLimitationsResponse?> GetLimits()
	{
		_logger.LogInformation($"Request email limitations info. Sending HttpRequest to the " +
			$"microservice at \'{this._settings.MicroserviceGetLimitsEndpoint}\'.");

		return await this._httpClient.GetFromJsonAsync<EmailLimitationsResponse>(
			this._settings.MicroserviceGetLimitsEndpoint);
	}
}
