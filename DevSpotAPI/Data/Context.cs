using Microsoft.EntityFrameworkCore;
using DevSpotAPI.Models;

namespace DevSpotAPI.Data
{
	public class Context : DbContext
	{
		public Context(DbContextOptions<Context> options) : base(options) { }

		public DbSet<User> Users => Set<User>();
		public DbSet<Job> Jobs => Set<Job>();
		public DbSet<Skill> Skills => Set<Skill>();

		public DbSet<UserSkill> UserSkills => Set<UserSkill>();
		public DbSet<JobSkill> JobSkills => Set<JobSkill>();

		public DbSet<UserEducation> UserEducations => Set<UserEducation>();
		public DbSet<UserWorkHistory> UserWorkHistories => Set<UserWorkHistory>();
		public DbSet<UserLicense> UserLicenses => Set<UserLicense>();
		public DbSet<UserPortfolioProject> UserPortfolioProjects => Set<UserPortfolioProject>();

		public DbSet<Request> Requests => Set<Request>();
		public DbSet<Review> Reviews => Set<Review>();

		public DbSet<Chat> Chats => Set<Chat>();
		public DbSet<Message> Messages => Set<Message>();

		public DbSet<Notification> Notifications => Set<Notification>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// ---------- Keys (many-to-many join tables) ----------
			modelBuilder.Entity<UserSkill>()
				.HasKey(x => new { x.UserId, x.SkillId });

			modelBuilder.Entity<JobSkill>()
				.HasKey(x => new { x.JobId, x.SkillId });

			// ---------- Relationships ----------
			modelBuilder.Entity<UserSkill>()
				.HasOne(x => x.User)
				.WithMany(u => u.UserSkills)
				.HasForeignKey(x => x.UserId);

			modelBuilder.Entity<UserSkill>()
				.HasOne(x => x.Skill)
				.WithMany(s => s.UserSkills)
				.HasForeignKey(x => x.SkillId);

			modelBuilder.Entity<JobSkill>()
				.HasOne(x => x.Job)
				.WithMany(j => j.JobSkills)
				.HasForeignKey(x => x.JobId);

			modelBuilder.Entity<JobSkill>()
				.HasOne(x => x.Skill)
				.WithMany(s => s.JobSkills)
				.HasForeignKey(x => x.SkillId);

			// Job: Client + Freelancer both -> User
			modelBuilder.Entity<Job>()
				.HasOne(j => j.Client)
				.WithMany(u => u.JobsAsClient)
				.HasForeignKey(j => j.ClientId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Job>()
				.HasOne(j => j.Freelancer)
				.WithMany(u => u.JobsAsFreelancer)
				.HasForeignKey(j => j.FreelancerId)
				.OnDelete(DeleteBehavior.Restrict);

			// Review: Job + Client(User) + Freelancer(User)
			modelBuilder.Entity<Review>()
				.HasOne(r => r.Job)
				.WithMany(j => j.Reviews)
				.HasForeignKey(r => r.JobId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Review>()
				.HasOne(r => r.Client)
				.WithMany(u => u.ReviewsGivenAsClient)
				.HasForeignKey(r => r.ClientId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Review>()
				.HasOne(r => r.Freelancer)
				.WithMany(u => u.ReviewsReceivedAsFreelancer)
				.HasForeignKey(r => r.FreelancerId)
				.OnDelete(DeleteBehavior.Restrict);

			// Request: Job + RequestedBy(User)
			modelBuilder.Entity<Request>()
				.HasOne(r => r.Job)
				.WithMany(j => j.Requests)
				.HasForeignKey(r => r.JobId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Request>()
				.HasOne(r => r.RequestedBy)
				.WithMany(u => u.RequestsMade)
				.HasForeignKey(r => r.RequestedById)
				.OnDelete(DeleteBehavior.Restrict);

			// Chat: UserA + UserB both -> User
			modelBuilder.Entity<Chat>()
				.HasOne(c => c.UserA)
				.WithMany(u => u.ChatsAsUserA)
				.HasForeignKey(c => c.UserAId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Chat>()
				.HasOne(c => c.UserB)
				.WithMany(u => u.ChatsAsUserB)
				.HasForeignKey(c => c.UserBId)
				.OnDelete(DeleteBehavior.Restrict);

			// Message: Chat + Sender(User)
			modelBuilder.Entity<Message>()
				.HasOne(m => m.Chat)
				.WithMany(c => c.Messages)
				.HasForeignKey(m => m.ChatId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Message>()
				.HasOne(m => m.Sender)
				.WithMany(u => u.MessagesSent)
				.HasForeignKey(m => m.SenderId)
				.OnDelete(DeleteBehavior.Restrict);

			// One-to-many profile tables
			modelBuilder.Entity<UserEducation>()
				.HasOne(e => e.User)
				.WithMany(u => u.Educations)
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<UserWorkHistory>()
				.HasOne(w => w.User)
				.WithMany(u => u.WorkHistories)
				.HasForeignKey(w => w.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<UserLicense>()
				.HasOne(l => l.User)
				.WithMany(u => u.Licenses)
				.HasForeignKey(l => l.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<UserPortfolioProject>()
				.HasOne(p => p.User)
				.WithMany(u => u.PortfolioProjects)
				.HasForeignKey(p => p.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// Notification: User + optional links
			modelBuilder.Entity<Notification>()
				.HasOne(n => n.User)
				.WithMany(u => u.Notifications)
				.HasForeignKey(n => n.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Notification>()
				.HasOne(n => n.Chat)
				.WithMany()
				.HasForeignKey(n => n.ChatId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Notification>()
				.HasOne(n => n.Message)
				.WithMany()
				.HasForeignKey(n => n.MessageId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Notification>()
				.HasOne(n => n.Job)
				.WithMany()
				.HasForeignKey(n => n.JobId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Notification>()
				.HasOne(n => n.Request)
				.WithMany()
				.HasForeignKey(n => n.RequestId)
				.OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Notification>()
				.HasOne(n => n.Review)
				.WithMany()
				.HasForeignKey(n => n.ReviewId)
				.OnDelete(DeleteBehavior.SetNull);

			// ---------- Indexes / Constraints ----------
			modelBuilder.Entity<User>()
				.HasIndex(u => u.Email)
				.IsUnique();

			modelBuilder.Entity<User>()
				.HasIndex(u => u.Username)
				.IsUnique();

			// One chat thread per pair of users (normalized ordering: UserAId < UserBId)
			modelBuilder.Entity<Chat>()
				.HasIndex(c => new { c.UserAId, c.UserBId })
				.IsUnique();

			modelBuilder.Entity<Chat>()
				.ToTable(t => t.HasCheckConstraint(
					"CK_Chat_UserAId_LessThan_UserBId",
					"\"UserAId\" < \"UserBId\""
				));

			// Messages indexes
			modelBuilder.Entity<Message>()
				.HasIndex(m => new { m.ChatId, m.CreatedAt });

			// (Optional) these two help inbox lookups a bit
			modelBuilder.Entity<Chat>().HasIndex(c => c.UserAId);
			modelBuilder.Entity<Chat>().HasIndex(c => c.UserBId);

			modelBuilder.Entity<Job>().HasIndex(j => j.ClientId);
			modelBuilder.Entity<Job>().HasIndex(j => j.FreelancerId);
		}
	}
}
