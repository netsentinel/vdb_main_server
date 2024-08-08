using Microsoft.AspNetCore.Mvc;
using ServicesLayer.Models.Common;
using ServicesLayer.Services;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;

namespace main_server_api.Middleware;

/* Поскольку при убийстве других сессий удаляются только их рефреш токены,
 * мы должны запретить все аксес токены, которые были выпущены после этого.
 * 
 * Да, с какой-то стороны это ломает смысл 
 */
public class SessionTerminatorMiddleware : IMiddleware
{
	private readonly SessionTerminatorService _sessionTerminatorService;

	public SessionTerminatorMiddleware(SessionTerminatorService sessionTerminatorService)
	{
		this._sessionTerminatorService = sessionTerminatorService;
	}

	public Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		var userIdStr = context.User.FindFirstValue(nameof(UserInfo.Id));

		if(userIdStr is not null)
		{
			var userId = int.Parse(userIdStr!);
			var iatUnixTimeStamp = int.Parse(context.User.FindFirstValue("iat")!);

			DateTime iatDT = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(iatUnixTimeStamp);

			if(!_sessionTerminatorService.ValidateIat(userId, iatDT))
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				return Task.CompletedTask;
			}
		}

		return next(context);
	}
}
