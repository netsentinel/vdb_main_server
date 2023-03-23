namespace main_server_api.Models.UserApi.Application.Device;


/* Id представляет собой номер, под которым девайс был зарегистрирован в БД,
 * который должен далее использоваться для запросов.
 */
public class AddDeviceResponse
{
	public long Id { get; set; }
}
