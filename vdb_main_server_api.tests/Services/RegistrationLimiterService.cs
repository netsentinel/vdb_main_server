using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		_output = output;
		_logger = new(output);
		_defaultSettings = new();
	}


	[Fact]
	public void CanCreate()
	{
		RegistrationLimiterService s = new(_defaultSettings, _logger);
		Assert.NotNull(s);
	}

	[Fact]
	public void CanAllowAndDeny()
	{
		var settings = new RegistrationLimiterServiceSettings() { MaxRegsPerPeriod = 3, PeriodSeconds = 100 };
		RegistrationLimiterService s = new(settings, _logger);
		Assert.True(s.CountAndAllow());
		Assert.True(s.CountAndAllow());
		Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());
	}

	[Fact]
	public async Task CanExecute1()
	{
		var settings = new RegistrationLimiterServiceSettings() { MaxRegsPerPeriod = 10, PeriodSeconds = 10 };
		RegistrationLimiterService s = new(settings, _logger);
		await s.StartAsync(new(false));

		for(int i = 0; i < 10; i++) Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());

		await Task.Delay(TimeSpan.FromSeconds(11));

		for(int i = 0; i < 10; i++) Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());
	}


	[Fact]
	public async Task CanExecute2()
	{
		var settings = new RegistrationLimiterServiceSettings() { MaxRegsPerPeriod = 10 * 10, PeriodSeconds = 10 };
		RegistrationLimiterService s = new(settings, _logger);
		await s.StartAsync(new(false));

		for(int i = 0; i < 10 * 10; i++) Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());

		await Task.Delay(TimeSpan.FromSeconds(11));

		for(int i = 0; i < 10 * 10; i++) Assert.True(s.CountAndAllow());
		Assert.False(s.CountAndAllow());
	}


	[Fact]
	public async Task CanExecuteSeeLogs()
	{
		var settings = new RegistrationLimiterServiceSettings() {
			MaxRegsPerPeriod = 1000,
			PeriodSeconds = 24 * 60 * 60
		};

		RegistrationLimiterService s = new(settings, _logger);
		await s.StartAsync(new(false));
		await s.StopAsync(new(true));

		/* Expected log:
		 * Step every (24*60*60)... = 3600.
		 * Drop per step: 1000 * (3600/(24*60*60)) = 41 <-> 42
		 */

		_output.WriteLine(string.Empty);

		settings = new RegistrationLimiterServiceSettings() {
			MaxRegsPerPeriod = 1000,
			PeriodSeconds = 2100
		};

		s = new(settings, _logger);
		await s.StartAsync(new(false));
		await s.StopAsync(new(true));

		/* Expected log:
		 * Step every 210.
		 * Drop per step: 1000 * ((2100/10)/(2100)) = 100
		 */
	}
}
