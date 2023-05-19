using ServicesLayer.Services;

namespace main_server_api.Middleware;

public class StatisticsMiddleware : IMiddleware
{
	private StatisticsService _statisticsService;

	public StatisticsMiddleware(StatisticsService statisticsService)
	{
		_statisticsService = statisticsService;
	}

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		_statisticsService.Count(context.Request.Method + ' ' + context.Request.Path);
		await next.Invoke(context);
	}
}

