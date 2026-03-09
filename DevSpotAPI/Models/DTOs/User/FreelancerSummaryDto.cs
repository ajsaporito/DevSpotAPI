namespace DevSpotAPI.Models.DTOs.User
{
	public class FreelancerSummaryDto
	{
		public int UserId { get; set; }
		public string Username { get; set; } = null!;
		public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
		public string? Title { get; set; }
		public string? Location { get; set; }
		public decimal? HourlyRate { get; set; }
		public string? ProfilePicUrl { get; set; }
		public List<string> Skills { get; set; } = [];
		public string? Bio { get; set; }
		public int JobSuccessRate { get; set; }
		public decimal TotalEarned { get; set; }
		public bool IsVerified { get; set; }
		public bool IsTopRated { get; set; }
		public bool IsAvailable { get; set; }
	}
}
