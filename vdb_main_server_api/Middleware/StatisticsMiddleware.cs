using ServicesLayer.Services;

namespace main_server_api.Middleware;

public class StatisticsMiddleware : IMiddleware
{
	private readonly StatisticsService _statisticsService;

	public StatisticsMiddleware(StatisticsService statisticsService)
	{
		this._statisticsService = statisticsService;
	}

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		this._statisticsService.Count(context.Request.Method + ' ' + context.Request.Path);
		await next.Invoke(context);
	}
}

