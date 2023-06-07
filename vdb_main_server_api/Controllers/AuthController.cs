using DataAccessLayer.Contexts;
using DataAccessLayer.Models;
using main_server_api.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using ServicesLayer.Models.Common;
using ServicesLayer.Models.Runtime;
using ServicesLayer.Services;
using ServicesLayer.Services.Static;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace main_server_api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
	private const string JwtRefreshTokenCookieName = @"jwt_refresh_token";

	private readonly VpnContext _context;
	private readonly JwtService _jwtService;
	private readonly RegistrationLimiterService _regLimiter;
	private readonly ILogger<AuthController> _logger;
	private static CookieOptions? _jwtCookieOptions;

	public AuthController(VpnContext context, JwtService jwtService, RegistrationLimiterService regLimiter, ILogger<AuthController> logger)
	{
		this._context = context;
		this._jwtService = jwtService;
		this._regLimiter = regLimiter;
		this._logger = logger;
	}


	// Метод возвращает OkResult, сигнализируя, что авторизация пользователя возможна.
	[HttpGet]
	public IActionResult Validate() => this.Ok();



	[HttpGet]
	[Route("sessions")]
	public async Task<IActionResult> GetRefreshCount()
	{
		var count = await this._context.Users.Where(x => x.Id == this.ParseIdClaim())
			.Select(x => x.RefreshTokensEntropies.Count).FirstOrDefaultAsync();

		return this.Ok(new SessionsResponse() { TotalCount = count });
	}

	/* Метод служит для генерации access & refresh JWT.
	 * Access генерируется в любом случае.
	 * Refresh генерируется в случае provideRefresh = true.
	 * 
	 * Твик refreshJwtInBody определяется, будет ли refresh JWT возвращен
	 * внутри responseObj для дальней записи в body ответа, либо будет
	 * сразу же записан в куки ответа (HTTP-Only & TLS-Only).
	 * 
	 * Метод добавляет токен в переданную модель user, 
	 * но не выполняет SaveChanges.
	 */
	[NonAction]
	public JwtResponse IssueJwtAndAddToUser(User user, bool provideRefresh = true, bool refreshJwtInBody = false)
	{
		// create access token using passed user model
		var responseObj = new JwtResponse(this._jwtService.GenerateAccessJwtToken(new UserInfo(user)));
		if(!provideRefresh) return responseObj;


		// is there too much tokens already? then just trim a half
		if(user.RefreshTokensEntropies.Count > byte.MaxValue / 8) {
			user.RefreshTokensEntropies = user.RefreshTokensEntropies
				.Skip(user.RefreshTokensEntropies.Count / 2)
				.ToList();
		}

		// create new refresh token for inserting into DB
		var issuedTokenRecord = new RefreshToken {
			IssuedToUser = user.Id,
			Entropy = Random.Shared.NextInt64(long.MinValue, long.MaxValue)
		};

		// write refresh token
		var refreshToken = this._jwtService.GenerateRefreshJwtToken(issuedTokenRecord);

		// decide where to place refresh token
		if(refreshJwtInBody) {
			responseObj.RefreshToken = refreshToken;
		} else {
			// if static is not inited yet
			if(_jwtCookieOptions is null
#if DEBUG
				|| true
#endif
				) {
				_jwtCookieOptions = new CookieOptions() {
#if RELEASE
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
#else
					Secure = false,
					SameSite = SameSiteMode.Lax,
#endif
					HttpOnly = true,
					IsEssential = true,
					Path = "/api/auth",
					MaxAge = this._jwtService.RefreshTokenLifespan
				};
			}
			this.Response.Cookies.Append(JwtRefreshTokenCookieName, refreshToken, _jwtCookieOptions);
		}

		user.RefreshTokensEntropies.Add(issuedTokenRecord.Entropy);
		return responseObj;
	}


	/* Метод валидирует refresh-JWT и находит связанного с ним юзверя.
	 * 
	 * Метод вносит изменения в модель (удаляет токен),
	 * но не выполняет SaveChanges.
	 */
	[NonAction]
	public async Task<(User? foundUserAsTracking, IActionResult? errorResult)> ValidateAndRemoveRefreshJWT(string refreshJwt)
	{
		// validate JWT format and extract value
		int userId;
		long jwtEntropy;
		try {
			var parsed = this._jwtService.ValidateJwtToken(refreshJwt);
			userId = int.Parse(parsed.FindFirstValue(nameof(RefreshToken.IssuedToUser))!);
			jwtEntropy = long.Parse(parsed.FindFirstValue(nameof(RefreshToken.Entropy))!);
		} catch {
			return (null, this.BadRequest(ErrorMessages.RefreshJwtIsInvalid));
		}

		// find user in database
		var foundUser = await this._context.Users.AsTracking()
			.FirstOrDefaultAsync(x => x.Id == userId);
		if(foundUser is null) {
			/* RFC 9110 https://www.rfc-editor.org/rfc/rfc9110.html#name-422-unprocessable-content
			 * 15.5.21. 422 Unprocessable Content
			 * The 422 (Unprocessable Content) status code indicates that 
			 * the server understands the content type of the request content 
			 * (hence a 415 (Unsupported Media Type) status code is inappropriate), 
			 * and the syntax of the request content is correct, but it was unable 
			 * to process the contained instructions. For example, this status code 
			 * can be sent if an XML request content contains well-formed (i.e., syntactically correct), 
			 * but semantically erroneous XML instructions.
			 */
			return (null, this.UnprocessableEntity(ErrorMessages.RefreshJwtUserNotFound));
		}

		// find jwt in user collection
		if(!foundUser.RefreshTokensEntropies.Contains(jwtEntropy)) {
			return (foundUser, this.Unauthorized(ErrorMessages.RefreshJwtIsNotFound));
		}

		// remove used jwt
		foundUser.RefreshTokensEntropies.Remove(jwtEntropy);

		// return associated user
		return (foundUser, null);
	}


	/* Метод валидирует переденные емаил и пароль.
	 * В ответ создаются refresh & access токены.
	 * Refresh может быть записан в тело, если передан
	 * соответствующий параметр.
	 */
	[HttpPost]
	[AllowAnonymous]
	public async Task<IActionResult> Login(
	[FromBody][Required] LoginRequest request,
	[FromQuery] bool provideRefresh = true,
	[FromQuery] bool refreshJwtInBody = false)
	{
		User? found;
		if(provideRefresh) { // as tracking if refresh is need to be saved
			found = await this._context.Users.AsTracking()
				.FirstOrDefaultAsync(x => x.Email == request.Email);
		} else {
			found = await this._context.Users.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Email == request.Email);
		}

		if(found is null) {
			return this.NotFound(nameof(request.Email));
		}

		if(!PasswordsService.ConfirmPassword(request.Password, found.PasswordHash, found.PasswordSalt)) {
			return this.Unauthorized(nameof(request.Password));
		}

		return await this.OkJwt(found, provideRefresh, refreshJwtInBody);
	}

	[NonAction]
	public async Task<OkObjectResult> OkJwt(
	User user,
	bool provideRefresh = true,
	bool refreshJwtInBody = false)
	{
		var jwt = this.IssueJwtAndAddToUser(user, provideRefresh, refreshJwtInBody);
		if(provideRefresh) {
			await this._context.SaveChangesAsync();
		}

		return this.Ok(jwt);
	}



	/* Метод регистрирует пользователя.
	 * По дефолту включено перенаправление на Login,
	 * если пользователь уже существует, что может быть модифицировано
	 * через url запроса.
	 */
	[HttpPut]
	[AllowAnonymous]
	public async Task<IActionResult> Register(
		[FromBody][Required] RegistrationRequest request,
		[FromQuery] bool redirectToLogin = false,
		[FromQuery] bool provideRefresh = true,
		[FromQuery] bool refreshJwtInBody = false)
	{
		// if user already exists
		if(await this._context.Users.AnyAsync(x => x.Email.Equals(request.Email))) {
			if(redirectToLogin) { // just redirect to login?
				return await this.Login(request, provideRefresh, refreshJwtInBody);
			} else { // or tell him to fuck off?
				return this.Conflict(nameof(request.Email));
			}
		}

		if(!this._regLimiter.CountAndAllow()) {
			return this.StatusCode(StatusCodes.Status503ServiceUnavailable);
		}

		// create password hash
		var passHash = PasswordsService.HashPassword(request.Password, out var passSalt);
		// create user
		var toAdd = new User {
			// basic user part
			Email = request.Email,
			PasswordSalt = passSalt,
			PasswordHash = passHash,
			IsAdmin = false,
			IsEmailConfirmed = false,
			PayedUntil = DateTime.MinValue,
			// other part
			RefreshTokensEntropies = new(0),
		};

		this._logger.LogDebug($"Added new user:\n" +
			$"==> Email: \'{toAdd.Email}\'\n" +
			$"==> PassHashB64: \'{Convert.ToBase64String(toAdd.PasswordHash)}\'\n" +
			$"==> PassSaltB64: \'{Convert.ToBase64String(toAdd.PasswordSalt)}\'");

		this._context.Users.Add(toAdd);
		await this._context.SaveChangesAsync();

		/* let the Login endpoint generate JWTs, moreover 
		 * it will validate registration succeeded
		 */
		return await this.Login(request, provideRefresh, refreshJwtInBody);
	}


	/* Данный метод должен выдавать юзверю новые JWT.
	 * Refresh может быть перед как в куках, так и в теле,
	 * но не в обоих местах.
	 */
	[HttpPatch] // yeah, prohibiting body in HttpDelete was a great decision! now try to pass a base64 there, the genuis you are, HTTP creator!
	[Route("")]
	[Route("refresh")]
	[AllowAnonymous]
	public async Task<IActionResult> RenewJwt([FromBody] RefreshJwtRequest? request)
	{
		var fromCookie = this.Request.Cookies[JwtRefreshTokenCookieName];
		var fromBody = request?.RefreshToken;
		string jwt;
		bool refreshJwtInBody;

		// validate that JWT is passed correctly
		if(fromCookie is not null && fromBody is null) {
			jwt = fromCookie;
			refreshJwtInBody = false;
		} else
		if(fromCookie is null && fromBody is not null) {
			jwt = fromBody;
			refreshJwtInBody = true;
		} else {
			return this.BadRequest(ErrorMessages.RefreshJwtIsExpectedInCookiesXorBody);
		}

		var (foundUserAsTracking, errorResult) = await this.ValidateAndRemoveRefreshJWT(jwt);
		if(errorResult is not null) {
			return errorResult;
		}

		// foundUser is guaranteed to be not null IF errorResult is null
		var newToken = this.IssueJwtAndAddToUser(foundUserAsTracking!, true, refreshJwtInBody);
		await this._context.SaveChangesAsync();
		return this.Ok(newToken);
	}


	/* Данное действие следует применять только с refresh-токеном из Http-only.
	 * Оно предполагает удаление всех refresh токенов данного юзера из базы данных.
	 */
	[HttpDelete]
	[Route("")]
	[Route("other-sessions")]
	public async Task<IActionResult> DeleteAllOtherDevices()
	{
		var refreshJwt = this.Request.Cookies[JwtRefreshTokenCookieName];

		if(refreshJwt is null) {
			return this.BadRequest(ErrorMessages.RefreshJwtIsExpectedInCookies);
		}

		var (foundUserAsTracking, errorResult) = await this.ValidateAndRemoveRefreshJWT(refreshJwt);
		if(errorResult is not null) {
			return errorResult;
		}

		// foundUser is guaranteed to be not null IF errorResult is null
		foundUserAsTracking!.RefreshTokensEntropies = new(1);
		var newToken = this.IssueJwtAndAddToUser(foundUserAsTracking!);
		await this._context.SaveChangesAsync();
		return this.Ok(newToken);
	}

	[HttpDelete]
	[Route("self")]
	[Route("self/{token}")]
	public async Task<IActionResult> TerminateSession([FromRoute] string? token = null)
	{
		var refreshJwt = this.Request.Cookies[JwtRefreshTokenCookieName];

		if(refreshJwt is not null && !string.IsNullOrWhiteSpace(token)) {
			return this.BadRequest(ErrorMessages.RefreshJwtIsExpectedInCookiesXorRoute);
		} else
		if(refreshJwt is null) {
			refreshJwt = token;
		}

		if(refreshJwt is null) {
			return this.BadRequest(ErrorMessages.RefreshJwtIsExpectedInCookies);
		}

		// simply do validate without issuing the new one
		var (foundUserAsTracking, errorResult) = await this.ValidateAndRemoveRefreshJWT(refreshJwt);
		if(errorResult is not null) {
			return errorResult;
		}

		await this._context.SaveChangesAsync();
		return this.Ok();
	}

	[HttpPatch]
	[Route("password")]
	public async Task<IActionResult> ChangePassword([FromBody][Required] ChangePasswordRequest request)
	{
		var user = await this._context.Users.FirstOrDefaultAsync(x => x.Id == this.ParseIdClaim());
		if(user is null) {
			return this.NotFound();
		}

		SetPassword(user, request);

		try {
			await this._context.SaveChangesAsync();
			return this.Ok();
		} catch {
			return this.Problem();
		}
	}

	[NonAction]
	private void SetPassword(User dbEntry, ChangePasswordRequest request)
	{
		var passHash = PasswordsService.HashPassword(request.Password, out var passSalt);
		dbEntry.PasswordSalt = passSalt;
		dbEntry.PasswordHash = passHash;
	}



	#region recovery
	private const string isRecoveryJwtClaim = nameof(isRecoveryJwtClaim);
	private const string emailJwtClaim = nameof(emailJwtClaim);
	private const string entropyClaim = nameof(entropyClaim);

	[HttpPut]
	[AllowAnonymous]
	[Route("recovery/{email}")]
	public async Task<IActionResult> CreateAndSendLink(
		[FromServices] SettingsProviderService settings,
		[FromServices] EmailSendingService sender,
		[FromRoute][Required][EmailAddress] string email)
	{
		var found = await this._context.Users.FirstOrDefaultAsync(x => x.Email == email);
		if(found is null) return this.NotFound();


		var metaValues = settings.MetaValues;
		var minDelay = TimeSpan.FromSeconds(settings.UserEmailLimitations.MinimalDelayBetweenMailsSenconds);
#if RELEASE
		if((DateTime.UtcNow - found.LastSendedEmail) < minDelay) {
			this.Response.Headers.Add(HeaderNames.RetryAfter,
				(minDelay - (DateTime.UtcNow - found.LastSendedEmail)).TotalSeconds.ToString());
			return this.StatusCode(503);
		}
#endif

		var entropy = DateTime.UtcNow.Ticks;
		var jwt = this._jwtService.GenerateJwtToken(new Claim[] {
			new(entropyClaim,entropy.ToString(),ClaimValueTypes.Integer64),
			new(isRecoveryJwtClaim,true.ToString(),ClaimValueTypes.Boolean),
			new(emailJwtClaim,email,ClaimValueTypes.String)
		}, minDelay * 16);

#if RELEASE
		var result = (int)(await sender.Send(new() {
			Subject = "Account recovery",
			Body = "Hello. Here is your link. Use it to login, then change your password. If you did not request a password reset, please ignore this message.\r\n" + metaValues.PasswordRecoveryBase + jwt,
			From = metaValues.ProjectEmailNoReply,
			FromName = metaValues.ProjectName,
			To = found.Email,
		}));
#endif

#if RELEASE
		if(200 <= result && result <= 299) {
#endif
		found.RecoveryJwtEntropy = entropy;
		found.LastSendedEmail = DateTime.UtcNow;
		await this._context.SaveChangesAsync();
		return this.Ok();
#if RELEASE
		} else {
			this.Response.Headers.Add(HeaderNames.RetryAfter, (minDelay.TotalSeconds + 60).ToString());
			return this.StatusCode(503);
		}
#endif
	}

	[HttpPost]
	[AllowAnonymous]
	[Route("recovery/{jwt}")]
	public async Task<IActionResult> ResetPassword([FromRoute][Required] string jwt, [FromBody][Required] ChangePasswordRequest request)
	{
		_logger.LogInformation("Begin password reset.");

		if(string.IsNullOrEmpty(jwt)) // does this fuck even give us what we need
			return this.BadRequest();

		var cls = this._jwtService.ValidateJwtToken(jwt); // ok, decode

		if(!bool.Parse(cls.FindFirstValue(isRecoveryJwtClaim)!)) // em... dont ask
			return this.BadRequest();

		var email = cls.FindFirstValue(emailJwtClaim); // and this should also never happen
		if(email is null)
			return BadRequest();

		var found = await this._context.Users.FirstOrDefaultAsync(x => x.Email == email);
		if(found is null) // user in the bag?
			return this.NotFound();

		var entropy = long.Parse(cls.FindFirstValue(entropyClaim)!); // ok, here we go
		if(entropy != found.RecoveryJwtEntropy)
			return this.Forbid();

		_logger.LogInformation($"JWT valid and user found in the database: \'{found.Email}\'.");

		found.RecoveryJwtEntropy = 0;
		SetPassword(found, request);

		try {
			await this._context.SaveChangesAsync();
			_logger.LogInformation($"Password reset successfully.");
			return this.Ok();
		} catch {
			_logger.LogInformation($"Problem saving new password to the db.");
			return this.Problem();
		}
	}
#endregion
}
