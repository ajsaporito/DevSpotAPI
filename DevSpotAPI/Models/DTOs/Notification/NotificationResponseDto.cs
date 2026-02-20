namespace DevSpotAPI.Models.DTOs.Notification
{
    public class NotificationResponseDto
    {
        public int NotificationId { get; set; }
        public string Type { get; set; } = null!;   // "Message", "Request", "Review", "JobUpdate"
        public string Title { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public decimal? Rating { get; set; }

        // Use these to navigate on click
        public int? ChatId { get; set; }
        public int? JobId { get; set; }
        public int? RequestId { get; set; }
        public int? ReviewId { get; set; }
    }
}
