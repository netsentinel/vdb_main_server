namespace DataAccessLayer.Models;

/* Пользователь, выполняя вход на каком-либо устройстве, регистрирует его
 * в базе данных со следующими параметрами:
 * 
 * Id - служебное свойство
 * 
 * DeviceId - номер девайса у данного пользователя. От 1 до int.MaxValue.
 * 
 * WgPubkey - публичный ключ, который был сгенерирован на данном устройстве.
 * 
 * LastConnectedNodeId - Id последней ноды, к которой было подключено данное
 * устройство. При попытке устросйтва подключиться к другой ноде текущей ноде
 * нужно отправить команду на его отключение.
 * 
 * RefreshJwtKey - ключ, который был записан в refresh-JWT. Мы не храним JWT
 * токены полностью, в этом нет смысла - в каждый токен записывается поле,
 * аля refresh_key:"key_base64", который является энтропией, будем хранить
 * только его.
 * 
 * Отношением User->UserDevice является OneToMany
 */
public class UserDevice
{
	public long Id { get; set; }
	public int DeviceId { get; set; }
	public string WgPubkey { get; set; }
	public int LastConnectedNodeId { get; set; }
	public string RefreshJwtKey { get; set; }
}
