namespace DevSpotAPI.Models.DTOs.Review
{
    public class ReviewResponseDto
    {
        public int ReviewId { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = null!;
        public decimal Rating { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }

        // Client info
        public int ClientId { get; set; }
        public string ClientUsername { get; set; } = null!;
        public string? ClientProfilePicUrl { get; set; }

        // Freelancer info
        public int FreelancerId { get; set; }
        public string FreelancerUsername { get; set; } = null!;
        public string? FreelancerProfilePicUrl { get; set; }
    }
}
