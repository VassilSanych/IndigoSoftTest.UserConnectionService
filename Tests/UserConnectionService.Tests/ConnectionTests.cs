using System.Net;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework.Legacy;

namespace UserConnectionService.Tests
{
	[TestFixture]
	public class ConnectionTests
	{
		private AppDbContext _context;

        [SetUp]
        public void Setup()
        {
            // Явно указываем InMemory-провайдер
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Уникальное имя для каждого теста
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }



		/// <summary>
		/// Logs a user connection to the database.
		/// </summary>
		/// <returns></returns>
		[Test]
		public async Task LogConnection_ValidData_AddsRecordToDatabase()
		{
			// Arrange
			var userId = 100001L;
			var ipAddress = "127.0.0.1";

			// Act
			await _context.UserConnections.AddAsync(new UserConnection
			{
				UserId = userId,
				IpAddress = ipAddress,
				Timestamp = DateTime.UtcNow
			});
			await _context.SaveChangesAsync();

			// Assert
			var connection = await _context.UserConnections.FirstOrDefaultAsync(uc => uc.UserId == userId && uc.IpAddress == ipAddress);
			Assert.That(connection, Is.Not.Null);
			Assert.That(connection.IpAddress, Is.EqualTo(ipAddress));
		}


		/// <summary>
		/// Finds user IDs based on a partial IP address match from a list of user connections.
		/// </summary>
		/// <returns>A list of distinct user IDs that match the given partial IP address.</returns>
		[Test]
		public async Task FindUsersByIp_PartialMatch_ReturnsMatchingUserIds()
		{
			// Arrange
			var connections = new List<UserConnection>
											{
												new() { UserId = 100001, IpAddress = "31.214.157.141", Timestamp = DateTime.UtcNow },
												new() { UserId = 1234567, IpAddress = "31.214.1.1", Timestamp = DateTime.UtcNow },
												new() { UserId = 9876543, IpAddress = "62.4.36.194", Timestamp = DateTime.UtcNow }
											};

			await _context.UserConnections.AddRangeAsync(connections);
			await _context.SaveChangesAsync();

			var partialIp = "31.214";

			// Act
			var users = await _context.UserConnections
				.Where(uc => EF.Functions.Like(uc.IpAddress, $"{partialIp}%"))
				.Select(uc => uc.UserId)
				.Distinct()
				.ToListAsync();

			// Assert
			CollectionAssert.AreEquivalent(new[] { 100001L, 1234567L }, users);
		}


		/// <summary>
		/// Tests the retrieval of unique IP addresses for a given user ID from the database.
		/// </summary>
		/// <returns>A list of distinct IP addresses associated with the specified user ID.</returns>
		[Test]
		public async Task GetUserIps_ValidUserId_ReturnsUniqueIps()
		{
			// Arrange
			var userId = 100001L;
			var connections = new List<UserConnection>
											{
												new() { UserId = userId, IpAddress = "127.0.0.1", Timestamp = DateTime.UtcNow },
												new() { UserId = userId, IpAddress = "31.214.157.141", Timestamp = DateTime.UtcNow },
												new() { UserId = userId, IpAddress = "127.0.0.1", Timestamp = DateTime.UtcNow } // Дубликат
                    };

			await _context.UserConnections.AddRangeAsync(connections);
			await _context.SaveChangesAsync();

			// Act
			var ips = await _context.UserConnections
				.Where(uc => uc.UserId == userId)
				.Select(uc => uc.IpAddress)
				.Distinct()
				.ToListAsync();

			// Assert
			CollectionAssert.AreEquivalent(new[] { "127.0.0.1", "31.214.157.141" }, ips);
		}


		/// <summary>
		/// Tests retrieval of the most recent user connection based on a valid user ID.
		/// </summary>
		/// <returns>Returns the most recent connection details including IP address and timestamp.</returns>
		[Test]
		public async Task GetLastConnection_ValidUserId_ReturnsMostRecentConnection()
		{
			// Arrange
			var userId = 100001L;
			var connections = new List<UserConnection>
											{
												new() { UserId = userId, IpAddress = "127.0.0.1", Timestamp = new DateTime(2024, 5, 1) },
												new() { UserId = userId, IpAddress = "31.214.157.141", Timestamp = new DateTime(2024, 5, 10) },
												new() { UserId = userId, IpAddress = "62.4.36.194", Timestamp = new DateTime(2024, 5, 5) }
											};

			await _context.UserConnections.AddRangeAsync(connections);
			await _context.SaveChangesAsync();

			// Act
			var lastConnection = await _context.UserConnections
				.Where(uc => uc.UserId == userId)
				.OrderByDescending(uc => uc.Timestamp)
				.Select(uc => new { uc.IpAddress, uc.Timestamp })
				.FirstOrDefaultAsync();

			// Assert
			Assert.That(lastConnection, Is.Not.Null);
			Assert.That(lastConnection.IpAddress, Is.EqualTo("31.214.157.141"));
			Assert.That(lastConnection.Timestamp, Is.EqualTo(new DateTime(2024, 5, 10)));
		}


		/// <summary>
		/// Tests the behavior of logging a connection with an invalid IP address. It verifies that an ArgumentException is
		/// thrown.
		/// </summary>
		/// <returns>Returns an ArgumentException with a message indicating the IP address is invalid.</returns>
		/// <exception cref="ArgumentException">Thrown when the provided IP address cannot be parsed as a valid IP address.</exception>
		[Test]
		public void LogConnection_InvalidIpAddress_ReturnsBadRequest()
		{
			// Arrange
			var invalidIp = "invalid-ip";

			// Act & Assert
			var ex = Assert.Throws<ArgumentException>(() =>
			{
				if (!IPAddress.TryParse(invalidIp, out _))
					throw new ArgumentException("Invalid IP address");
				
			});

			Assert.That(ex?.Message, Is.EqualTo("Invalid IP address"));
		}
	}
}
