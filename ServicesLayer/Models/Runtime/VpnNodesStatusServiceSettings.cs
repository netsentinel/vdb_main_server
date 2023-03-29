using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicesLayer.Models.Runtime;
public class VpnNodesStatusServiceSettings
{
	public int ReCacheIntervalSeconds { get; init; } = 60;
}
