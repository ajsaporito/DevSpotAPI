namespace DevSpotAPI.Models.DTOs.Job
{
    public class JobDetailResponseDto
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
        public string? Location { get; set; }

        public List<string> Skills { get; set; } = [];

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? EstCompletionDate { get; set; }

        // HasReview: true if a review has already been submitted for this job.
        // Use this on the frontend to show/hide the "Leave a Review" button
        // and on the backend to block duplicate review submissions.
        public bool HasReview { get; set; }

        // Full client info
        public int ClientId { get; set; }
        public string ClientUsername { get; set; } = null!;
        public string ClientFirstName { get; set; } = null!;
        public string ClientLastName { get; set; } = null!;
        public string? ClientProfilePicUrl { get; set; }
        public string? ClientLocationCity { get; set; }
        public string? ClientLocationCountry { get; set; }

        // Assigned freelancer (null if job is still open)
        public int? FreelancerId { get; set; }
        public string? FreelancerUsername { get; set; }
        public string? FreelancerProfilePicUrl { get; set; }

        // Total number of proposals submitted for this job.
        // Populated in the controller via .Include(j => j.Requests) then job.Requests.Count
        public int RequestCount { get; set; }
    }
}
