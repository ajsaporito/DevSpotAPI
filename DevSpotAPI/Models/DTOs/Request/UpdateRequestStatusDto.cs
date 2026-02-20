namespace DevSpotAPI.Models.DTOs.Request
{
    public class UpdateRequestStatusDto
    {
        // Allowed values: "Accepted", "Rejected", "Cancelled"
        public string Status { get; set; } = null!;
    }
}
