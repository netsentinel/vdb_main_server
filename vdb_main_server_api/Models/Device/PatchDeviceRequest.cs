namespace main_server_api.Models.Device;

[Obsolete]
public class PatchDeviceRequest : AddDeviceRequest
{
	public long Id { get; set; }
}
