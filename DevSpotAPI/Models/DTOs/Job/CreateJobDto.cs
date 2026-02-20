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
        public List<IFormFile>? Attachments { get; set; }
    }
}

// -------------------------------------------------------
// Enums to add to Job.cs (or a separate Enums file)
// -------------------------------------------------------

public enum JobDuration
{
    LessThanOneMonth = 0,
    OneToThreeMonths = 1,
    ThreeToSixMonths = 2,
    MoreThanSixMonths = 3
}

public enum ExperienceLevel
{
    EntryLevel = 0,
    Intermediate = 1,
    Expert = 2
}

// -------------------------------------------------------
// Fields to ADD to Job model (Job.cs)
// -------------------------------------------------------
// public JobDuration? Duration { get; set; }
// public ExperienceLevel? ExperienceLevel { get; set; }
//
// Note: Budget maps to the existing FlatAmount (Fixed Price)
// or HourlyRate (Hourly) fields depending on PayType.
// Attachments are optional and will need a separate
// storage/upload strategy (e.g. local disk or cloud).