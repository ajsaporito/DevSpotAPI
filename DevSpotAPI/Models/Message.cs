namespace DevSpotAPI.Models
{
	public class Message
	{
		public int MessageId { get; set; }
		public int ChatId { get; set; }
		public int SenderId { get; set; }

		public string Text { get; set; } = null!;
		public DateTime CreatedAt { get; set; }

		public Chat Chat { get; set; } = null!;
		public User Sender { get; set; } = null!;
	}
}
