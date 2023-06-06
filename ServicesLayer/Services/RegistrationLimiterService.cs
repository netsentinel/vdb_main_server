using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServicesLayer.Models.Runtime;

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

		this._maxPerPeriod = settings.MaxRegsPerPeriod;
		this._periodSeconds = settings.PeriodSeconds;
		this._logger = logger;

		this._registrations = 0;

		this._logger.LogInformation($"Created {nameof(RegistrationLimiterService)}.");
	}

	public bool CountAndAllow()
	{
		this._logger.LogTrace($"Counting registration request. " +
			$"Current status: {this._registrations}/{this._maxPerPeriod}.");

		if(this._registrations >= this._maxPerPeriod) return false;

		this._registrations++;
		return true;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		this._logger.LogInformation($"Staring {nameof(ExecuteAsync)}...");

		var stepS = this._periodSeconds / 10;
		while(stepS > 3600) stepS = 3600;
#if RELEASE
		while(stepS < 5) stepS = 5;
#endif

		var dropedPartPerStep = stepS / (float)this._periodSeconds; // usually 0.01 <-> 1
		var dropedPerStep = (int)(this._maxPerPeriod * dropedPartPerStep);

		this._logger.LogInformation($"Step: every {stepS} seconds. " +
			$"Values droped per step: {dropedPerStep}. " +
			$"Totally steps per period: {this._periodSeconds / stepS}." +
			$"Totally droped per period: {this._maxPerPeriod}.");

		while(!stoppingToken.IsCancellationRequested) {
			this._logger.LogInformation($"Begin drop...");

			var start = this._registrations;
			this._registrations -= (this._registrations > dropedPerStep) ? dropedPerStep : this._registrations;
			var final = this._registrations;

			this._logger.LogInformation($"Droped: {start - final}. Remain: {final}. " +
				$"{nameof(ExecuteAsync)} is delayed for {stepS} seconds.");

			await Task.Delay(TimeSpan.FromSeconds(stepS), stoppingToken);
		}
	}
}
