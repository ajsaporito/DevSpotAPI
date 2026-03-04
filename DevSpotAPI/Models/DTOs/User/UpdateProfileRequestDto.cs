using System.ComponentModel.DataAnnotations;

namespace DevSpotAPI.Models.DTOs.User
{
	public class UpdateProfileRequestDto
	{
		[Required, StringLength(100)]
		public string Name { get; set; } = string.Empty;
		[StringLength(120)]
        public string? City { get; set; }

		[StringLength(120)]
		public string? Country { get; set; }

		[StringLength(4000)]
		public string? Bio { get; set; }
		public List<string> Skills { get; set; } = new();

		[StringLength(50)]
		public string? Username { get; set; }

		[EmailAddress, StringLength(254)]
		public string? Email { get; set; }

		public List<EducationDto> Education { get; set; } = new();

		public ChangePasswordDto? PasswordChange { get; set; }
	}
}
