namespace ServicesLayer.Models.Runtime;

public class JwtServiceSettings
{
	public int AccessTokenLifespanSeconds { get; set; }
	public int RefreshTokenLifespanSeconds { get; set; }
	public string SigningKeyBase64 { get; set; } = null!;
}