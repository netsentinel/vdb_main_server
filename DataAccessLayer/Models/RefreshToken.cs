using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models;

/* Refresh-токены хранятся в базе данных в виде отдельной таблицы.
 * В неё заносятся как токены для устройств, так и токены для сайта.
 * 
 * Свойство Id служит не только как PK, но и как единственный 
 * claim генерируемого JWT-токена.
 * 
 * Свойство IssuedToUser существует для выдачи access токена юзеру,
 * которому был выдан refresh, с которым производится обращение.
 * 
 * Свойство ValidUntil служебное и не должно использоваться ни для 
 * каких целей, кроме как удаления устаревших токенов из базы данных.
 */
public class RefreshToken
{
	public long Id { get; set; } // there may be a lot of these... so use long
	public int IssuedToUser { get; set; }
	public DateTime ValidUntilUtc { get; set; }
}
