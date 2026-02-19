namespace DevSpotAPI.Models.DTOs.User
{
	public sealed class UploadProfilePhotoRequestDto
	{
		public IFormFile File { get; set; } = null!;
	}
}
