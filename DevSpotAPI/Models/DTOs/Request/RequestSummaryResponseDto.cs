namespace DevSpotAPI.Models.DTOs.Request
{
    // Used in: ViewRequestsModal (proposal cards per job)
    public class RequestSummaryResponseDto
    {
        public int RequestId { get; set; }
        public int JobId { get; set; }
        public string Status { get; set; } = null!;     // "Pending", "Accepted", "Rejected", "Cancelled"
        public DateTime CreatedAt { get; set; }

        // Job pay info (client-set, no bidding)
        public string PayType { get; set; } = null!;   // "Hourly" or "Flat"
        public decimal? HourlyRate { get; set; }
        public decimal? FlatAmount { get; set; }

        // Freelancer info
        public int FreelancerId { get; set; }
        public string FreelancerUsername { get; set; } = null!;
        public string FreelancerFirstName { get; set; } = null!;
        public string FreelancerLastName { get; set; } = null!;
        public string? FreelancerProfilePicUrl { get; set; }
        public string? FreelancerLocationCity { get; set; }
        public string? FreelancerLocationCountry { get; set; }
        public List<string> FreelancerSkills { get; set; } = [];
    }
}
