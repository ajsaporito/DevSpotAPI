namespace DevSpotAPI.Models
{
	public class Review
	{
		public int ReviewId { get; set; }
		public int JobId { get; set; }
		public int ClientId { get; set; }
		public int FreelancerId { get; set; }

		public decimal Rating { get; set; }
		public string? Comments { get; set; }
		public DateTime CreatedAt { get; set; }

		public Job Job { get; set; } = null!;
		public User Client { get; set; } = null!;
		public User Freelancer { get; set; } = null!;
	}
}
