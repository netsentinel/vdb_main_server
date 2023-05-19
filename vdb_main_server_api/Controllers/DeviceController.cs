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
    private readonly VpnNodesManipulator _nodesService;
    private readonly DeviceControllerSettings _settings;
    private readonly Dictionary<int, int> _accessLevelToDevicesLimit;


    public DeviceController(VpnContext context, VpnNodesManipulator nodesService, SettingsProviderService settingsProvider)
    {
        _context = context;
        _nodesService = nodesService;
        _settings = settingsProvider.DeviceControllerSettings;
        _accessLevelToDevicesLimit = _settings.AccessLevelToMaxDevices?
            .ToDictionary(x => x.AccessLevel, x => x.DevicesLimit * _settings.DevicesLimitMultiplier)
            ?? new Dictionary<int, int>();
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
    [AllowAnonymous]
    [Route("user-devices-limits")]
    public async Task<IActionResult> GetDevicesLimits()
    {
        return await Task.Run(() => Ok(_settings.AccessLevelToMaxDevices?
            .Select(x => new AccessLevelToDevicesLimit()
            {
                AccessLevel = x.AccessLevel,
                DevicesLimit = x.DevicesLimit * _settings.DevicesLimitMultiplier
            })));
    }

    [HttpGet]
    public async Task<IActionResult> ListDevices()
    {
        return Ok(await _context.Devices
            .Where(x => x.UserId == this.ParseIdClaim()).ToListAsync());
    }

    [HttpPut]
    public async Task<IActionResult> AddNewDevice([FromBody][Required] AddDeviceRequest request, [FromQuery] bool allowDuplicate = false)
    {
        if (!this.ValidatePubkey(request.WireguardPublicKey, 256 / 8))
        {
            return BadRequest(ErrorMessages.WireguardPublicKeyFormatInvalid);
        }

        var found = await _context.Devices.FirstOrDefaultAsync(x => x.WireguardPublicKey == request.WireguardPublicKey);
        if (found is not null)
        {

            return Problem(ErrorMessages.WireguardPublicKeyAlreadyExists, statusCode:
                allowDuplicate && found.UserId == this.ParseIdClaim()
                ? StatusCodes.Status302Found
                : StatusCodes.Status303SeeOther);
        }

        var userId = this.ParseIdClaim();
        var userAccessLevel = (await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId))?.GetAccessLevel();

        if (userAccessLevel is null)
        {
            return UnprocessableEntity(ErrorMessages.AccessJwtUserNotFound);
        }

        var devicesCount = await _context.Devices.CountAsync(x => x.UserId == userId);
        if (devicesCount >= GetDevicesLimit(userAccessLevel.Value))
        {
            return Conflict(ErrorMessages.DevicesLimitReached);
        }

        var added = _context.Devices.Add(new UserDevice
        {
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
        var userId = this.ParseIdClaim();

        var toDelete = await _context.Devices.FirstOrDefaultAsync(d =>
            d.WireguardPublicKey == request.WireguardPublicKey && d.UserId == userId);

        if (toDelete is null)
        {
            return NotFound();
        }

        if (toDelete.LastConnectedNodeId is not null)
        {
            // not awaited, fire-and-forget
            _ = _nodesService.RemovePeerFromNode(
                toDelete.LastConnectedNodeId.Value, toDelete.WireguardPublicKey);
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

        return await DeleteDevice(new DeleteDeviceRequest { WireguardPublicKey = actualKey });
    }
}