using DataAccessLayer.Models;
using main_server_api.Models.UserApi.Website.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace vdb_main_server_api.Services;

public sealed class JwtService
{
	private JwtSecurityTokenHandler _tokenHandler;
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

		if (_signingKey.Length != (512 / 8))
			throw new ArgumentOutOfRangeException("JWT signing key must be exact 512 bits long.");
	}


	public string GenerateJwtToken(IEnumerable<Claim> claims, TimeSpan? lifespan = null)
	{
		return _tokenHandler.WriteToken(_tokenHandler.CreateToken(new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.UtcNow.Add(lifespan ?? AccessTokenLifespan),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_signingKey), SecurityAlgorithms.HmacSha512Signature)
		}));
	}

	#region app-specific
	public string GenerateAccessJwtToken(UserInfo user)
	{
		return GenerateJwtToken(new Claim[]
		{
			new Claim(nameof(user.IsAdmin), user.IsAdmin.ToString()),
			new Claim(nameof(user.Email), user.Email),
			new Claim(nameof(user.IsEmailConfirmed),user.IsEmailConfirmed.ToString()),
			new Claim(nameof(user.UserDevicesIds), Utf8Json.JsonSerializer.ToJsonString(user.UserDevicesIds)),
			new Claim(nameof(user.PayedUntilUtc), user.PayedUntilUtc.ToString("o")) // 'o' format provider satisfies ISO 8601
		});
	}
	#endregion
}

