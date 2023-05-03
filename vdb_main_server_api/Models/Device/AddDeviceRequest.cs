using System.ComponentModel.DataAnnotations;

namespace main_server_api.Models.Device;

public class AddDeviceRequest
{
	private const int LengthOfBase64For256Bits = 256 / 8 * 4 / 3 + 3;

	[Required]
	[MaxLength(LengthOfBase64For256Bits)]
	public string WireguardPublicKey { get; set; }
}
