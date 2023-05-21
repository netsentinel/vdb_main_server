using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicesLayer.Services;

/* Это, конечно, не leaky bucket, но работает нормально.
 * 
 * В переменную PeriondSeconds устанавливается лимит времени, которое 
 * вообще учитывает данный сервис. В целом, лучше применять сутки, так
 * как это невилирует колебания притока-отдока пользователей, проще
 * говоря невилирует час-пики регистраций.
 * 
 * В переменную MaxRegsPerPeriod устанавливается максимальное число
 * регистраций за данный период. Сами регистарции являются скользящими,
 * т.е. не происходит условного дропа данных в условные 0 часов 0 минут 
 * каждые сутки.
 * 
 * Для 10к регистраций к день и Coeff = 2, данный класс займет в памяти
 * 8 * (10*10^3) * 2 / (1024^2) = 0.15 МБ.
 */
public sealed class RegistrationLimiterService : BackgroundService
{
	//private readonly RegistrationLimiterServiceSettings _settings;
	private readonly ILogger<RegistrationLimiterService> _logger;

	private readonly int _maxPerPeriod;
	private readonly int _periodSeconds;
	private int _registrations;

	public RegistrationLimiterService(RegistrationLimiterServiceSettings settings, ILogger<RegistrationLimiterService> logger)
	{
		logger.LogInformation($"Creating {nameof(RegistrationLimiterService)}...");

		_maxPerPeriod = settings.MaxRegsPerPeriod;
		_periodSeconds = settings.PeriodSeconds;
		_logger = logger;

		_registrations = 0;

		_logger.LogInformation($"Created {nameof(RegistrationLimiterService)}.");
	}

	public bool CountAndAllow()
	{
		_logger.LogTrace($"Counting registration request. " +
			$"Current status: {_registrations}/{_maxPerPeriod}.");

		if(_registrations >= _maxPerPeriod) return false;

		_registrations++;
		return true;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation($"Staring {nameof(ExecuteAsync)}...");

		int stepS = _periodSeconds / 10;
		while(stepS > 3600) stepS = 3600;
#if RELEASE
		while(stepS < 5) stepS = 5;
#endif

		float dropedPartPerStep = stepS/ (float)_periodSeconds; // usually 0.01 <-> 1
		int dropedPerStep = (int)(_maxPerPeriod * dropedPartPerStep);

		_logger.LogInformation($"Step: every {stepS} seconds. " +
			$"Values droped per step: {dropedPerStep}. " +
			$"Totally steps per period: {_periodSeconds / stepS}." +
			$"Totally droped per period: {_maxPerPeriod}.");

		while(!stoppingToken.IsCancellationRequested) {
			_logger.LogInformation($"Begin drop...");

			var start = _registrations;
			_registrations -= (_registrations > dropedPerStep) ? dropedPerStep : _registrations;
			var final = _registrations;

			_logger.LogInformation($"Droped: {start-final}. Remain: {final}. " +
				$"{nameof(ExecuteAsync)} is delayed for {stepS} seconds.");

			await Task.Delay(TimeSpan.FromSeconds(stepS), stoppingToken);
		}
	}
}
