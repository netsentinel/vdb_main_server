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
public class SessionTerminatorServiceTests
{
	private readonly ITestOutputHelper _output;
	private readonly XunitLogger<SessionTerminatorService> _logger;
	private readonly RegistrationLimiterServiceSettings _defaultSettings;


	public SessionTerminatorServiceTests(ITestOutputHelper output)
	{
		this._output = output;
		this._logger = new(output);
		this._defaultSettings = new();
	}

	[Fact]
	public void CanCreate()
	{
		var instance = new SessionTerminatorService(10, _logger);

		Assert.NotNull(instance);
	}

	[Fact]
	public void CanGetNotAddedRecord()
	{
		var instance = new SessionTerminatorService(10, _logger);

		var notAddedValue = instance.GetUserMinimalJwtIat(123);

		Assert.Equal(DateTime.MinValue, notAddedValue);
	}

	[Fact]
	public void CanAddRecord()
	{
		var instance = new SessionTerminatorService(10, _logger);

		var now = DateTime.UtcNow;

		instance.SetUserMinimalJwtIat(123, now);

		var addedValue = instance.GetUserMinimalJwtIat(123);

		Assert.Equal(now, addedValue);
	}

	[Fact]
	public void CanPerformClear()
	{
		var instance = new SessionTerminatorService(1, _logger);

		int timesAdded = 0;
		for(int i = 0; i < 16384; i++)
		{
			timesAdded++;
			instance.SetUserMinimalJwtIat(i);
		}

		Assert.Equal(16384, timesAdded);
		Assert.Equal(16384, instance.GetCurrentCount());
		Assert.Equal(16384, instance.GetCurrentAddedSinceLastCleanup());

		Thread.Sleep(500);
		instance.SetUserMinimalJwtIat(16384); // this will be saved
		Thread.Sleep(500);
		instance.SetUserMinimalJwtIat(16385); // clean performs here

		instance.SetUserMinimalJwtIat(16386); // this is added after clean

		Assert.Equal(3, instance.GetCurrentCount());
		Assert.Equal(1, instance.GetCurrentAddedSinceLastCleanup());
	}

	[Fact]
	public void CanValidate()
	{
		var instance = new SessionTerminatorService(1, _logger);

		Assert.True(instance.ValidateIat(123, DateTime.UtcNow));

		instance.SetUserMinimalJwtIat(123);

		Assert.True(instance.ValidateIat(123, DateTime.UtcNow));

		Assert.False(instance.ValidateIat(123, DateTime.UtcNow.AddDays(-1)));

		Thread.Sleep(1000);

		instance.Clear();

		Assert.True(instance.ValidateIat(123, DateTime.UtcNow));
	}
}
