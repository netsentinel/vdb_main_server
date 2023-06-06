using ServicesLayer.Models.Runtime;
using ServicesLayer.Services;
using Xunit;
using Xunit.Abstractions;

namespace main_server_api.tests.Services;
public class RegistrationLimiterServiceTests
{
	private readonly ITestOutputHelper _output;
	private readonly XunitLogger<RegistrationLimiterService> _logger;
	private readonly RegistrationLimiterServiceSettings _defaultSettings;


	public RegistrationLimiterServiceTests(ITestOutputHelper output)
	{
		this._output = output;
		this._logger = new(output);
		this._defaultSettings = new();
	}


	[Fact]
	public void CanCreate()
	{
		RegistrationLimiterService s = new(this._defaultSettings, this._logger);
		Assert.NotNull(s);
	}

	[Fact]
	public void CanAllowAndDeny()
	{
		var settings = new RegistrationLimiterServiceSettings() { MaxRegsPerPeriod = 3, PeriodSeconds = 100 };
		RegistrationLimiterService s = new(settings, this._logger);
		Assert.True(s.CountAndAllow());
		Assert.True(s.CountAndAllow());
		Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());
	}

	[Fact]
	public async Task CanExecute1()
	{
		var settings = new RegistrationLimiterServiceSettings() { MaxRegsPerPeriod = 10, PeriodSeconds = 10 };
		RegistrationLimiterService s = new(settings, this._logger);
		await s.StartAsync(new(false));

		for(var i = 0; i < 10; i++) Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());

		await Task.Delay(TimeSpan.FromSeconds(11));

		for(var i = 0; i < 10; i++) Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());
	}


	[Fact]
	public async Task CanExecute2()
	{
		var settings = new RegistrationLimiterServiceSettings() { MaxRegsPerPeriod = 10 * 10, PeriodSeconds = 10 };
		RegistrationLimiterService s = new(settings, this._logger);
		await s.StartAsync(new(false));

		for(var i = 0; i < 10 * 10; i++) Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());

		await Task.Delay(TimeSpan.FromSeconds(11));

		for(var i = 0; i < 10 * 10; i++) Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());
	}


	[Fact]
	public async Task CanExecuteSeeLogs()
	{
		var settings = new RegistrationLimiterServiceSettings() {
			MaxRegsPerPeriod = 1000,
			PeriodSeconds = 24 * 60 * 60
		};

		RegistrationLimiterService s = new(settings, this._logger);
		await s.StartAsync(new(false));
		await s.StopAsync(new(true));

		/* Expected log:
		 * Step every (24*60*60)... = 3600.
		 * Drop per step: 1000 * (3600/(24*60*60)) = 41 <-> 42
		 */

		this._output.WriteLine(string.Empty);

		settings = new RegistrationLimiterServiceSettings() {
			MaxRegsPerPeriod = 1000,
			PeriodSeconds = 2100
		};

		s = new(settings, this._logger);
		await s.StartAsync(new(false));
		await s.StopAsync(new(true));

		/* Expected log:
		 * Step every 210.
		 * Drop per step: 1000 * ((2100/10)/(2100)) = 100
		 */
	}
}
