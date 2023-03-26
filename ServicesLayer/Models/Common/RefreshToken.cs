using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace main_server_api.Models.UserApi.Website.Common;

public class RefreshToken
{
	[Obsolete]
	public long Id { get; set; } // being used as an uuid actually

	public int IssuedToUser { get; set; }
	public long Entropy { get; set; }
}
