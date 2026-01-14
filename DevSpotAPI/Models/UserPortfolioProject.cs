namespace DevSpotAPI.Models
{
	public class UserPortfolioProject
	{
		public int UserPortfolioProjectId { get; set; }
		public int UserId { get; set; }

		public string Title { get; set; } = null!;
		public string? Summary { get; set; }
		public string? ProjectUrl { get; set; }
		public string? RepoUrl { get; set; }
		public string? Thumbnail { get; set; }
		public DateTime CreatedAt { get; set; }

		public User User { get; set; } = null!;
	}
}
