namespace DevSpotAPI.Models.DTOs.Message
{
    public class SendMessageDto
    {
        // Controller will find or create the chat
        public int OtherUserId { get; set; }
        public string Text { get; set; } = null!;
    }
}
