using DataAccessLayer.Contexts;
using DataAccessLayer.Models;
using main_server_api.Models.Runtime;
using main_server_api.Models.UserApi.Application.Device;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using vdb_main_server_api.Services;

namespace main_server_api.Controllers;


/* Предполагается, что пользователь авторизуется на устройстве как и на сайте,
 * после чего использует свой access-токен для доступа сюда.
 * 
 * Endpoints:
 * PUT GetDevices - Список девайсов текущего юзера
 * 
 * PUT AddDevice(string wgPukey) => JwtDeviceResponse;
 */
[Authorize]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
	private readonly VpnContext _context;
	private readonly JwtService _jwtService;
	private readonly DeviceControllerSettings _settings;
	private readonly Dictionary<int, int> _accessLevelToDevicesLimit;
	private static CookieOptions? _jwtCookieOptions;


	public DeviceController(VpnContext context, JwtService jwtService, SettingsProviderService settingsProvider)
	{
		_context = context;
		_jwtService = jwtService;
		_settings = settingsProvider.DeviceControllerSettings;
		_accessLevelToDevicesLimit = _settings.AccessLevelToMaxDevices?
			.ToDictionary(x => x.AccessLevel, x => x.DevicesLimit) ?? new Dictionary<int, int>();
	}

	[NonAction]
	public int GetDevicesLimitForUser(User user)
	{
		var accessLevel = (int)user.GetAccessLevel();

		if(_accessLevelToDevicesLimit.TryGetValue(accessLevel, out var limit)) {
			return limit;
		} else {
			return accessLevel * 3 + 1;
		}
	}

	[HttpPut]
	public async Task<IActionResult> AddNewDevice(AddDeviceRequest request)
	{
		var userId = int.Parse(Request.HttpContext.User.FindFirstValue(
			nameof(DataAccessLayer.Models.User.Id))!);

		var found = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if(found is null) {
			return Problem(
				"Access JWT is valid but the user it was issued to is not found on the server.",
				statusCode: StatusCodes.Status410Gone);
		}

		if( found.UserDevicesIds.Count >= GetDevicesLimitForUser(found)) {
			return Conflict("Devices limit reached.");
		}

		var userDevice = new UserDevice { WgPubkey = request.WgPubkey };
		_context.UserDevices.Add(userDevice);
		await _context.SaveChangesAsync();
		found.UserDevicesIds.Add(userDevice.Id);
		await _context.SaveChangesAsync();

		return Ok(new AddDeviceResponse { Id = userDevice.Id });
	}
	



}
