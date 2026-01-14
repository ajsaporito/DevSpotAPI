namespace DevSpotAPI.Models
{
	public class Chat
	{
		public int ChatId { get; set; }
		public int UserAId { get; set; }
		public int UserBId { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime? LastMessageAt { get; set; }

		public User UserA { get; set; } = null!;
		public User UserB { get; set; } = null!;

		public ICollection<Message> Messages { get; set; } = new List<Message>();
	}
}
