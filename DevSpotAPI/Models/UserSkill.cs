namespace DevSpotAPI.Models
{
	public class UserSkill
	{
		public int UserId { get; set; }
		public int SkillId { get; set; }

		public User User { get; set; } = null!;
		public Skill Skill { get; set; } = null!;
	}
}
