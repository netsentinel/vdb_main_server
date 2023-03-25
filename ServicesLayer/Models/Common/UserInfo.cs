using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using DataAccessLayer.Models;

namespace main_server_api.Models.UserApi.Website.Common;

public class UserInfo
{
	public int Id { get; set; }
	public bool IsAdmin { get; init; }
	public string Email { get; init; }
	public bool IsEmailConfirmed { get; init; }
	public List<long> UserDevicesIds { get; init; }
	public DateTime PayedUntilUtc { get; init; }


	public UserInfo(User source)
	{
		this.Id = source.Id;
		this.IsAdmin = source.IsAdmin;
		this.Email = source.Email;
		this.IsEmailConfirmed = source.IsEmailConfirmed;
		this.UserDevicesIds = source.UserDevicesIds;
		this.PayedUntilUtc = source.PayedUntil;
	}
}
