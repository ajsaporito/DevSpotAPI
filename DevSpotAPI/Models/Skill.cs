namespace DevSpotAPI.Models
{
	public class Skill
	{
		public int SkillId { get; set; }
		public string Name { get; set; } = null!;

		public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
		public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
	}
}
