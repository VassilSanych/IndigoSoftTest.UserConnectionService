using Microsoft.EntityFrameworkCore;

namespace UserConnectionService.Data
{
	public class AppDbContext : DbContext
	{
		public DbSet<UserConnection> UserConnections { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION"));
		}

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
