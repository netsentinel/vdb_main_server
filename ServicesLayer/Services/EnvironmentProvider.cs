using Microsoft.Extensions.Logging;

namespace vdb_main_server_api.Services;

public sealed class EnvironmentProvider
{
	// there is no need to access this variable's value from the code, it's just a reminder
	private const string ENV_GENERATE_JWT_SIG = "VDB_GENERATE_JWT_SIG";
	private const string ENV_JWT_SIGNING_KEY_B64 = "VDB_JWT_SIGNING_KEY";


	[Obsolete]
	public string? JWT_SIGNING_KEY_B64 { get; init; } = null;


	private readonly ILogger<EnvironmentProvider>? _logger;

	public EnvironmentProvider(ILogger<EnvironmentProvider>? logger)
	{
		_logger = logger;

		JWT_SIGNING_KEY_B64 = ParseStringValue(ENV_JWT_SIGNING_KEY_B64, 
			x=> Convert.TryFromBase64String(x, new byte[512/8], out _));
	}

	private string GetIncorrectIgnoredMessage(string EnvName)
	{
		return $"Incorrect value of {EnvName} environment variable was ignored.";
	}

	private bool? ParseBoolValue(string EnvName)
	{
		string? str = Environment.GetEnvironmentVariable(EnvName);
		if (str is not null)
		{
			if (str.Equals("true", StringComparison.InvariantCultureIgnoreCase))
			{
				_logger?.LogInformation($"{EnvName}={true}.");
				return true;
			}
			if (str.Equals("false", StringComparison.InvariantCultureIgnoreCase))
			{
				_logger?.LogInformation($"{EnvName}={false}.");
				return false;
			}
			_logger?.LogWarning(GetIncorrectIgnoredMessage(EnvName));
		}
		_logger?.LogInformation($"{EnvName} was not present.");
		return null;
	}
	private int? ParseIntValue(string EnvName, int minValue = int.MinValue)
	{
		string? str = Environment.GetEnvironmentVariable(EnvName);
		if (str is not null)
		{
			if (int.TryParse(str, out int val))
			{
				_logger?.LogInformation($"{EnvName}={val}.");
				return val;
			}
			_logger?.LogWarning(GetIncorrectIgnoredMessage(EnvName));
		}
		_logger?.LogInformation($"{EnvName} was not present.");
		return null;
	}

	private string? ParseStringValue(string EnvName, Func<string, bool> valueValidator)
	{
		string? str = Environment.GetEnvironmentVariable(EnvName);
		if (str is not null)
		{
			if (valueValidator(str))
			{
				_logger?.LogInformation($"{EnvName}={str}.");
				return str;
			}
			_logger?.LogWarning(GetIncorrectIgnoredMessage(EnvName));
		}
		_logger?.LogInformation($"{EnvName} was not present.");
		return null;
	}
}


