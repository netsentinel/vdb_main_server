using DataAccessLayer.Models;
using main_server_api.Models.UserApi.Website.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace main_server_api.Controllers;


#pragma warning disable IDE0060
public static class ControllerBaseExtensions
{
	[NonAction]
	public static bool ValidatePubkey(this ControllerBase ctr, string pk, int strictBytesCount = -1)
	{
		return !string.IsNullOrWhiteSpace(pk)
			&& pk.Length < (1024 * 4 / 3 + 4) // < +4 or <= +3, I personally prefer < over <=... dont annoy me with this!
			&& Convert.TryFromBase64String(pk, new byte[pk.Length], out var bytesCount)
			&& (strictBytesCount == -1 || bytesCount == strictBytesCount);
	}

	/// <exception cref="NullReferenceException"/>
	[NonAction]
	public static async IAsyncEnumerable<T> IncapsulateEnumerator<T>(this ControllerBase ctr, IAsyncEnumerator<T> enumerator)
	{
		while(await enumerator.MoveNextAsync())
			yield return enumerator.Current;
	}

	/// <exception cref="ArgumentNullException"/>
	/// <exception cref="FormatException"/>
	/// <exception cref="OverflowException"/>
	[NonAction]
	public static int ParseIdClaim(this ControllerBase ctr) => int.Parse(ctr.User.FindFirstValue(nameof(UserInfo.Id))!);
}
