namespace DevSpotAPI.Models.DTOs.Auth
{
	public sealed class AuthResponseDto
	{
		public string AccessToken { get; set; } = "";
		public DateTime ExpiresAtUtc { get; set; }
		public int UserId { get; set; }
		public string Username { get; set; } = "";
		public string? ProfilePicUrl { get; set; }
	}
}
