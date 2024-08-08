using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Contexts;


/* MigrateAsync() on app startup recommended.
 */
public class VpnContext : DbContext
{
	public VpnContext(DbContextOptions<VpnContext> options)
		: base(options)
	{

	}

	public DbSet<User> Users { get; protected set; }
	public DbSet<UserDevice> Devices { get; protected set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<User>(entity => {
			entity.HasAlternateKey(u => u.Email);
		});

		modelBuilder.Entity<UserDevice>(entity => {
			entity.HasAlternateKey(u => u.WireguardPublicKey);
		});
	}
}
