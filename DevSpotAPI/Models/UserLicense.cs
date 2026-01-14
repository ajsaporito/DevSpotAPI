namespace DevSpotAPI.Models
{
	public class UserLicense
	{
		public int UserLicenseId { get; set; }
		public int UserId { get; set; }

		public string LicenseName { get; set; } = null!;
		public string? Issuer { get; set; }
		public DateTime IssueDate { get; set; }
		public DateTime? ExpiryDate { get; set; }

		public User User { get; set; } = null!;
	}
}
