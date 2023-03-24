using Microsoft.AspNetCore.Mvc;

namespace main_server_api.Controllers;


#pragma warning disable IDE0060
public static class ControllerBaseExtensions
{
	[NonAction]
	public static bool ValidatePubkey(this ControllerBase ctr, string pk, int strictBytesCount = -1)
	{
		return !string.IsNullOrWhiteSpace(pk)
			&& pk.Length < 1024
			&& Convert.TryFromBase64String(pk, new byte[pk.Length], out var bytesCount)
			&& (strictBytesCount == -1 || bytesCount == strictBytesCount);
	}

	[NonAction]
	public static async IAsyncEnumerable<T> IncapsulateEnumerator<T>(this ControllerBase ctr, IAsyncEnumerator<T> enumerator)
	{
		while (await enumerator.MoveNextAsync())
			yield return enumerator.Current;
	}
}
