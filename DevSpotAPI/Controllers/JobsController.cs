using DevSpotAPI.Data;
using DevSpotAPI.Models;
using DevSpotAPI.Models.DTOs.Job;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DevSpotAPI.Controllers
{
	[ApiController]
	[Route("api/jobs")]
	public class JobsController : ControllerBase
	{
		private readonly Context _ctx;

		public JobsController(Context ctx)
		{
			_ctx = ctx;
		}

		// GET: api/jobs
		[HttpGet]
		public async Task<ActionResult<List<JobSummaryResponseDto>>> GetJobs()
		{
			var jobs = await _ctx.Jobs
				.Where(j => j.Status == JobStatus.Open)
				.Include(j => j.Client)
				.Include(j => j.JobSkills).ThenInclude(js => js.Skill)
				.OrderByDescending(j => j.CreatedAt)
				.ToListAsync();

			return jobs.Select(ToSummary).ToList();
		}

		// GET: api/jobs/{id}
		[HttpGet("{id}")]
		public async Task<ActionResult<JobDetailResponseDto>> GetJob(int id)
		{
			var job = await _ctx.Jobs
				.Include(j => j.Client)
				.Include(j => j.Freelancer)
				.Include(j => j.JobSkills).ThenInclude(js => js.Skill)
				.Include(j => j.Requests)
				.SingleOrDefaultAsync(j => j.JobId == id);

			if (job == null) return NotFound();

			return ToDetail(job);
		}

		// GET: api/jobs/my
		[Authorize]
		[HttpGet("my")]
		public async Task<ActionResult<List<JobSummaryResponseDto>>> GetMyJobs()
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var jobs = await _ctx.Jobs
				.Where(j => j.ClientId == userId)
				.Include(j => j.Client)
				.Include(j => j.JobSkills).ThenInclude(js => js.Skill)
				.OrderByDescending(j => j.CreatedAt)
				.ToListAsync();

			return jobs.Select(ToSummary).ToList();
		}

		// POST: api/jobs
		[Authorize]
		[HttpPost]
		public async Task<ActionResult<JobDetailResponseDto>> CreateJob([FromBody] CreateJobDto dto)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var skillNames = dto.Skills
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.Distinct()
				.ToList();

			await using var tx = await _ctx.Database.BeginTransactionAsync();

			var job = new Job
			{
				ClientId = userId.Value,
				Title = dto.Title.Trim(),
				Description = dto.Description.Trim(),
				Status = JobStatus.Open,
				PayType = dto.PayType,
				HourlyRate = dto.PayType == PayType.Hourly ? dto.Budget : null,
				FlatAmount = dto.PayType == PayType.Flat ? dto.Budget : null,
				Duration = dto.Duration,
				ExperienceLevel = dto.ExperienceLevel,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_ctx.Jobs.Add(job);
			await _ctx.SaveChangesAsync();

			if (skillNames.Count > 0)
			{
				var existingSkills = await _ctx.Skills
					.Where(s => skillNames.Contains(s.Name))
					.ToListAsync();

				var existingSet = existingSkills.Select(s => s.Name).ToHashSet();

				var newSkills = skillNames
					.Where(n => !existingSet.Contains(n))
					.Select(n => new Skill { Name = n })
					.ToList();

				if (newSkills.Count > 0)
				{
					_ctx.Skills.AddRange(newSkills);
					await _ctx.SaveChangesAsync();
					existingSkills.AddRange(newSkills);
				}

				var jobSkills = existingSkills.Select(s => new JobSkill
				{
					JobId = job.JobId,
					SkillId = s.SkillId
				});

				_ctx.JobSkills.AddRange(jobSkills);
				await _ctx.SaveChangesAsync();
			}

			await tx.CommitAsync();

			var created = await _ctx.Jobs
				.Include(j => j.Client)
				.Include(j => j.JobSkills).ThenInclude(js => js.Skill)
				.Include(j => j.Requests)
				.SingleAsync(j => j.JobId == job.JobId);

			return CreatedAtAction(nameof(GetJob), new { id = job.JobId }, ToDetail(created));
		}

		// PUT: api/jobs/{id}
		[Authorize]
		[HttpPut("{id}")]
		public async Task<ActionResult<JobDetailResponseDto>> UpdateJob(int id, [FromBody] CreateJobDto dto)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var job = await _ctx.Jobs
				.Include(j => j.Client)
				.Include(j => j.Freelancer)
				.Include(j => j.JobSkills).ThenInclude(js => js.Skill)
				.Include(j => j.Requests)
				.SingleOrDefaultAsync(j => j.JobId == id);

			if (job == null) return NotFound();
			if (job.ClientId != userId) return Forbid();

			var skillNames = dto.Skills
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.Distinct()
				.ToList();

			await using var tx = await _ctx.Database.BeginTransactionAsync();

			job.Title = dto.Title.Trim();
			job.Description = dto.Description.Trim();
			job.PayType = dto.PayType;
			job.HourlyRate = dto.PayType == PayType.Hourly ? dto.Budget : null;
			job.FlatAmount = dto.PayType == PayType.Flat ? dto.Budget : null;
			job.Duration = dto.Duration;
			job.ExperienceLevel = dto.ExperienceLevel;
			job.UpdatedAt = DateTime.UtcNow;

			_ctx.JobSkills.RemoveRange(job.JobSkills);
			await _ctx.SaveChangesAsync();

			if (skillNames.Count > 0)
			{
				var existingSkills = await _ctx.Skills
					.Where(s => skillNames.Contains(s.Name))
					.ToListAsync();

				var existingSet = existingSkills.Select(s => s.Name).ToHashSet();

				var newSkills = skillNames
					.Where(n => !existingSet.Contains(n))
					.Select(n => new Skill { Name = n })
					.ToList();

				if (newSkills.Count > 0)
				{
					_ctx.Skills.AddRange(newSkills);
					await _ctx.SaveChangesAsync();
					existingSkills.AddRange(newSkills);
				}

				var jobSkills = existingSkills.Select(s => new JobSkill
				{
					JobId = job.JobId,
					SkillId = s.SkillId
				});

				_ctx.JobSkills.AddRange(jobSkills);
			}

			await _ctx.SaveChangesAsync();
			await tx.CommitAsync();

			await _ctx.Entry(job).Collection(j => j.JobSkills).Query()
				.Include(js => js.Skill).LoadAsync();

			return ToDetail(job);
		}

		// DELETE: api/jobs/{id}
		[Authorize]
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteJob(int id)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var job = await _ctx.Jobs.SingleOrDefaultAsync(j => j.JobId == id);
			if (job == null) return NotFound();
			if (job.ClientId != userId) return Forbid();

			_ctx.Jobs.Remove(job);
			await _ctx.SaveChangesAsync();

			return NoContent();
		}

		// -------------------------------------------------------------------------
		// Helpers
		// -------------------------------------------------------------------------

		private int? GetUserId()
		{
			var uidStr = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(uidStr, out var id) ? id : null;
		}

		private static string DurationLabel(JobDuration? duration) => duration switch
		{
			JobDuration.LessThanOneMonth  => "Less than 1 month",
			JobDuration.OneToThreeMonths  => "1-3 months",
			JobDuration.ThreeToSixMonths  => "3-6 months",
			JobDuration.MoreThanSixMonths => "More than 6 months",
			_                             => "Unknown"
		};

		private static JobSummaryResponseDto ToSummary(Job job) => new()
		{
			JobId             = job.JobId,
			Title             = job.Title,
			Description       = job.Description,
			PayType           = job.PayType.ToString(),
			HourlyRate        = job.HourlyRate,
			FlatAmount        = job.FlatAmount,
			Duration          = job.Duration.HasValue ? DurationLabel(job.Duration) : null,
			ExperienceLevel   = job.ExperienceLevel?.ToString(),
			Status            = job.Status.ToString(),
			Skills            = job.JobSkills.Select(js => js.Skill.Name).ToList(),
			CreatedAt         = job.CreatedAt,
			ClientId          = job.ClientId,
			ClientUsername    = job.Client.Username,
			ClientProfilePicUrl = job.Client.ProfilePicUrl
		};

		private static JobDetailResponseDto ToDetail(Job job) => new()
		{
			JobId               = job.JobId,
			Title               = job.Title,
			Description         = job.Description,
			PayType             = job.PayType.ToString(),
			HourlyRate          = job.HourlyRate,
			FlatAmount          = job.FlatAmount,
			Duration            = job.Duration.HasValue ? DurationLabel(job.Duration) : null,
			ExperienceLevel     = job.ExperienceLevel?.ToString(),
			Status              = job.Status.ToString(),
			Location            = job.Location,
			Skills              = job.JobSkills.Select(js => js.Skill.Name).ToList(),
			CreatedAt           = job.CreatedAt,
			UpdatedAt           = job.UpdatedAt,
			EstCompletionDate   = job.EstCompletionDate,
			HasReview           = job.HasReview,
			ClientId            = job.ClientId,
			ClientUsername      = job.Client.Username,
			ClientFirstName     = job.Client.FirstName,
			ClientLastName      = job.Client.LastName,
			ClientProfilePicUrl = job.Client.ProfilePicUrl,
			ClientLocationCity  = job.Client.LocationCity,
			ClientLocationCountry = job.Client.LocationCountry,
			FreelancerId            = job.FreelancerId,
			FreelancerUsername      = job.Freelancer?.Username,
			FreelancerFirstName     = job.Freelancer?.FirstName,
			FreelancerLastName      = job.Freelancer?.LastName,
			FreelancerProfilePicUrl = job.Freelancer?.ProfilePicUrl,
			RequestCount        = job.Requests.Count
		};
	}
}
