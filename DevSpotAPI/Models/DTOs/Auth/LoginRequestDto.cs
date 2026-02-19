namespace DevSpotAPI.Models.DTOs.Auth
{
	public sealed class LoginRequestDto
	{
		public string EmailOrUsername { get; set; } = "";
		public string Password { get; set; } = "";
	}
}
