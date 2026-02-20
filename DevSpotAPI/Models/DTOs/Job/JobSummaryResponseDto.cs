namespace DevSpotAPI.Models.DTOs.Job
{
    // Used in: FindJobsPage (job listing cards), HybridDashboard (job list)
    public class JobSummaryResponseDto
    {
        public int JobId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }

        public string PayType { get; set; } = null!;   // "Hourly" or "Flat"
        public decimal? HourlyRate { get; set; }
        public decimal? FlatAmount { get; set; }

        public string? Duration { get; set; }           // e.g. "1-3 months"
        public string? ExperienceLevel { get; set; }    // "EntryLevel", "Intermediate", "Expert"
        public string Status { get; set; } = null!;     // "Open", "InProgress", etc.

        public List<string> Skills { get; set; } = [];

        public DateTime CreatedAt { get; set; }

        // Basic client info for the card
        public int ClientId { get; set; }
        public string ClientUsername { get; set; } = null!;
        public string? ClientProfilePicUrl { get; set; }
    }
}
