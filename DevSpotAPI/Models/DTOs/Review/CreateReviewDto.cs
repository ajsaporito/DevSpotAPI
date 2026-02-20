namespace DevSpotAPI.Models.DTOs.Review
{
    public class CreateReviewDto
    {
        public int JobId { get; set; }
        public decimal Rating { get; set; }   // 1-5
        public string? Comments { get; set; }
    }
}
