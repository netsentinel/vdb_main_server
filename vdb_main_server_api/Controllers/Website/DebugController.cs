//#if DEBUG
//using DataAccessLayer.Contexts;
//using DataAccessLayer.Models;
//using main_server_api.Models.Auth;
//using main_server_api.Models.Device;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using ServicesLayer.Models.NodeApi;
//using ServicesLayer.Services;
//using System.ComponentModel.DataAnnotations;
//using System.Data.Entity;

//namespace main_server_api.Controllers.Website;

//[AllowAnonymous]
//[ApiController]
//[Route("api/[controller]")]
//[Consumes("application/json")]
//[Produces("application/json")]
//public class DebugController : ControllerBase
//{
//	private readonly SettingsProviderService _settingsProvider;
//	private readonly VpnNodesService _nodesService;
//	private readonly ILogger<DebugController> _logger;
//	private readonly VpnContext _context;
//	public DebugController(
//		SettingsProviderService settingsProvider,
//		VpnNodesService nodesService,
//		ILogger<DebugController> logger,
//		VpnContext ctx)
//	{
//#if RELEASE
//		throw new NotImplementedException("Debug controller is disabled.");
//#endif

//		if(DateTime.UtcNow > new DateTime(2023, 5, 7).AddDays(7)) {
//			throw new NotImplementedException("Update date to access debug controller.");
//		}

//		this._settingsProvider = settingsProvider;
//		this._nodesService = nodesService;
//		this._logger = logger;


//		this._logger.LogWarning($"{nameof(DebugController)} is being accessed.");

//		this._context = ctx;
//	}

//	[HttpGet]
//	public async Task<IActionResult> GetStatus()
//	{
//		return this.Ok();
//	}


//	[HttpPut]
//	[Route("add-device-to-user")]
//	public async Task<IActionResult> AddDevice(AddDeviceRequest request)
//	{
//		var userId = this.ParseIdClaim();

//		var added = this._context.Devices.Add(new UserDevice {
//			UserId = userId,
//			WireguardPublicKey = request.WireguardPublicKey,
//			LastConnectedNodeId = null,
//			LastSeenUtc = DateTime.UtcNow
//		});

//		await this._context.SaveChangesAsync();
//		return this.StatusCode(StatusCodes.Status201Created);
//	}

//	[HttpGet]
//	[Route("vpn-nodes")]
//	public async Task<IActionResult> GetNodes()
//	{
//		return this.Ok(this._settingsProvider.VpnNodeInfos.Select(x => x.ToNotParsed()));
//	}

//	[HttpPut]
//	[Route("push-to-ams")]
//	public async Task<IActionResult> PushToAms([Required][FromBody] PeerActionRequest request)
//	{
//		return this.Ok(await this._nodesService.AddPeerToNode(request.PublicKey, this._settingsProvider.VpnNodeInfos.First().Name));
//	}

//	[HttpPatch]
//	[Route("push-to-ams")]
//	public async Task<IActionResult> RemoveFromAms([Required][FromBody] PeerActionRequest request)
//	{
//		return await this._nodesService
//			.RemovePeerFromNode(request.PublicKey, this._settingsProvider.VpnNodeInfos.First().Name) ?
//			this.Ok() : this.Problem();
//	}

//	[HttpPost]
//	[Route("endpoint1")]
//	public async Task<IActionResult> tryValidation([Required][FromBody] LoginRequest request)
//	{
//		if(this.ModelState.IsValid) return this.Ok();
//		else return this.ValidationProblem();

//	}
//}
//#endif