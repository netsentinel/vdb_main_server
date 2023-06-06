using Microsoft.AspNetCore.Mvc;
using ServicesLayer.Models.Common;
using System.Security.Claims;

namespace main_server_api.Controllers;


#pragma warning disable IDE0060
public static class ControllerBaseExtensions
{
	[NonAction]
	public static bool ValidatePubkey(this ControllerBase ctr, string pk, int strictBytesCount = -1)
	{
		return !string.IsNullOrWhiteSpace(pk)
			&& pk.Length < 1024 * 4 / 3 + 4 // < +4 or <= +3, I personally prefer < over <=... dont annoy me with this!
			&& Convert.TryFromBase64String(pk, new byte[pk.Length], out var bytesCount)
			&& (strictBytesCount == -1 || bytesCount == strictBytesCount);
	}
	[NonAction]
	public static bool ValidatePubkeyWithoutDecoding(this ControllerBase ctr, string pk, int strictBytesCount = 256 / 8)
	{
		return !string.IsNullOrWhiteSpace(pk)
			&& pk.Length <= strictBytesCount * 4 / 3 + 3;
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

	/// <exception cref="ArgumentNullException"/>
	/// <exception cref="FormatException"/>
	/// <exception cref="OverflowException"/>
	[NonAction]
	public static bool ParseAdminClaim(this ControllerBase ctr) => bool.Parse(ctr.User.FindFirstValue(nameof(UserInfo.IsAdmin))!);
}
