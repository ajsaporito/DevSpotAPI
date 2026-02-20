namespace DevSpotAPI.Models.DTOs.Message
{
    public class MessageResponseDto
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string SenderUsername { get; set; } = null!;
        public string? SenderProfilePicUrl { get; set; }
        public string Text { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsOwn { get; set; }
    }
}
