namespace DevSpotAPI.Models
{
	public class UserEducation
	{
		public int UserEducationId { get; set; }
		public int UserId { get; set; }

		public string InstitutionName { get; set; } = null!;
		public string Degree { get; set; } = null!;
		public string FieldOfStudy { get; set; } = null!;
		public DateTime StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public string? Description { get; set; }

		public User User { get; set; } = null!;
	}
}
