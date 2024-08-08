using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace main_server_api.Models.Auth;

public class LoginRequest
{
	[Required]
	[MaxLength(50)]
	[DataType(DataType.EmailAddress)]
	[RegularExpression("^[a-zA-Z0-9_\\.-]+@([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,6}$", ErrorMessage = "E-mail is not valid")]
	public string Email { get; set; }

	[Required]
	[MinLength(6)]
	[MaxLength(256)]
	[DataType(DataType.Password)]
	[PasswordPropertyText]
	public string Password { get; set; }
}
