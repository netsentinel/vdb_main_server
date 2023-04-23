namespace main_server_api.Models.Runtime;

public class AccessLevelToDevicesLimit
{
	public int AccessLevel { get; set; }
	public int DevicesLimit { get; set; }
}

public class DeviceControllerSettings
{
	public AccessLevelToDevicesLimit[]? AccessLevelToMaxDevices { get; set; }
	public int DevicesLimitMultiplier { get; set; } = 1;
}
