using System.ComponentModel.DataAnnotations;

namespace DevSpotAPI.Models.DTOs.User
{
	public class ChangePasswordDto
	{
		[Required, StringLength(200)]
		public string CurrentPassword { get; set; } = string.Empty;

		[Required, StringLength(200)]
		public string NewPassword { get; set; } = string.Empty;

		[Required, StringLength(200)]
		public string ConfirmNewPassword { get; set; } = string.Empty;
	}
}
