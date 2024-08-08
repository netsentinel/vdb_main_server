//using DataAccessLayer.Contexts;
//using DataAccessLayer.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Net.Http.Headers;
//using ServicesLayer.Models.Runtime;
//using ServicesLayer.Services;
//using System.ComponentModel.DataAnnotations;
//using System.Data.Entity;
//using System.Data.Entity.ModelConfiguration.Conventions;
//using System.Security.Claims;

//namespace main_server_api.Controllers;


///* Ссылка для сброса пароля выглядит как url/jwt. Это обеспечивает также валидацию
// * времени жизни ссылки. Ссылки могут содержать точки согласно спецификации:
// * http://www.w3.org/Addressing/URL/url-spec.txt
// */
//[AllowAnonymous]
//[ApiController]
//[Route("api/[controller]")]
//[Consumes("application/json")]
//[Produces("application/json")]
//public class RecoveryController : ControllerBase
//{
//	const string isRecoveryJwtClaim = nameof(isRecoveryJwtClaim);
//	const string emailJwtClaim = nameof(emailJwtClaim);
//	const string entropyClaim = nameof(entropyClaim);

//	private readonly VpnContext _context;
//	private readonly JwtService _jwtService;
//	private readonly EmailSendingService _sender;
//	private readonly ILogger<AuthController> _logger;
//	private static CookieOptions? _jwtCookieOptions;

//	public RecoveryController(VpnContext context, JwtService jwtService, EmailSendingService sender, ILogger<AuthController> logger)
//	{
//		_context = context;
//		_jwtService = jwtService;
//		_sender = sender;
//		_logger = logger;
//	}

//	[HttpPut]
//	[Route("{email}")]
//	public async Task<IActionResult> CreateAndSendLink(
//		[FromServices] SettingsProviderService settings,
//		[FromServices] MetaValues metaValues,
//		[FromRoute][Required][EmailAddress] string email)
//	{
//		var found = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
//		if(found is null) return NotFound();

//		var minDelay = TimeSpan.FromSeconds(settings.UserEmailLimitations.MinimalDelayBetweenMailsSenconds);
//		if((DateTime.UtcNow - found.LastSendedEmail) < minDelay) {
//			Response.Headers.Add(HeaderNames.RetryAfter,
//				(minDelay - (DateTime.UtcNow - found.LastSendedEmail)).TotalSeconds.ToString());
//			return StatusCode(503);
//		}

//		var entropy = DateTime.UtcNow.Ticks;
//		var jwt = _jwtService.GenerateJwtToken(new Claim[] {
//			new(entropyClaim,entropy.ToString(),ClaimValueTypes.Integer64),
//			new(isRecoveryJwtClaim,true.ToString(),ClaimValueTypes.Boolean),
//			new(emailJwtClaim,email,ClaimValueTypes.String)
//		}, minDelay * 16);

//		var result = (int)(await _sender.Send(new() {
//			Subject = "Account recovery",
//				Body = metaValues.PasswordRecoveryBase + jwt,
//			   From = metaValues.ProjectEmailNoReply,
//			   FromName = metaValues.ProjectName,
//			   To = found.Email,   
//		}));

//		if(200<= result && result <= 299) {
//			found.RecoveryJwtEntripy = entropy;
//			await _context.SaveChangesAsync();
//			return Ok();
//		} else {
//			Response.Headers.Add(HeaderNames.RetryAfter, (minDelay.TotalSeconds + 60).ToString());
//			return StatusCode(503);
//		}
//	}

//	[HttpPost]
//	[Route("{jwt}")]
//	public async Task<IActionResult> LoginByLink([FromRoute][Required] string jwt)
//	{
//		if(string.IsNullOrEmpty(jwt)) // does this fuck even give us what we need
//			return BadRequest();


//		var cls = _jwtService.ValidateJwtToken(jwt); // ok, decode

//		if(!bool.Parse(cls.FindFirstValue(isRecoveryJwtClaim)!)) // em... dont ask
//			return BadRequest();

//		var found = await _context.Users.FirstOrDefaultAsync(x=> x.Email == jwt);
//		if(found is null) // user in the bag?
//			return NotFound();

//		var entropy = long.Parse(cls.FindFirstValue(entropyClaim)!); // ok, here we go
//		if(entropy != found.RecoveryJwtEntripy) 
//			return Forbid();

//		// ok, now just log him + f off him
//	}
//}
