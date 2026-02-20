using DevSpotAPI.Models;

namespace DevSpotAPI.Models.DTOs.Job
{
    public class CreateJobDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<string> Skills { get; set; } = [];
        public PayType PayType { get; set; }
        public decimal? Budget { get; set; }
        public JobDuration Duration { get; set; }
        public ExperienceLevel ExperienceLevel { get; set; }

    }
}