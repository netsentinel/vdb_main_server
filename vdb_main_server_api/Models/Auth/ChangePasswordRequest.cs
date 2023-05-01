using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

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
