using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicesLayer.Models.Common;
using ServicesLayer.Services;
using System.Security.Claims;

namespace main_server_api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class StatisticsController : ControllerBase
{
	[HttpGet]
	[Route("all")]
	public async Task<IActionResult> GetAll([FromServices] StatisticsService service)
	{
		if(!this.ParseAdminClaim()) return this.NotFound();

		return await Task.Run(() => this.Ok(service.EnpointToRequestsTime));
	}

	[HttpGet]
	[Route("count")]
	public async Task<IActionResult> GetCounts([FromServices] StatisticsService service)
	{
		var t = this.User.FindFirstValue(nameof(UserInfo.IsAdmin));
		if(!this.ParseAdminClaim()) return this.NotFound();

		return await Task.Run(() => this.Ok(service.EnpointToRequestsTime.ToDictionary(x => x.Key, x => x.Value.Count)));
	}
}
