using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using DataAccessLayer.Models;

namespace main_server_api.Models.UserApi.Website.Common;

public class UserInfo
{
	public bool IsAdmin { get; init; }
	public string Email { get; init; }
	public bool IsEmailConfirmed { get; init; }
	public List<int> UserDevicesIds { get; init; }
	public DateTime PayedUntilUtc { get; init; }


	public UserInfo(User source)
	{
		this.IsAdmin = source.IsAdmin;
		this.Email = source.Email;
		this.IsEmailConfirmed = source.IsEmailConfirmed;
		this.UserDevicesIds = source.UserDevicesIds;
		this.PayedUntilUtc = source.PayedUntil;
	}
}
