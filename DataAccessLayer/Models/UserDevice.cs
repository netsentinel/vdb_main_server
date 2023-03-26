using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataAccessLayer.Models;

/* Пользователь, выполняя вход на каком-либо устройстве, регистрирует его
 * в базе данных со следующими параметрами:
 * 
 * Свойство Id служит как PK.
 * 
 * Свойство WgPubkey - публичный ключ, который был сгенерирован на данном устройстве.
 * 
 * Свойство LastConnectedNodeId - Id последней ноды, к которой было подключено данное
 * устройство. При попытке устросйтва подключиться к другой ноде текущей ноде нужно 
 * отправить команду на его отключение.
 * 
 * Отношением User->UserDevice является OneToMany
 */
public class UserDevice
{
	// base64 length is 4/3 of bytes count, and '+3' is a replace for Math.Ceil
	private const int LengthOfBase64For256Bits = ((256 / 8) * 4 / 3) + 3;
	private const int LengthOfBase64For512Bits = ((512 / 8) * 4 / 3) + 3;



	public string GenerateDeviceName()
	{
		var bytes = Encoding.UTF8.GetBytes(this.WireguardPublicKey, 0, 4);
		return (bytes[0] + bytes[1] + bytes[2] + bytes[3]).ToString("000");
	}

	public long Id { get; set; }
	public int UserId { get; set; }
	[MaxLength(LengthOfBase64For256Bits)] public string WireguardPublicKey { get; set; }
	public int? LastConnectedNodeId { get; set; }
	public DateTime? LastSeenUtc { get; set; }
}
