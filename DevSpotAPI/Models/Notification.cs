namespace DevSpotAPI.Models
{
	public class Notification
	{
		public int NotificationId { get; set; }
		public int UserId { get; set; }

		public int? ChatId { get; set; }
		public int? MessageId { get; set; }
		public int? JobId { get; set; }
		public int? RequestId { get; set; }
		public int? ReviewId { get; set; }

		public NotificationType Type { get; set; }
		public string Title { get; set; } = null!;
		public decimal? Rating { get; set; }

		public bool IsRead { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? ReadAt { get; set; }

		public User User { get; set; } = null!;
		public Chat? Chat { get; set; }
		public Message? Message { get; set; }
		public Job? Job { get; set; }
		public Request? Request { get; set; }
		public Review? Review { get; set; }
	}

	public enum NotificationType
	{
		Message = 0,
		Request = 1,
		Review = 2,
		JobUpdate = 3
	}
}
