using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models;

/* Свойство Id служит как PK.
 * 
 * Свойство IsAdmin служит как идентификатор администратора.
 * 
 * Свойство Email служит как AK и логин для входа на сайт.
 * 
 * Свойство IsEmailConfirmed отображает, подтверждена ли почта.
 * 
 * Свойство PasswordSalt хранит соль пароля, строго 64 байта (512 бит).
 * 
 * Свойство PasswordHash хранит хеш пароля, строго 64 байта (512 бит).
 * 
 * Свойство UserDevicesIds служит как FK для таблицы UserDevices и
 * содержит список Id всех девайсов, на которых у пользователя выполнен вход.
 * 
 * Свойство PayedUntil отображает дату и время, до которого у пользователя
 * имеется платный доступ.
 */


public class User
{
	public enum AccessLevels
	{
		Unconfirmed, // email
		Free,
		Payed,
		Admin
	}

	public AccessLevels GetAccessLevel()
	{
		if (this.IsAdmin) return AccessLevels.Admin;
		if (this.PayedUntil > DateTime.UtcNow) return AccessLevels.Payed;
		if (this.IsEmailConfirmed) return AccessLevels.Free;

		return AccessLevels.Unconfirmed;
	}


	public int Id { get; set; }
	[DefaultValue(false)] public bool IsAdmin { get; set; } = false;
	[MaxLength(50)] public string Email { get; set; }
	[DefaultValue(false)] public bool IsEmailConfirmed { get; set; } = false;
	[MaxLength(512/8)] public byte[] PasswordSalt { get; set; }
	[MaxLength(512 / 8)] public byte[] PasswordHash { get; set; }
	public List<long> UserDevicesIds { get; set; } // Foreign Keys for UserDevice model
	[DefaultValue(0)] public DateTime PayedUntil { get; set; } = DateTime.MinValue;
}
