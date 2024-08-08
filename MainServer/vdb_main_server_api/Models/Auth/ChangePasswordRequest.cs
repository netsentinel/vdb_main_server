using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace main_server_api.Models.Auth;

public class ChangePasswordRequest
{
	[Required]
	[MinLength(6)]
	[MaxLength(256)]
	[DataType(DataType.Password)]
	[PasswordPropertyText]
	public string Password { get; set; }
}
