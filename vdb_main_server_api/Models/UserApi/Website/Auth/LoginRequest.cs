using Microsoft.AspNetCore.Mvc.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace main_server_api.Models.UserApi.Website.Auth;

public class LoginRequest
{
    [Required]
    [MaxLength(50)]
    [DataType(DataType.EmailAddress)]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(8)]
    [MaxLength(256)]
    [DataType(DataType.Password)]
    [PasswordPropertyText]
    public string Password { get; set; }
}
