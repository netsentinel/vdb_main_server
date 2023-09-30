using Microsoft.Extensions.Configuration;
using ServicesLayer.Models.Common;
using ServicesLayer.Models.Runtime;

namespace ServicesLayer.Services;

/* Sigleton-сервис, служит для повышения уровня абстракции в других сервисах.
 * Обеспечивает получение настроек из appsettings и прочих файлов
 * с последующей их записью в соответствующие модели.
 */
public class SettingsProviderService
{
	protected readonly IConfiguration _configuration;

	public virtual IEnumerable<VpnNodeInfo> VpnNodeInfos =>
		this._configuration.GetSection(nameof(this.VpnNodeInfos)).Get<VpnNodeInfoNotParsed[]>()?
		.Select(x => new VpnNodeInfo(x)) ?? Enumerable.Empty<VpnNodeInfo>();

	public virtual VpnNodesServiceSettings VpnNodesServiceSettings =>
		this._configuration.GetSection(nameof(this.VpnNodesServiceSettings))
		.Get<VpnNodesServiceSettings>() ?? new();

	public virtual JwtServiceSettings JwtServiceSettings {
		get {
			var result = this._configuration.GetSection(nameof(this.JwtServiceSettings))
				.Get<JwtServiceSettings>() ?? new();

			var generatedSig = this._configuration.GetSection(nameof(GeneratedSigningKey))
				.Get<GeneratedSigningKey>();

			if(generatedSig is not null && !string.IsNullOrEmpty(generatedSig.SigningKeyBase64)) {
				result.SigningKeyBase64 = generatedSig.SigningKeyBase64;
			}

			return result;
		}
	}

	public virtual DeviceControllerSettings DeviceControllerSettings =>
		this._configuration.GetSection(nameof(this.DeviceControllerSettings))
		.Get<DeviceControllerSettings>() ?? new();

	public virtual VpnNodesStatusServiceSettings VpnNodesStatusServiceSettings =>
		this._configuration.GetSection(nameof(this.VpnNodesStatusServiceSettings))
		.Get<VpnNodesStatusServiceSettings>() ?? new();

	public virtual RegistrationLimiterServiceSettings RegistrationLimiterServiceSettings =>
		this._configuration.GetSection(nameof(this.RegistrationLimiterServiceSettings))
		.Get<RegistrationLimiterServiceSettings>() ?? new();

	public virtual UserEmailLimitations UserEmailLimitations =>
		this._configuration.GetSection(nameof(this.UserEmailLimitations))
		.Get<UserEmailLimitations>() ?? new();

	public virtual MetaValues MetaValues =>
		this._configuration.GetSection(nameof(this.MetaValues))
		.Get<MetaValues>() ?? new();

	public virtual EmailSendingServiceSettings EmailSendingServiceSettings
		=> this._configuration.GetSection(nameof(this.EmailSendingServiceSettings))
		.Get<EmailSendingServiceSettings>() ?? new();

	public virtual LinksInfo LinksInfo
		=> this._configuration.GetSection(nameof(this.LinksInfo))
		.Get<LinksInfo>() ?? new();

	public SettingsProviderService(IConfiguration configuration)
	{
		this._configuration = configuration;
	}
}

