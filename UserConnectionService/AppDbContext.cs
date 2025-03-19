using Microsoft.EntityFrameworkCore;

namespace UserConnectionService
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
	{
		public DbSet<UserConnection> UserConnections { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<UserConnection>()
				.HasIndex(uc => uc.IpAddress)
				.HasMethod("BTREE");

			modelBuilder.Entity<UserConnection>()
				.HasIndex(uc => new { uc.UserId, uc.Timestamp });
		}
	}
}