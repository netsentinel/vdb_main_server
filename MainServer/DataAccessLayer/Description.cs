namespace DataAccessLayer;


/* Данная библиотека реализует доступ к данным из СУБД. На этапе проектирования существует вопрос выбора фреймфорка - EF или Dapper.
 * У автора настоящей разработки имеется как опыт работы с EF, так и с PgSQL, что, по сути, позвоялет использовать Dapper. 
 * В данном случае принято решение использовать EF и, при необходимости, Dapper, хотя, сложных запросов, требующих оптимизации,
 * пока не предвидится.
 * 
 * 
 * В данном проекте реализованы следующие модели:
 * 
 * User - сущность юзера.
 * int Id { get; set; }
 * bool IsAdmin
 *	[MaxLength(50)] public string Email { get; set; }
 *	[DefaultValue(false)] public bool IsEmailConfirmed { get; set; } = false;
 *	[MaxLength(512)] public byte[] PasswordSalt { get; set; }
 *	[MaxLength(512)] public byte[] PasswordHash { get; set; }
 *	[MaxLength(512)] public byte[] RefreshJwtKey { get; set; }
 *	public List<int> UserDevicesIds { get; set; } // Foreign Keys for UserDevice model
 * [DefaultValue(0)] public DateTime PayedUntil { get; set; } = DateTime.MinValue;
 */