using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace main_server_api.tests;

// https://stackoverflow.com/a/47713709/11325184
public class XunitLogger<T> : ILogger<T>, IDisposable
{
	private readonly ITestOutputHelper _output;

	public XunitLogger(ITestOutputHelper output)
	{
		this._output = output;
	}
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
	{
		this._output.WriteLine(state?.ToString());
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return true;
	}

	public IDisposable BeginScope<TState>(TState state)
	{
		return this;
	}

	public void Dispose()
	{
	}
}