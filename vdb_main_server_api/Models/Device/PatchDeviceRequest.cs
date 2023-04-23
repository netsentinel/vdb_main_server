namespace main_server_api.Models.UserApi.Application.Device;

[Obsolete]
public class PatchDeviceRequest:AddDeviceRequest
{
	public long Id { get; set; }
}
