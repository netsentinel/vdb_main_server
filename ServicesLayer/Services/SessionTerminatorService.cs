using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicesLayer.Services;

/* Данный сервис нужен для реализации функции завершения всех остальных сессий.
 * В данном случае требуется небольшой стейтфул, но все равно лучше чем писать
 * всё в базу данных. 
 * 
 * Когда пользователь завершает все сессии, мы берем лайфтайм access JWT, 
 * на время этого лайфтайма заносим сюда запись, что все JWT для юзера,
 * которые были выпущены до момента завершения сессий - недействительны.
 * 
 * Очищать устаревшие записи можно было бы фоновым таском, но я пришел
 * к выводу что проще делать это на каждом запросе, если прошел определенный
 * интервал, что называется, руководствуюсь KISS принципом.
 */
public class SessionTerminatorService
{
	// user id to minimal required 'iat' (issued at) value in access JWT token
	private Dictionary<int, DateTime> _userIdToMinimalAccessJwtIat;
	private int _accessLifeTimeSeconds;
	private int _addedSinceLastClean;

	private ILogger<SessionTerminatorService> _logger;

	public SessionTerminatorService(int accessLifeTimeSeconds, ILogger<SessionTerminatorService> logger)
	{
		_userIdToMinimalAccessJwtIat = new();
		_accessLifeTimeSeconds = accessLifeTimeSeconds;
		_addedSinceLastClean = 0;
		_cleanInProgress = false;

		_logger = logger;

		this._logger.LogInformation($"Created {nameof(SessionTerminatorService)}.");
	}


	private bool _cleanInProgress;
	private void CleanOutdated()
	{
		if(_cleanInProgress) return;
		_cleanInProgress = true;

		try
		{
			DateTime started = DateTime.UtcNow;

			_userIdToMinimalAccessJwtIat = _userIdToMinimalAccessJwtIat
				.Where(x => (DateTime.UtcNow - x.Value).TotalSeconds < _accessLifeTimeSeconds)
				.ToDictionary(x => x.Key, x => x.Value);

			_logger.LogInformation($"Cleanup completed, took {(DateTime.UtcNow - started).TotalMilliseconds.ToString("0.0")} ms.");
		}
		finally
		{
			_cleanInProgress = false;
		}
	}

	public void SetUserMinimalJwtIat(int usedId, DateTime minimalIat)
	{
		if(_userIdToMinimalAccessJwtIat.TryGetValue(usedId, out var currMinIat))
		{
			if(currMinIat < minimalIat) _userIdToMinimalAccessJwtIat[usedId] = minimalIat;
		}
		else
		{
			_userIdToMinimalAccessJwtIat.Add(usedId, currMinIat);
			_addedSinceLastClean++;

			// 16384 elements will consume not more than 5 MB here
			if(_addedSinceLastClean > 16384) CleanOutdated();
		}
	}

	public void SetUserMinimalJwtIat(int usedId)
	{
		SetUserMinimalJwtIat(usedId, DateTime.UtcNow.AddSeconds(-3));
	}


	/// <returns>Minimal 'iat' attribute value if userId was found, <see cref="DateTime.MinValue"/> otherwise</returns>
	public DateTime GetUserMinimalJwtIat(int userId)
	{
		var exists = _userIdToMinimalAccessJwtIat.TryGetValue(userId, out var currMinIat);

		return exists ? currMinIat : DateTime.MinValue;
	}

	/// <returns>Is access jwt with passed iat can be used</returns>
	public bool ValidateIat(int userId, DateTime iatUtc)
	{
		return iatUtc > GetUserMinimalJwtIat(userId);
	}
}
