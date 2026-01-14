namespace DevSpotAPI.Models
{
	public class Request
	{
		public int RequestId { get; set; }
		public int JobId { get; set; }
		public int RequestedById { get; set; }

		public RequestStatus Status { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? AcceptedAt { get; set; }

		public Job Job { get; set; } = null!;
		public User RequestedBy { get; set; } = null!;

		public enum RequestStatus
		{
			Pending = 0,
			Accepted = 1,
			Rejected = 2,
			Cancelled = 3
		}
	}
}
