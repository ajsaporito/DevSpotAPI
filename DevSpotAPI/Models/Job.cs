namespace DevSpotAPI.Models
{
	public class Job
	{
		public int JobId { get; set; }

		public int ClientId { get; set; }
		public int? FreelancerId { get; set; }

		public string Title { get; set; } = null!;
		public string? Description { get; set; }

		public JobStatus Status { get; set; }
		public PayType PayType { get; set; }

		public decimal? HourlyRate { get; set; }
		public decimal? FlatAmount { get; set; }

		public string? Location { get; set; }
		public bool HasReview { get; set; }

		public decimal? EstHoursWeekly { get; set; }
		public DateTime? EstCompletionDate { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		// nav
		public User Client { get; set; } = null!;
		public User? Freelancer { get; set; }

		public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
		public ICollection<Request> Requests { get; set; } = new List<Request>();
		public ICollection<Review> Reviews { get; set; } = new List<Review>();
	}

	public enum JobStatus
	{
		Open = 0,
		InProgress = 1,
		Completed = 2,
		Cancelled = 3
	}

	public enum PayType
	{
		Hourly = 0,
		Flat = 1
	}
}
