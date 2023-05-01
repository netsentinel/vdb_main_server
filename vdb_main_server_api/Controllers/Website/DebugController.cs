#if DEBUG
using main_server_api.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using vdb_main_server_api.Models.Runtime;
using vdb_main_server_api.Services;
using vdb_node_api.Models.NodeApi;

namespace main_server_api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class Debug2Controller : ControllerBase
{
    private readonly SettingsProviderService _settingsProvider;
    private readonly VpnNodesService _nodesService;
    private readonly ILogger<DebugController> _logger;
    public Debug2Controller(
        SettingsProviderService settingsProvider,
        VpnNodesService nodesService,
        ILogger<DebugController> logger)
    {
        if (DateTime.UtcNow > new DateTime(2023, 3, 15).AddDays(7))
        {
            throw new NotImplementedException("Update date to access debug controller.");
        }

        _settingsProvider = settingsProvider;
        _nodesService = nodesService;
        _logger = logger;


        _logger.LogWarning($"{nameof(DebugController)} is being accessed.");
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        return Ok();
    }

    [HttpGet]
    [Route("vpn-nodes")]
    public async Task<IActionResult> GetNodes()
    {
        return Ok(_settingsProvider.VpnNodeInfos.Select(x => x.ToNotParsed()));
    }

    [HttpPut]
    [Route("push-to-ams")]
    public async Task<IActionResult> PushToAms([Required][FromBody] PeerActionRequest request)
    {
        return Ok(await _nodesService.AddPeerToNode(request.PublicKey, _settingsProvider.VpnNodeInfos.First().Name));
    }

    [HttpPatch]
    [Route("push-to-ams")]
    public async Task<IActionResult> RemoveFromAms([Required][FromBody] PeerActionRequest request)
    {
        return await _nodesService
            .RemovePeerFromNode(request.PublicKey, _settingsProvider.VpnNodeInfos.First().Name) ?
            Ok() : Problem();
    }

    [HttpPost]
    [Route("endpoint1")]
    public async Task<IActionResult> tryValidation([Required][FromBody]LoginRequest request)
    {
        if (ModelState.IsValid) return Ok();
        else return ValidationProblem();

	}
}
#endif