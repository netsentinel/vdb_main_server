//using Microsoft.Extensions.Logging;

//namespace ServicesLayer.Services;

//[Obsolete("Use json file for passing settigns to the application.")]
//public sealed class EnvironmentProvider
//{
//	[Obsolete("There is no need to access this variable from the code, it's just a reminder.")]
//	private const string ENV_GENERATE_JWT_SIG = "VDB_GENERATE_JWT_SIG";

//	public bool? GENERATE_JWT_SIG { get; init; } = null;

//	private readonly ILogger<EnvironmentProvider>? _logger;

//	public EnvironmentProvider(ILogger<EnvironmentProvider>? logger)
//	{
//		this._logger = logger;


//		this.GENERATE_JWT_SIG = this.ParseBoolValue(ENV_GENERATE_JWT_SIG);
//	}

//	private string GetIncorrectIgnoredMessage(string EnvName)
//	{
//		return $"Incorrect value of {EnvName} environment variable was ignored.";
//	}

//	private bool? ParseBoolValue(string EnvName)
//	{
//		var str = Environment.GetEnvironmentVariable(EnvName);
//		if(str is not null) {
//			if(str.Equals("true", StringComparison.InvariantCultureIgnoreCase)) {
//				this._logger?.LogInformation($"{EnvName}={true}.");
//				return true;
//			}
//			if(str.Equals("false", StringComparison.InvariantCultureIgnoreCase)) {
//				this._logger?.LogInformation($"{EnvName}={false}.");
//				return false;
//			}
//			this._logger?.LogWarning(this.GetIncorrectIgnoredMessage(EnvName));
//		}
//		this._logger?.LogInformation($"{EnvName} was not present.");
//		return null;
//	}
//	private int? ParseIntValue(string EnvName, int minValue = int.MinValue)
//	{
//		var str = Environment.GetEnvironmentVariable(EnvName);
//		if(str is not null) {
//			if(int.TryParse(str, out var val)) {
//				this._logger?.LogInformation($"{EnvName}={val}.");
//				return val;
//			}
//			this._logger?.LogWarning(this.GetIncorrectIgnoredMessage(EnvName));
//		}
//		this._logger?.LogInformation($"{EnvName} was not present.");
//		return null;
//	}

//	private string? ParseStringValue(string EnvName, Func<string, bool> valueValidator)
//	{
//		var str = Environment.GetEnvironmentVariable(EnvName);
//		if(str is not null) {
//			if(valueValidator(str)) {
//				this._logger?.LogInformation($"{EnvName}={str}.");
//				return str;
//			}
//			this._logger?.LogWarning(this.GetIncorrectIgnoredMessage(EnvName));
//		}
//		this._logger?.LogInformation($"{EnvName} was not present.");
//		return null;
//	}
//}


