using System.ComponentModel.DataAnnotations;

namespace DevSpotAPI.Models.DTOs.Auth
{
	public sealed class RegisterRequestDto
	{
		[Required]
		[StringLength(50, MinimumLength = 1)]
		[RegularExpression(@"^[A-Za-z][A-Za-z\s'\-]*$", ErrorMessage = "First name can only contain letters, spaces, apostrophes, and hyphens.")]
		public string FirstName { get; set; } = "";

		[Required]
		[StringLength(50, MinimumLength = 1)]
		[RegularExpression(@"^[A-Za-z][A-Za-z\s'\-]*$", ErrorMessage = "Last name can only contain letters, spaces, apostrophes, and hyphens.")]
		public string LastName { get; set; } = "";

		[Required]
		[StringLength(30, MinimumLength = 3)]
		// allows letters, numbers, underscore, dot; must start with letter/number
		[RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._]{2,29}$", ErrorMessage = "Username must be at least 3 characters and contain only letters, numbers, '.' or '_'")]
		public string Username { get; set; } = "";

		[Required]
		[EmailAddress(ErrorMessage = "Email is not valid.")]
		[StringLength(255)]
		public string Email { get; set; } = "";

		[Required]
		[MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
		public string Password { get; set; } = "";

		// optional
		public string? LocationCountry { get; set; }
		public string? LocationCity { get; set; }
		[StringLength(500)]
		public string? Bio { get; set; }
		public string? ProfilePicUrl { get; set; }

		public List<string> Skills { get; set; } = new();
	}
}
