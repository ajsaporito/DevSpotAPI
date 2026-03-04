using System.ComponentModel.DataAnnotations;

namespace DevSpotAPI.Models.DTOs.User
{
	public class EducationDto
	{
		[StringLength(200)]
		public string? School { get; set; }

		[StringLength(200)]
		public string? Degree { get; set; }
		public string? FieldOfStudy { get; set; } = null!;

		[Required]
		public DateTime StartDate { get; set; }

		public DateTime? EndDate { get; set; }
	}
}
