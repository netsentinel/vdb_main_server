using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models;
public class RefreshToken
{
	public int Id { get; set; }
	public int IssuedToUser { get; set; }
	public DateTime ValidUntilUtc { get; set; }
}
