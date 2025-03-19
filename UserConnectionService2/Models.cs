using System.ComponentModel.DataAnnotations;

namespace UserConnectionService
{
	public class UserConnection
	{
		[Key]
		public int Id { get; set; }

		public long UserId { get; set; }

		[MaxLength(45)] // Для IPv6
		public string IpAddress { get; set; }

		public DateTime Timestamp { get; set; }
	}
}