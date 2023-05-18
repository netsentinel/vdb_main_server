using Microsoft.IdentityModel.Tokens;
using ServicesLayer.Models.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ServicesLayer.Services;

public sealed class JwtService
{
	private readonly JwtSecurityTokenHandler _tokenHandler;
	private readonly byte[] _signingKey;
	public TimeSpan AccessTokenLifespan { get; init; }
	public TimeSpan RefreshTokenLifespan { get; init; }


	public JwtService(SettingsProviderService settingsProvider)
	{
		this._tokenHandler = new JwtSecurityTokenHandler();

		var settings = settingsProvider.JwtServiceSettings;
		this.AccessTokenLifespan = TimeSpan.FromSeconds(settings.AccessTokenLifespanSeconds);
		this.RefreshTokenLifespan = TimeSpan.FromSeconds(settings.RefreshTokenLifespanSeconds);
		this._signingKey = Convert.FromBase64String(settings.SigningKeyBase64);

		if(this._signingKey.Length != 512 / 8)
			throw new ArgumentOutOfRangeException("JWT signing key must be exact 512 bits long.");
	}

	public string GenerateJwtToken(IEnumerable<Claim> claims, TimeSpan? lifespan = null)
	{
		return this._tokenHandler.WriteToken(this._tokenHandler.CreateToken(new SecurityTokenDescriptor {
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.UtcNow.Add(lifespan ?? this.AccessTokenLifespan),
			// sometimes server responds so fast that nbf is being in the future so fails client-side validation
			NotBefore = DateTime.UtcNow.AddSeconds(-1),
			// HmacSha512 is used for message authentication, while HmacSha512Signature is used for creating and verifying digital signatures in JWTs.
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(this._signingKey), SecurityAlgorithms.HmacSha512Signature)
		}));
	}

	public ClaimsPrincipal ValidateJwtToken(string token)
	{
		var result = this._tokenHandler.ValidateToken(token, new TokenValidationParameters {
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(this._signingKey),
			/* Твик снизу устанавливает шаг проверки валидации времени смерти токена.
			 * CTRL+F 'public TimeSpan ClockSkew' по ссылке ниже
			 * https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/blob/dev/src/Microsoft.IdentityModel.Tokens/TokenValidationParameters.cs#L345
			 * По умолчанию 5 минут, для тестов это слишком долго.
			 * 
			 * Более того, судя по тому, что сказал GPT, проще вообще ставить 0 в любых случаях.
			 * 
			 * Clock skew in JWT (JSON Web Token) refers to the difference in time between the clock 
			 * on the server that issued the token and the clock on the server that is verifying the token. 
			 * This difference can cause issues with token validation, as the token may appear to be 
			 * expired even though it is still valid according to the issuing server's clock. 
			 * To account for clock skew, JWT implementations typically allow for a certain amount of 
			 * leeway in the token's expiration time, or include a timestamp in the token itself that 
			 * can be used to calculate the actual expiration time regardless of clock differences. 
			 * It is important to properly handle clock skew to ensure that JWT-based authentication 
			 * and authorization systems are reliable and secure.
			 */
			ClockSkew = TimeSpan.Zero
		}, out _);
		return result;
	}

	#region app-specific
	public string GenerateAccessJwtToken(UserInfo user)
	{
		return this.GenerateJwtToken(new Claim[]
		{
			new Claim(nameof(user.Id),user.Id.ToString()),
			new Claim(nameof(user.IsAdmin), user.IsAdmin.ToString()),
			new Claim(nameof(user.Email), user.Email),
			new Claim(nameof(user.IsEmailConfirmed),user.IsEmailConfirmed.ToString()),
			new Claim(nameof(user.PayedUntilUtc), user.PayedUntilUtc.ToString("o")) // 'o' format provider satisfies ISO 8601
		});
	}

	public string GenerateRefreshJwtToken(RefreshToken token)
	{
		return this.GenerateJwtToken(new[] {
			new Claim(nameof(token.IssuedToUser), token.IssuedToUser.ToString()),
			new Claim(nameof(token.Entropy),token.Entropy.ToString()),
			}, this.RefreshTokenLifespan);
	}
	#endregion
}

