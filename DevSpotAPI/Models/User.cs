namespace DevSpotAPI.Models
{
	public class User
	{
		public int UserId { get; set; }
		public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
		public string Email { get; set; } = null!;
		public string Username { get; set; } = null!;
		public string PasswordHash { get; set; } = null!;
		public string? ProfilePicUrl { get; set; }
		public string? Bio { get; set; }
		public string? LocationCountry { get; set; }
		public string? LocationCity { get; set; }
		public bool IsAdmin { get; set; }
		public bool IsVerified { get; set; }
		public DateTime CreatedAt { get; set; }

		public ICollection<UserEducation> Educations { get; set; } = new List<UserEducation>();
		public ICollection<UserWorkHistory> WorkHistories { get; set; } = new List<UserWorkHistory>();
		public ICollection<UserLicense> Licenses { get; set; } = new List<UserLicense>();
		public ICollection<UserPortfolioProject> PortfolioProjects { get; set; } = new List<UserPortfolioProject>();

		public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();

		public ICollection<Job> JobsAsClient { get; set; } = new List<Job>();
		public ICollection<Job> JobsAsFreelancer { get; set; } = new List<Job>();

		public ICollection<Review> ReviewsGivenAsClient { get; set; } = new List<Review>();
		public ICollection<Review> ReviewsReceivedAsFreelancer { get; set; } = new List<Review>();

		public ICollection<Request> RequestsMade { get; set; } = new List<Request>();

		public ICollection<Chat> ChatsAsUserA { get; set; } = new List<Chat>();
		public ICollection<Chat> ChatsAsUserB { get; set; } = new List<Chat>();

		public ICollection<Message> MessagesSent { get; set; } = new List<Message>();

		public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
	}
}
