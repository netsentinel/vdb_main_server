using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicesLayer.Models.Common;
using ServicesLayer.Services;
using System.Configuration;

namespace main_server_api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class LinksController : ControllerBase
{
	private readonly LinksInfo _links;

	public LinksController(SettingsProviderService sp)
	{
		this._links = sp.LinksInfo;
	}

	[HttpGet]
	[Route("latest-release")]
	public async Task<IActionResult> GetLatestReleaseLink()
	{
		return await Task.FromResult(Ok(_links.LatestReleaseLink));
	}
}
