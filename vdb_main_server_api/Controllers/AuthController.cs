using DataAccessLayer.Contexts;
using DataAccessLayer.Models;
using main_server_api.Models.UserApi.Website.Auth;
using main_server_api.Models.UserApi.Website.Common;
using main_server_api.Services.Static;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Security.Claims;
using vdb_main_server_api.Services;

namespace main_server_api.Controllers;


[AllowAnonymous]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
	private const string JwtRefreshTokenCookieName = "jwt_refresh_token";

	private readonly VpnContext _context;
	private readonly JwtService _jwtService;
	private static CookieOptions? _jwtCookieOptions;

	public AuthController(VpnContext context, JwtService jwtService)
	{
		_context = context;
		_jwtService = jwtService;
	}

	// Метод возвращает OkResult, сигнализируя, что авторизация пользователя возможна.
	[HttpGet]
	[Authorize]
	public IActionResult Validate() => Ok();


	/* Метод служит для генерации access & refresh JWT.
	 * Access генерируется в любом случае.
	 * Refresh генерируется в случае provideRefresh = true.
	 * Твик refreshJwtInBody определяется, будет ли refresh JWT возвращен
	 * внутри responseObj для дальней записи в body ответа, либо будет
	 * сразу же записан в куки ответа (HTTP-Only & TLS-Only).
	 */
	[NonAction]
	public async Task<JwtResponse> IssueJwtAndSaveChanges(User user,
		bool? provideRefresh = null, bool? refreshJwtInBody = null)
	{
#if DEBUG
		var debugVar1 = new UserInfo(user);
#endif
		// create access token using passed user model
		var responseObj = new JwtResponse(_jwtService.GenerateAccessJwtToken(new UserInfo(user)));

		if(!(provideRefresh ?? true)) {
			return responseObj;
		}

		// create new refresh for inserting into DB
		var issuedTokenRecord = new RefreshToken {
			IssuedToUser = user.Id,
			ValidUntilUtc = DateTime.UtcNow.Add(_jwtService.RefreshTokenLifespan)
		};
		_context.RefreshTokens.Add(issuedTokenRecord);
		// SaveChanges MUST be performed for Id field appearence 
		await _context.SaveChangesAsync();

		// create refresh token
		var refreshToken = _jwtService.GenerateRefreshJwtToken(issuedTokenRecord);

		// where to place token?
		if(refreshJwtInBody ?? false) {
			responseObj.RefreshToken = refreshToken;
		} else {
			// is static is not inited yet
			if(_jwtCookieOptions is null) {
				_jwtCookieOptions = new CookieOptions() {
#if RELEASE
					HttpOnly = true,
					Secure = true,
#endif
					MaxAge = _jwtService.RefreshTokenLifespan,
					SameSite = SameSiteMode.Strict,
				};
			}
			Response.Cookies.Append(JwtRefreshTokenCookieName, refreshToken, _jwtCookieOptions);
		}

		return responseObj;
	}

	[NonAction]
	public async Task<(RefreshToken? foundToken, User? foundUser, IActionResult? errorResult)> ParseRefreshJwtAndFindUser(string refreshJwt)
	{
		// validate JWT format and extract value
		int jwtId;
		try {
			var parsed = _jwtService.ValidateJwtToken(refreshJwt);
			jwtId = int.Parse(parsed.FindFirstValue(nameof(RefreshToken.Id))!);
		} catch {
			return (null, null, BadRequest("Refresh JWT is invalid."));
		}

		// Check JWT in database
		var foundToken = await _context.RefreshTokens.AsTracking()
			.FirstOrDefaultAsync(x => x.Id == jwtId);

		if(foundToken is null) {
			return (null, null, Unauthorized("Refresh JWT is not found on the server."));
		}

		// Find the user
		var foundUser = await _context.Users.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == foundToken.IssuedToUser);

		if(foundUser is null) {
			return (null, null, Problem(
				"Refresh JWT is valid but the user it was issued to is not found on the server.",
				statusCode: StatusCodes.Status410Gone));
		}

		return (foundToken, foundUser, null);
	}


	/* Метод валидирует переденные емаил и пароль.
	 * В ответ создаются refresh & access токены.
	 * Refresh может быть записан в тело, если передан
	 * соответствующий параметр.
	 */
	[HttpPost]
	public async Task<IActionResult> Login(
	[FromBody][Required] LoginRequest request,
	[FromQuery] bool? provideRefresh = true,
	[FromQuery] bool? refreshJwtInBody = false)
	{
		var found = await _context.Users.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Email == request.Email);

		if(found is null) {
			return NotFound(nameof(request.Email));
		}

		if(!PasswordsService.ConfirmPassword(request.Password, found.PasswordHash, found.PasswordSalt)) {
			return Unauthorized(nameof(request.Password));
		}

		return Ok(await IssueJwtAndSaveChanges(found, provideRefresh, refreshJwtInBody));
	}

	/* Метод регистрирует пользователя.
	 * По дефолту включено перенаправление на Login,
	 * если пользователь уже существует, что может быть модифицировано
	 * через url запроса.
	 */
	[HttpPut]
	public async Task<IActionResult> Register(
		[FromBody][Required] RegistrationRequest request,
		[FromQuery] bool? redirectToLogin = true,
		[FromQuery] bool? provideRefresh = null,
		[FromQuery] bool? refreshJwtInBody = null)
	{
		// user already exists
		if(await _context.Users.AnyAsync(x => x.Email.Equals(request.Email))) {
			if(redirectToLogin ?? true) {
				return await Login(request, provideRefresh, refreshJwtInBody);
			} else {
				return Conflict(nameof(request.Email));
			}
		}

		// create password hash
		var passHash = PasswordsService.HashPassword(request.Password, out var passSalt);
		var toAdd = new User {
			Email = request.Email,
			PasswordSalt = passSalt,
			PasswordHash = passHash,
			IsAdmin = false,
			IsEmailConfirmed = false,
			PayedUntil = DateTime.MinValue,
			UserDevicesIds = new(0)
		};

		_context.Users.Add(toAdd);
		await _context.SaveChangesAsync();
		return await Login(request, provideRefresh, refreshJwtInBody);
	}


	[HttpPatch]
	[Route("")]
	[Route("refresh")]
	public async Task<IActionResult> RenewJwt([FromBody] RefreshJwtRequest? request)
	{
		var fromCookie = Request.Cookies[JwtRefreshTokenCookieName];
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
			return BadRequest(
				"Refresh JWT must be provided in cookies OR request body strictly.");
		}

		var (foundToken, foundUser, errorResult) = await ParseRefreshJwtAndFindUser(jwt);
		if(errorResult is not null) { // foundToken, foundUser is guaranteed to be not null
			return errorResult;
		}

		// Remove used JWT
		_context.RefreshTokens.Remove(foundToken!);
		return Ok(await IssueJwtAndSaveChanges(foundUser!, true, refreshJwtInBody));
	}

	/* Данное действие следует применять только с refresh-токеном из Http-only.
	 * Оно предполагает удаление всех refresh токенов данного юзера из базы данных.
	 */
	[HttpDelete]
	public async Task<IActionResult> DeleteAllOtherDevices()
	{
		var refreshJwt = Request.Cookies[JwtRefreshTokenCookieName];

		if(refreshJwt is null) {
			return BadRequest(
				"Refresh JWT must be provided in cookies strictly.");
		}

		var (foundToken, foundUser, errorResult) = await ParseRefreshJwtAndFindUser(refreshJwt);
		if(errorResult is not null) { // foundToken, foundUser is guaranteed to be not null
			return errorResult;
		}

		await _context.RefreshTokens.Where(t => t.IssuedToUser == foundToken!.IssuedToUser).ExecuteDeleteAsync();

		return Ok(await IssueJwtAndSaveChanges(foundUser!, true, false));
	}
}
