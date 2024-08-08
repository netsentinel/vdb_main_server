namespace ServicesLayer.Services;
public sealed class StatisticsService
{
	/* DateTime size is 8 bytes.
	 * Let's imagine we got 50 endpoints, 
	 * and the 2^31 is the C# hard-coded list max len.
	 * 50 * 2^31 * 8 /1024/1024 = a few TBs...
	 * 50 * 2^16 * 8 /1024/1024 = 25 MB. Looks ok...
	 */
	private const int maxStored = ushort.MaxValue; // 2^16

	private readonly Dictionary<string, List<DateTime>> _enpointToRequestsTime = new();
	public IReadOnlyDictionary<string, List<DateTime>> EnpointToRequestsTime
		=> this._enpointToRequestsTime.AsReadOnly();

	public StatisticsService()
	{

	}

	public void Count(string endpointName)
	{
		if(!this._enpointToRequestsTime.ContainsKey(endpointName)) {
			this._enpointToRequestsTime.Add(endpointName, new List<DateTime>());
		}

		var list = this._enpointToRequestsTime[endpointName];

		if(list.Count == maxStored) {
			list.RemoveRange(0, maxStored / 2);
		}

		list.Add(DateTime.UtcNow);
	}
}
