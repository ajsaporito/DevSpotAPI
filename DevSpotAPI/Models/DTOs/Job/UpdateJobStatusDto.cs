namespace DevSpotAPI.Models.DTOs.Job
{
	public class UpdateJobStatusDto
	{
		// Allowed values: "Open", "InProgress", "Completed"
		public string Status { get; set; } = null!;
	}
}
