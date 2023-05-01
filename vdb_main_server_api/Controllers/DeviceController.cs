using Dapper;
using DataAccessLayer.Contexts;
using DataAccessLayer.Models;
using main_server_api.Models.Device;
using main_server_api.Models.Runtime;
using main_server_api.Models.UserApi.Application.Device;
using main_server_api.Models.UserApi.Website.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using vdb_main_server_api.Services;
using static DataAccessLayer.Models.User;

namespace main_server_api.Controllers;


/* Предполагается, что пользователь авторизуется на устройстве как и на сайте,
 * после чего использует свой access-токен для доступа сюда.
 * 
 * Endpoints:
 * GET ListDevices - Список девайсов текущего юзера
 * 
 * PUT AddDevice(string wgPukey) => JwtDeviceResponse;
 */
[Authorize]
[Route("api/[controller]")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class DeviceController : ControllerBase
{
	private readonly VpnContext _context;
	private readonly VpnNodesService _nodesService;
	private readonly DeviceControllerSettings _settings;
	private readonly Dictionary<int, int> _accessLevelToDevicesLimit;
	private static CookieOptions? _jwtCookieOptions;


	public DeviceController(VpnContext context, VpnNodesService nodesService, SettingsProviderService settingsProvider)
	{
		_context = context;
		_nodesService = nodesService;
		_settings = settingsProvider.DeviceControllerSettings;
		_accessLevelToDevicesLimit = _settings.AccessLevelToMaxDevices?
			.ToDictionary(x => x.AccessLevel, x => x.DevicesLimit) ?? new Dictionary<int, int>();
	}

	[NonAction]
	public int GetDevicesLimit(User.AccessLevels userAccessLevel)
	{
		var accessLevel = (int)userAccessLevel;

		var result = _accessLevelToDevicesLimit.TryGetValue(accessLevel, out var limit) ?
			limit : accessLevel * 3 + 1; // lowest: 1, highest: 13

#if DEBUG
		return result * 5;
#else
		return result;
#endif
	}

	[HttpGet]
	public async Task<IActionResult> ListDevices()
	{
		return Ok(await _context.Devices
			.Where(x => x.UserId == this.ParseIdClaim()).ToListAsync());
	}

	[HttpPut]
	public async Task<IActionResult> AddNewDevice([FromBody][Required] AddDeviceRequest request)
	{
		if(!this.ValidatePubkey(request.WireguardPublicKey, 256 / 8)) {
			return BadRequest(ErrorMessages.WireguardPublicKeyFormatInvalid);
		}

		if(await _context.Devices.AnyAsync(x => x.WireguardPublicKey == request.WireguardPublicKey)) {
			return Conflict(ErrorMessages.WireguardPublicKeyAlreadyExists);
		}

		var userId = this.ParseIdClaim();
		var userAccessLevel = (await _context.Users.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == userId))?.GetAccessLevel();

		if(userAccessLevel is null) {
			return UnprocessableEntity(ErrorMessages.AccessJwtUserNotFound);
		}

		var devicesCount = await _context.Devices.CountAsync(x => x.UserId == userId);
		if(devicesCount >= GetDevicesLimit(userAccessLevel.Value)) {
			return Conflict(ErrorMessages.DevicesLimitReached);
		}

		var added = _context.Devices.Add(new UserDevice {
			UserId = userId,
			WireguardPublicKey = request.WireguardPublicKey,
			LastConnectedNodeId = null,
			LastSeenUtc = DateTime.UtcNow
		});

		await _context.SaveChangesAsync();
		return StatusCode(StatusCodes.Status201Created);
	}


	/* RFC 3986
	 * The query component contains non-hierarchical data that, along 
	 * with data in the path component, serves to identify a resource. But... 
	 * base64-encoded wg pubkey in url?
	 */
	[HttpPatch]
	public async Task<IActionResult> DeleteDevice([FromBody][Required] DeleteDeviceRequest request)
	{
		int userId = this.ParseIdClaim();

		var toDelete = await _context.Devices.FirstOrDefaultAsync(d =>
			d.WireguardPublicKey == request.WireguardPublicKey && d.UserId == userId);

		if(toDelete is null) {
			return NotFound();
		}

		if(toDelete.LastConnectedNodeId is not null) {
			// not awaited, fire-and-forget
			_ = _nodesService.RemovePeerFromNode(
				toDelete.WireguardPublicKey, toDelete.LastConnectedNodeId.Value);
		}

		_context.Remove(toDelete);
		await _context.SaveChangesAsync();

		return Accepted(); // because we did not await one of the calls above 
	}


	[HttpDelete]
	[Route("{pubkeyBase64Url}")]
	public async Task<IActionResult> DeleteDevice([Required][FromRoute] string pubkeyBase64Url)
	{
		var actualKey = pubkeyBase64Url
			.Replace('-', '+')
			.Replace('_', '/');

		return await this.DeleteDevice(new DeleteDeviceRequest { WireguardPublicKey = actualKey });
	}
}