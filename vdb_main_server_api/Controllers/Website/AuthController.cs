using DataAccessLayer.Contexts;
using DataAccessLayer.Models;
using main_server_api.Models.UserApi.Website.Auth;
using main_server_api.Models.UserApi.Website.Common;
using main_server_api.Services.Static;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
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

		var responseObj = new JwtResponse(_jwtService.GenerateAccessJwtToken(new UserInfo(found)));

		if (!(provideRefresh ?? true))
		{
			return Ok(responseObj);
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
			Response.Cookies.Append(JwtRefreshTokenCookieName, refreshToken);
		}

		return Ok(responseObj);

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

		return await Login(request, provideRefresh,refreshJwtInBody);
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
