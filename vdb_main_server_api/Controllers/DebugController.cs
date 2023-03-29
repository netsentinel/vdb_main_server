using DataAccessLayer.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vdb_main_server_api.Services;

namespace main_server_api.Controllers;



[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class DebugController :ControllerBase
{

	private readonly VpnContext _context;
	private readonly ILogger<AuthController> _logger;

	public DebugController(VpnContext context, ILogger<AuthController> logger)
	{
		if(DateTime.UtcNow > new DateTime(2023, 04, 10))
			throw new AggregateException();

		_context = context;
		this._logger = logger;
	}

	[HttpGet]
	[Route("users")]
	public async Task<IActionResult> GetAllUser()
	{
		return Ok(await _context.Users.ToListAsync());
	}

	[HttpGet]
	[Route("devices")]
	public async Task<IActionResult> GetAllDevices()
	{
		return Ok(await _context.Devices.ToListAsync());
	}
}
