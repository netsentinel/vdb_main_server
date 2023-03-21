using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

/* Всё приложение вращается вокруг данной модели.
 * Этот комментарий пишется перед её кодированием.
 * Опишем еще раз, что мы хотим получить в итоге.
 * 
 * Первое - регистрация пользователя. Мы используем почтовый
 * адрес и пароль. Почтовый адрес представляется как varchar(n).
 * Пароль представляется как byte[] Hash, ыекштп Salt, где 
 * Hash - HmacSha512(pass, salt), Salt - 512 байт энтропии.
 * Пользователь должен подтверждить почту. Для этого задаётся
 * поле bool IsEmailConfirmed.
 * 
 * У каждого пользователя есть уровень полномочий. Сейчас 
 * их определеяется 3 штуки - unconfirmed, free, payed, admin.
 * Смысл понятен из названий.
 * 
 * Каждому пользователю определяется массив текущих устройств,
 * на которых выполнен вход. Он хранится как модель из трех значений:
 * DeviceName - string (имя устройства), 
 *		возможен переход к (int,int) - два заранее известных слова
 * PublicKey - string (публичный ключ)
 * NodeName - string? (имя VPN-ноды)
 * RefreshToken - JWT refresh токен.
 * Когда пользователь выполняет вход на одном из устройств - оно
 * регистрируется соответствующим образом. Каждому пользователю установлен
 * лимит устройств в соответствии с уровнем доступа. Сервер проверяет, что он
 * не превышает лимиты под устройствам, в противном случае клиент
 * предлагает пользователю отключить одно из устройств и удалить его.
 * Отключение состоит в аннулировании Refresh-JWT путем его удаления из базы.

 */


public class User
{
	public enum AccessLevels
	{
		unconfirmed, // email
		free,
		payed,
		admin
	}


	public int Id { get; set; }
	[DefaultValue(false)] public bool IsAdmin { get; set; } = false;
	[MaxLength(50)] public string Email { get; set; }
	[DefaultValue(false)] public bool IsEmailConfirmed { get; set; } = false;
	[MaxLength(512)] public byte[] PasswordSalt { get; set; }
	[MaxLength(512)] public byte[] PasswordHash { get; set; }
	[MaxLength(512)] public byte[] RefreshJwtKey { get; set; }
	public List<int> UserDevicesIds { get; set; } // Foreign Keys for UserDevice model
	[DefaultValue(0)] public DateTime PayedUntil { get; set; } = DateTime.MinValue;
}
