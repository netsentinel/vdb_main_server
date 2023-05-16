using DataAccessLayer.Contexts;
using DataAccessLayer.Models;
using main_server_api.Models.Device;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Services;
using System.ComponentModel.DataAnnotations;

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
	private static readonly CookieOptions? _jwtCookieOptions;


	public DeviceController(VpnContext context, VpnNodesService nodesService, SettingsProviderService settingsProvider)
	{
		this._context = context;
		this._nodesService = nodesService;
		this._settings = settingsProvider.DeviceControllerSettings;
		this._accessLevelToDevicesLimit = this._settings.AccessLevelToMaxDevices?
			.ToDictionary(x => x.AccessLevel, x => x.DevicesLimit * _settings.DevicesLimitMultiplier)
			?? new Dictionary<int, int>();
	}

	[NonAction]
	public int GetDevicesLimit(User.AccessLevels userAccessLevel)
	{
		var accessLevel = (int)userAccessLevel;

		var result = this._accessLevelToDevicesLimit.TryGetValue(accessLevel, out var limit) ?
			limit : accessLevel * 3 + 1; // lowest: 1, highest: 13

#if DEBUG
		return result * 5;
#else
		return result;
#endif
	}

	[HttpGet]
	[AllowAnonymous]
	[Route("user-devices-limits")]
	public async Task<IActionResult> GetDevicesLimits()
	{
		return await Task.Run(() => Ok(this._settings.AccessLevelToMaxDevices?
			.Select(x => new AccessLevelToDevicesLimit() {
				AccessLevel = x.AccessLevel,
				DevicesLimit = x.DevicesLimit * _settings.DevicesLimitMultiplier
			})));
	}

	[HttpGet]
	public async Task<IActionResult> ListDevices()
	{
		return this.Ok(await this._context.Devices
			.Where(x => x.UserId == this.ParseIdClaim()).ToListAsync());
	}

	[HttpPut]
	public async Task<IActionResult> AddNewDevice([FromBody][Required] AddDeviceRequest request, [FromQuery] bool allowDuplicate = false)
	{
		if(!this.ValidatePubkey(request.WireguardPublicKey, 256 / 8)) {
			return this.BadRequest(ErrorMessages.WireguardPublicKeyFormatInvalid);
		}

		var found = await this._context.Devices.FirstOrDefaultAsync(x => x.WireguardPublicKey == request.WireguardPublicKey);
		if(found is not null) {

			return this.Problem(ErrorMessages.WireguardPublicKeyAlreadyExists, statusCode:
				(allowDuplicate && found.UserId == this.ParseIdClaim())
				? StatusCodes.Status302Found
				: StatusCodes.Status303SeeOther);
		}
		 
		var userId = this.ParseIdClaim();
		var userAccessLevel = (await this._context.Users.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == userId))?.GetAccessLevel();

		if(userAccessLevel is null) {
			return this.UnprocessableEntity(ErrorMessages.AccessJwtUserNotFound);
		}

		var devicesCount = await this._context.Devices.CountAsync(x => x.UserId == userId);
		if(devicesCount >= this.GetDevicesLimit(userAccessLevel.Value)) {
			return this.Conflict(ErrorMessages.DevicesLimitReached);
		}

		var added = this._context.Devices.Add(new UserDevice {
			UserId = userId,
			WireguardPublicKey = request.WireguardPublicKey,
			LastConnectedNodeId = null,
			LastSeenUtc = DateTime.UtcNow
		});

		await this._context.SaveChangesAsync();
		return this.StatusCode(StatusCodes.Status201Created);
	}


	/* RFC 3986
	 * The query component contains non-hierarchical data that, along 
	 * with data in the path component, serves to identify a resource. But... 
	 * base64-encoded wg pubkey in url?
	 */
	[HttpPatch]
	public async Task<IActionResult> DeleteDevice([FromBody][Required] DeleteDeviceRequest request)
	{
		var userId = this.ParseIdClaim();

		var toDelete = await this._context.Devices.FirstOrDefaultAsync(d =>
			d.WireguardPublicKey == request.WireguardPublicKey && d.UserId == userId);

		if(toDelete is null) {
			return this.NotFound();
		}

		if(toDelete.LastConnectedNodeId is not null) {
			// not awaited, fire-and-forget
			_ = this._nodesService.RemovePeerFromNode(
				toDelete.WireguardPublicKey, toDelete.LastConnectedNodeId.Value);
		}

		this._context.Remove(toDelete);
		await this._context.SaveChangesAsync();

		return this.Accepted(); // because we did not await one of the calls above 
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