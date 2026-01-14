namespace DevSpotAPI.Models
{
	public class UserWorkHistory
	{
		public int UserWorkHistoryId { get; set; }
		public int UserId { get; set; }

		public string Company { get; set; } = null!;
		public string RoleTitle { get; set; } = null!;
		public DateTime StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public bool CurrentlyEmployed { get; set; }
		public string? Description { get; set; }

		public User User { get; set; } = null!;
	}
}
