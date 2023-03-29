using main_server_api.Models.Runtime;
using Microsoft.Extensions.Configuration;
using ServicesLayer.Models.Runtime;
using vdb_main_server_api.Models.Runtime;

namespace vdb_main_server_api.Services;

/* Sigleton-сервис, служит для повышения уровня абстракции в других сервисах.
 * Обеспечивает получение настроек из appsettings и прочих файлов
 * с последующей их записью в соответствующие модели.
 */
public class SettingsProviderService
{
	protected readonly IConfiguration _configuration;
	protected readonly EnvironmentProvider _environment;

	public virtual IEnumerable<VpnNodeInfo> VpnNodeInfos =>
		_configuration.GetSection(nameof(VpnNodeInfos)).Get<VpnNodeInfoNotParsed[]>()?
		.Select(x => new VpnNodeInfo(x)) ?? Enumerable.Empty<VpnNodeInfo>();

	public virtual VpnNodesServiceSettings VpnNodesServiceSettings =>
		_configuration.GetSection(nameof(VpnNodesServiceSettings))
		.Get<VpnNodesServiceSettings>() ?? new();

	public virtual JwtServiceSettings JwtServiceSettings {
		get {
			var result = _configuration.GetSection(nameof(JwtServiceSettings))
				.Get<JwtServiceSettings>() ?? new();

			var generatedSig = _configuration.GetSection(nameof(GeneratedSigningKey))
				.Get<GeneratedSigningKey>();

			if(generatedSig is not null && !string.IsNullOrEmpty(generatedSig.SigningKeyBase64)) {
				result.SigningKeyBase64 = generatedSig.SigningKeyBase64;
			}

			return result;
		}
	}

	public virtual DeviceControllerSettings DeviceControllerSettings =>
		_configuration.GetSection(nameof(DeviceControllerSettings))
		.Get<DeviceControllerSettings>() ?? new();

	public VpnNodesStatusServiceSettings VpnNodesStatusServiceSettings =>
		_configuration.GetSection(nameof(VpnNodesStatusServiceSettings))
		.Get<VpnNodesStatusServiceSettings>() ?? new();

	public SettingsProviderService(IConfiguration configuration, EnvironmentProvider environmentProvider)
	{
		_configuration = configuration;
		_environment = environmentProvider;
	}
}

