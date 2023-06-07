using DataAccessLayer.Contexts;
using main_server_api.Models.Device;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServicesLayer.Services;
using System.ComponentModel.DataAnnotations;

namespace main_server_api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class ConnectionController : ControllerBase
{
	private readonly VpnContext _context;
	private readonly VpnNodesManipulator _nodesService;
	private readonly NodesPublicInfoBackgroundService _statusService;
	private readonly ILogger<ConnectionController> _logger;
	public ConnectionController(
		VpnContext context,
		VpnNodesManipulator nodesService,
		NodesPublicInfoBackgroundService statusService,
		ILogger<ConnectionController> logger)
	{
		this._context = context;
		this._nodesService = nodesService;
		this._statusService = statusService;
		this._logger = logger;
	}

	// This endpoint is HIGHLY recommended to be cached using reverse-proxy, i.e. NGINX
	// i.e. proxy_cache any 1m; 
	[HttpGet]
	[AllowAnonymous]
	[Route("nodes-list")]
	public async Task<IActionResult> GetNodesList([FromServices] NodesPublicInfoBackgroundService reporter)
	{
		return await Task.Run(() => this.Ok(reporter.LastReport));
	}

	/* TODO: Создать сервис отложенного отключения.
	 * Пусть этот сервис повторяет запрос на отключение от ноды с частатой,
	 * зависящей от параметра pressure, вычисляемого на основании числа заявок
	 * в очереди. Также что он должен дропать заявки, в случае переподлючения
	 * к ноде. Хотя необходимость дропа сомнительно, ибо если нода недоступна
	 * для отключения, то она недоступна и для подключения... ну, так должно быть.
	 * 
	 * Данный метод имеет некоторую уязвимость безопасности. 
	 * Рассмотрим следующий сценарий:
	 * 
	 * 1. Пользователь отправляет запрос на подключение к ноде с id=1. 
	 *    В базу данных осуществляется запись LastConnectedNodeId=1.
	 * 
	 * 2. Пользователь завершает процесс из диспетчера задач, не произведя
	 *    чистое отключение ('graceful disconnection').
	 *    
	 * 3. Пользователь очищает данные приложения, открывает его заного.
	 * 
	 * 4. Пользователь подключается к ноде id=2. Если в этот момент нода 
	 *    с id=1 недоступна, то строка '_ = _nodesService.RemovePeerFromNode(...'
	 *    это попросту игнорирует. Ложная недоступность возможна даже по причине
	 *    банальной потери пакетов.
	 *   
	 * 5. В случае, если нода с id=1 вновь оказывается доступна до того, как на ней
	 *    будет достигнут интервал обновления и сжатия списка пиров https://github.com/LuminoDiode/rest2wireguard/blob/476021dd1a26e793466e8f711707e66d2f6ed74a/vdb_node_api/Services/PeersBackgroundService.cs#L120
	 *				int delayS = 
	 *					_settings.PeersRenewIntervalSeconds 
	 *					- (int)(DateTime.UtcNow - _lastUpdateUtc).TotalSeconds;
	 *	  то в дейсвительность публичный ключ машины юзверя оказывается добавлен
	 *	  на двух нодах одновременно, что, скопировав приватный ключ на другую машину
	 *	  и при возможности реплицировать данную уязвимость, позволяет имея одну
	 *	  зарегистрированную в базе данных машину, подключить 'её' ко всем имеющимся
	 *	  нодам, а в действительности подключить N машин одновременно, которые будут
	 *	  зарегистрированы в базе данных как одна, а N равно суммарному числу нод 
	 *	  в системе.   
	 */
	[HttpPut]
	public async Task<IActionResult> ConnectToNode([FromBody][Required] ConnectDeviceRequest request)
	{
		var userId = this.ParseIdClaim();

		var foundDevice = this._context.Devices
			.FirstOrDefault(x =>
				x.WireguardPublicKey == request.WireguardPublicKey &&
				x.UserId == this.ParseIdClaim());

		if(foundDevice is null) {
			// device does not exist for the user, reset it locally and relogin
			return this.StatusCode(StatusCodes.Status406NotAcceptable);
		}

		this._logger.LogInformation($"Found device with id={foundDevice.Id}. " +
			$"Connecting it to node with id={request.NodeId}...");


		// ensure disconnected from prev node
		if(foundDevice.LastConnectedNodeId is not null
			&& foundDevice.LastConnectedNodeId != request.NodeId) {
			this._logger.LogInformation($"Sending disconnection request to the pevious connected " +
				$"node with id={foundDevice.LastConnectedNodeId}...");
			try {
				// not awaited, fire-and-forget
				_ = this._nodesService.RemovePeerFromNode(
					 foundDevice.LastConnectedNodeId.Value, foundDevice.WireguardPublicKey);
			} catch { }
		}

		foundDevice.LastConnectedNodeId = request.NodeId;
		foundDevice.LastSeenUtc = DateTime.UtcNow;
		try {
			this._logger.LogInformation($"Sending CONNECTION request for device with ID={foundDevice.Id}" +
				$"to node with ID={foundDevice.LastConnectedNodeId}...");
			var addResult = await this._nodesService.AddPeerToNode(request.NodeId, foundDevice.WireguardPublicKey);
			if(addResult is not null && addResult.InterfacePublicKey is not null) {
				var node = this._nodesService.IdToNode[request.NodeId].nodeInfo;
				await this._context.SaveChangesAsync();
				return this.Ok(new ConnectDeviceResponse(addResult,
					request.WireguardPublicKey, node.IpAddress.ToString(), node.WireguardPort));
			} else {
				this._logger.LogInformation($"Unable to add pubkey \'{request.WireguardPublicKey.Substring(0, 3)}...\' " +
				$"to node {request.NodeId}.");
				return this.Problem(Utf8Json.JsonSerializer.ToJsonString(addResult));
			}
		} catch(Exception ex) {
			try {
				// not awaited, fire-and-forget
				_ = this._nodesService.RemovePeerFromNode( // LastConnectedNodeId is not null here!
					foundDevice.LastConnectedNodeId.Value, foundDevice.WireguardPublicKey);
			} catch { }
			this._logger.LogInformation($"Unable to add pubkey \'{request.WireguardPublicKey.Substring(0, 3)}...\' " +
				$"to node {request.NodeId}: \'{ex.Message}\'.");
		}

		return this.StatusCode(StatusCodes.Status500InternalServerError);
	}
}
