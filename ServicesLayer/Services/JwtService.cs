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
#if RELEASE
			Expires = DateTime.UtcNow.Add(lifespan ?? this.AccessTokenLifespan),
#else
			Expires = DateTime.UtcNow.Add(lifespan ?? TimeSpan.FromSeconds(10)),
#endif 
			NotBefore = DateTime.UtcNow.AddSeconds(-1), // sometimes server responds so fast that nbf is being in the future so fails client-side validation
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
			IssuerSigningKey = new SymmetricSecurityKey(this._signingKey)
#if DEBUG
			/* Данный твик устанавливает шаг проверки валидации времени смерти токена.
			 * https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/blob/dev/src/Microsoft.IdentityModel.Tokens/TokenValidationParameters.cs#L345
			 * По умолчанию 5 минут, для тестов это слишком долго.
			 */
			,
			ClockSkew = TimeSpan.Zero
#endif
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

