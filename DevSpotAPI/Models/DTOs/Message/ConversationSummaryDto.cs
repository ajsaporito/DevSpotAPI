namespace DevSpotAPI.Models.DTOs.Message
{
    // Used in: MessagesPage (conversation list sidebar)
    public class ConversationSummaryDto
    {
        public int ChatId { get; set; }
        public int OtherUserId { get; set; }
        public string OtherUsername { get; set; } = null!;
        public string OtherFirstName { get; set; } = null!;
        public string OtherLastName { get; set; } = null!;
        public string? OtherProfilePicUrl { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
