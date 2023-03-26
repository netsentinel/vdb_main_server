using System.ComponentModel.DataAnnotations;

namespace main_server_api.Models.UserApi.Application.Device;

public class ConnectDeviceRequest
{
	[Required]
	[MaxLength(256*4/3+3)]
	public string WireguardPubkey { get; set; }
	[Required]
	[Range(0,int.MaxValue)]
	public int NodeId { get; set; }
}
