using System.ComponentModel.DataAnnotations;

namespace main_server_api.Models.Auth;

public class RefreshJwtRequest
{
	[Required]
	public string RefreshToken { get; set; }
}
