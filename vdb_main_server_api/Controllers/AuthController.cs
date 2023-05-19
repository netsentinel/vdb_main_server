using DataAccessLayer.Contexts;
using DataAccessLayer.Models;
using main_server_api.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicesLayer.Models.Common;
using ServicesLayer.Services;
using ServicesLayer.Services.Static;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

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
    private readonly ILogger<AuthController> _logger;
    private static CookieOptions? _jwtCookieOptions;

    public AuthController(VpnContext context, JwtService jwtService, ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }


    // Метод возвращает OkResult, сигнализируя, что авторизация пользователя возможна.
    [HttpGet]
    public IActionResult Validate() => Ok();

    [HttpGet]
    [Route("sessions")]
    public async Task<IActionResult> GetRefreshCount()
    {
        var count = await _context.Users.Where(x => x.Id == this.ParseIdClaim())
            .Select(x => x.RefreshTokensEntropies.Count).FirstOrDefaultAsync();

        return Ok(new SessionsResponse() { TotalCount = count });
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
        var responseObj = new JwtResponse(_jwtService.GenerateAccessJwtToken(new UserInfo(user)));
        if (!provideRefresh) return responseObj;


        // is there too much tokens already? then just trim a half
        if (user.RefreshTokensEntropies.Count > byte.MaxValue / 8)
        {
            user.RefreshTokensEntropies = user.RefreshTokensEntropies
                .Skip(user.RefreshTokensEntropies.Count / 2)
                .ToList();
        }

        // create new refresh token for inserting into DB
        var issuedTokenRecord = new RefreshToken
        {
            IssuedToUser = user.Id,
            Entropy = Random.Shared.NextInt64(long.MinValue, long.MaxValue)
        };

        // write refresh token
        var refreshToken = _jwtService.GenerateRefreshJwtToken(issuedTokenRecord);

        // decide where to place refresh token
        if (refreshJwtInBody)
        {
            responseObj.RefreshToken = refreshToken;
        }
        else
        {
            // if static is not inited yet
            if (_jwtCookieOptions is null
#if DEBUG
				|| true
#endif
                )
            {
                _jwtCookieOptions = new CookieOptions()
                {
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
                    MaxAge = _jwtService.RefreshTokenLifespan
                };
            }
            Response.Cookies.Append(JwtRefreshTokenCookieName, refreshToken, _jwtCookieOptions);
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
        try
        {
            var parsed = _jwtService.ValidateJwtToken(refreshJwt);
            userId = int.Parse(parsed.FindFirstValue(nameof(RefreshToken.IssuedToUser))!);
            jwtEntropy = long.Parse(parsed.FindFirstValue(nameof(RefreshToken.Entropy))!);
        }
        catch
        {
            return (null, BadRequest(ErrorMessages.RefreshJwtIsInvalid));
        }

        // find user in database
        var foundUser = await _context.Users.AsTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);
        if (foundUser is null)
        {
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
            return (null, UnprocessableEntity(ErrorMessages.RefreshJwtUserNotFound));
        }

        // find jwt in user collection
        if (!foundUser.RefreshTokensEntropies.Contains(jwtEntropy))
        {
            return (foundUser, Unauthorized(ErrorMessages.RefreshJwtIsNotFound));
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
        if (provideRefresh)
        { // as tracking if refresh is need to be saved
            found = await _context.Users.AsTracking()
                .FirstOrDefaultAsync(x => x.Email == request.Email);
        }
        else
        {
            found = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == request.Email);
        }

        if (found is null)
        {
            return NotFound(nameof(request.Email));
        }

        if (!PasswordsService.ConfirmPassword(request.Password, found.PasswordHash, found.PasswordSalt))
        {
            return Unauthorized(nameof(request.Password));
        }

        var jwt = IssueJwtAndAddToUser(found, provideRefresh, refreshJwtInBody);
        if (provideRefresh)
        {
            await _context.SaveChangesAsync();
        }

        return Ok(jwt);
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
        if (await _context.Users.AnyAsync(x => x.Email.Equals(request.Email)))
        {
            if (redirectToLogin)
            { // just redirect to login?
                return await Login(request, provideRefresh, refreshJwtInBody);
            }
            else
            { // or tell him to fuck off?
                return Conflict(nameof(request.Email));
            }
        }

        // create password hash
        var passHash = PasswordsService.HashPassword(request.Password, out var passSalt);
        // create user
        var toAdd = new User
        {
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

        _logger.LogDebug($"Added new user:\n" +
            $"==> Email: \'{toAdd.Email}\'\n" +
            $"==> PassHashB64: \'{Convert.ToBase64String(toAdd.PasswordHash)}\'\n" +
            $"==> PassSaltB64: \'{Convert.ToBase64String(toAdd.PasswordSalt)}\'");

        _context.Users.Add(toAdd);
        await _context.SaveChangesAsync();

        /* let the Login endpoint generate JWTs, moreover 
		 * it will validate registration succeeded
		 */
        return await Login(request, provideRefresh, refreshJwtInBody);
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
        var fromCookie = Request.Cookies[JwtRefreshTokenCookieName];
        var fromBody = request?.RefreshToken;
        string jwt;
        bool refreshJwtInBody;

        // validate that JWT is passed correctly
        if (fromCookie is not null && fromBody is null)
        {
            jwt = fromCookie;
            refreshJwtInBody = false;
        }
        else
        if (fromCookie is null && fromBody is not null)
        {
            jwt = fromBody;
            refreshJwtInBody = true;
        }
        else
        {
            return BadRequest(ErrorMessages.RefreshJwtIsExpectedInCookiesXorBody);
        }

        var (foundUserAsTracking, errorResult) = await ValidateAndRemoveRefreshJWT(jwt);
        if (errorResult is not null)
        {
            return errorResult;
        }

        // foundUser is guaranteed to be not null IF errorResult is null
        var newToken = IssueJwtAndAddToUser(foundUserAsTracking!, true, refreshJwtInBody);
        await _context.SaveChangesAsync();
        return Ok(newToken);
    }


    /* Данное действие следует применять только с refresh-токеном из Http-only.
	 * Оно предполагает удаление всех refresh токенов данного юзера из базы данных.
	 */
    [HttpDelete]
    [Route("")]
    [Route("other-sessions")]
    public async Task<IActionResult> DeleteAllOtherDevices()
    {
        var refreshJwt = Request.Cookies[JwtRefreshTokenCookieName];

        if (refreshJwt is null)
        {
            return BadRequest(ErrorMessages.RefreshJwtIsExpectedInCookies);
        }

        var (foundUserAsTracking, errorResult) = await ValidateAndRemoveRefreshJWT(refreshJwt);
        if (errorResult is not null)
        {
            return errorResult;
        }

        // foundUser is guaranteed to be not null IF errorResult is null
        foundUserAsTracking!.RefreshTokensEntropies = new(1);
        var newToken = IssueJwtAndAddToUser(foundUserAsTracking!);
        await _context.SaveChangesAsync();
        return Ok(newToken);
    }

    [HttpDelete]
    [Route("self")]
    [Route("self/{token}")]
    public async Task<IActionResult> TerminateSession([FromRoute] string? token = null)
    {
        var refreshJwt = Request.Cookies[JwtRefreshTokenCookieName];

        if (refreshJwt is not null && !string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(ErrorMessages.RefreshJwtIsExpectedInCookiesXorRoute);
        }
        else
        if (refreshJwt is null)
        {
            refreshJwt = token;
        }

        if (refreshJwt is null)
        {
            return BadRequest(ErrorMessages.RefreshJwtIsExpectedInCookies);
        }

        // simply do validate without issuing the new one
        var (foundUserAsTracking, errorResult) = await ValidateAndRemoveRefreshJWT(refreshJwt);
        if (errorResult is not null)
        {
            return errorResult;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch]
    [Route("password")]
    public async Task<IActionResult> ChangePassword([FromBody][Required] ChangePasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync();
        if (user is null)
        {
            return NotFound();
        }

        var passHash = PasswordsService.HashPassword(request.Password, out var passSalt);
        user.PasswordSalt = passSalt;
        user.PasswordHash = passHash;

        try
        {
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch
        {
            return Problem();
        }
    }
}
