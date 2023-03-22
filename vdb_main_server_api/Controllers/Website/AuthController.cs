using DataAccessLayer.Contexts;
using DataAccessLayer.Models;
using main_server_api.Models.UserApi.Website.Auth;
using main_server_api.Models.UserApi.Website.Common;
using main_server_api.Services.Static;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Security.Claims;
using vdb_main_server_api.Services;

namespace main_server_api.Controllers.Website;


[AllowAnonymous]
[Route("api/[controller]")]
public sealed class AccountController : ControllerBase
{
	private const string JwtRefreshTokenIdClaimName = "TokenId";
	private const string JwtRefreshTokenCookieName = "jwt_refresh_token";

	private readonly VpnContext _context;
	private readonly JwtService _jwtService;

	public AccountController(VpnContext context, JwtService jwtService)
	{
		_context = context;
		_jwtService = jwtService;
	}



	[HttpGet]
	[Authorize]
	public IActionResult Validate() => Ok();



	[NonAction]
	public async Task<JwtResponse> IssueJwtAndWriteToResponse(
		User user, bool? provideRefresh=null, bool? refreshJwtInBody=null)
	{
		var found = user;

		var responseObj = new JwtResponse(_jwtService.GenerateAccessJwtToken(new UserInfo(found)));

		if (!(provideRefresh ?? true))
		{
			return responseObj;
		}

		var issuedTokenRecord = new RefreshToken { IssuedToUser = found.Id };
		_context.RefreshTokens.Add(issuedTokenRecord);
		await _context.SaveChangesAsync();

		var refreshToken = _jwtService.GenerateJwtToken(new[] {
			new Claim(JwtRefreshTokenIdClaimName, issuedTokenRecord.Id.ToString()) },
			_jwtService.RefreshTokenLifespan);

		if (refreshJwtInBody ?? false)
		{
			responseObj.RefreshToken = refreshToken;
		}
		else
		{
			Response.Cookies.Append(JwtRefreshTokenCookieName, refreshToken, new CookieOptions()
			{
				HttpOnly = true,
				Secure = true,
				Expires = DateTime.UtcNow.Add(_jwtService.RefreshTokenLifespan),
				MaxAge = _jwtService.RefreshTokenLifespan,
				SameSite = SameSiteMode.Strict,
			});
		}

		return responseObj;
	}


	[HttpPost]
	public async Task<IActionResult> Login(
		[FromBody][Required] LoginRequest request,
		[FromQuery] bool? provideRefresh,
		[FromQuery] bool? refreshJwtInBody)
	{
		var found = await _context.Users.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Email == request.Email);

		if (found is null)
		{
			return NotFound(nameof(request.Email));
		}

		if (!PasswordsService.ConfirmPassword(request.Password, found.PasswordHash, found.PasswordSalt))
		{
			return Unauthorized(nameof(request.Password));
		}

		return Ok(await IssueJwtAndWriteToResponse(found,provideRefresh,refreshJwtInBody));
	}

	[HttpPut]
	public async Task<IActionResult> Register(
		[FromBody][Required] RegistrationRequest request,
		[FromQuery] bool? redirectToLogin,
		[FromQuery] bool? provideRefresh,
		[FromQuery] bool? refreshJwtInBody)
	{
		if ((redirectToLogin ?? true)
			&& _context.Users.Any(x => x.Email.Equals(request.Email)))
		{
			return await Login(request, provideRefresh, refreshJwtInBody);
		}

		var passHash = PasswordsService.HashPassword(request.Password, out var passSalt);
		var toAdd = new User
		{
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
	[Route("refresh")]
	public async Task<IActionResult> RenewJwt([FromBody] RefreshJwtRequest? request)
	{
		var fromCookie = Request.Cookies[JwtRefreshTokenCookieName];
		var fromBody = request?.RefreshToken;
		string jwt;
		bool refreshJwtInBody;

		// validate that JWT is passed correctly
		if (fromCookie is not null && fromBody is null)
		{
			jwt = fromCookie;
			refreshJwtInBody = false;
		} else 
		if(fromCookie is null && fromBody is not null)
		{
			jwt = fromBody;
			refreshJwtInBody = true;
		}
		else
		{
			return BadRequest(
				"Refresh JWT must be provided in cookies OR request body strictly.");
		}

		// validate JWT format and extract value
		int jwtId;
		try
		{
			var parsed = _jwtService.ValidateJwtToken(jwt);
			jwtId = int.Parse(parsed.FindFirstValue(JwtRefreshTokenIdClaimName)!);
		}
		catch
		{
			return BadRequest(
				"Refresh JWT format is invalid.");
		}


		// Check JWT in database
		var foundToken = await _context.RefreshTokens.AsTracking()
			.FirstOrDefaultAsync(x => x.Id == jwtId);

		if(foundToken is null || DateTime.UtcNow > foundToken.ValidUntilUtc)
		{
			return Unauthorized("Refresh JWT is not found on the server.");
		}

		// Find the user
		var foundUser = await _context.Users.AsNoTracking()
			.FirstOrDefaultAsync(x=> x.Id == foundToken.IssuedToUser);

		if(foundUser is null)
		{
			return Problem("Refresh JWT was correct but the user it was issued to is not found on the server.",
				statusCode: StatusCodes.Status410Gone);
		} 

		foundToken.ValidUntilUtc = DateTime.UtcNow.Add(_jwtService.RefreshTokenLifespan);
		return Ok(await IssueJwtAndWriteToResponse(foundUser, true, refreshJwtInBody)); // this method performs SaveChanges!
	}

	[HttpPatch]
	public IActionResult ChangePassword()
	{
		throw new NotImplementedException();
	}

	[HttpDelete]
	public IActionResult DeleteAccount()
	{
		throw new NotImplementedException();
	}
}
