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

[Authorize]
[Route("api/[controller]")]
public class ConnectionController : ControllerBase
{
	private readonly VpnContext _context;
	private readonly VpnNodesService _nodesService;
	public ConnectionController(VpnContext context, VpnNodesService nodesService)
	{
		_context = context;
		_nodesService = nodesService;
	}

	[HttpPut]
	public async Task<IActionResult> ConnectToNode(ConnectDeviceRequest request)
	{
		int userId;
		try {
			userId = int.Parse(Request.HttpContext.User.FindFirstValue(
				nameof(Models.UserApi.Website.Common.UserInfo.Id))!);
		} catch {
			return BadRequest("Access JWT is invalid.");
		}

		var found = await _context.Users.AsNoTracking().Where(u => u.Id == userId).Take(1)
			.Select(u => u.UserDevicesIds).AnyAsync(x => x.Contains(request.DeviceId));

		if(found == false) {
			// device does not exist for the user. reset it locally and relogin
			return StatusCode(StatusCodes.Status406NotAcceptable);
		}

		var device = await _context.UserDevices.AsTracking()
			.Where(d => d.Id == request.DeviceId).FirstOrDefaultAsync();

		if(device is null) {
			// this should really never happen
			return StatusCode(StatusCodes.Status406NotAcceptable);
		}

		if(device.LastConnectedNodeId is not null) {
			try {
				// not awaited, fire-and-forget
				_ = _nodesService.RemovePeerFromNode(device.WgPubkey, device.LastConnectedNodeId.Value);
			} catch { }
		} 

		try {
			var addResult = await _nodesService.AddPeerToNode(device.WgPubkey, request.NodeId);
			if(addResult is not null && addResult.InterfacePublicKey is not null) {
				device.LastConnectedNodeId = request.NodeId;
				device.LastSeenUtc = DateTime.UtcNow;
				await _context.SaveChangesAsync();
				return Ok(addResult);
			}
		} catch {
			// not awaited, fire-and-forget
			_ = _nodesService.RemovePeerFromNode(device.WgPubkey, request.NodeId);
		}

		return Problem();
	}
}
