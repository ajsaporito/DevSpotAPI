using DevSpotAPI.Data;
using DevSpotAPI.Models;
using DevSpotAPI.Models.DTOs.User;
using DevSpotAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DevSpotAPI.Controllers
{
	[ApiController]
	[Route("api/users")]
	public class UsersController : ControllerBase
	{
		private readonly Context _ctx;
		private readonly IWebHostEnvironment _env;
		private readonly IConfiguration _config;
		private readonly PasswordService _passwords;

		public UsersController(Context ctx, IWebHostEnvironment env, IConfiguration config, PasswordService passwords)
		{
			_ctx = ctx;
			_env = env;
			_config = config;
			_passwords = passwords;
		}

		static DateTime AsUtc(DateTime dt) =>
			dt.Kind == DateTimeKind.Utc
				? dt
				: DateTime.SpecifyKind(dt, DateTimeKind.Utc);

		// GET: api/users/freelancers
		[HttpGet("freelancers")]
		public async Task<ActionResult<List<FreelancerSummaryDto>>> GetFreelancers([FromQuery] string? q)
		{
			var query = _ctx.Users
				.Include(u => u.UserSkills).ThenInclude(us => us.Skill)
				.Include(u => u.JobsAsFreelancer)
				.Where(u => u.UserSkills.Any()) // only users with skills = freelancers
				.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(u =>
					u.FirstName.ToLower().Contains(term) ||
					u.LastName.ToLower().Contains(term) ||
					u.Username.ToLower().Contains(term) ||
					(u.Bio != null && u.Bio.ToLower().Contains(term)) ||
					u.UserSkills.Any(us => us.Skill.Name.ToLower().Contains(term))
				);
			}

			var users = await query
				.OrderBy(u => u.FirstName)
				.ThenBy(u => u.LastName)
				.ToListAsync();

			return users.Select(ToFreelancerSummary).ToList();
		}

		// GET: api/users/{userId}/profile
		[HttpGet("{userId:int}/profile")]
		public async Task<ActionResult<FreelancerProfileDto>> GetFreelancerProfile(int userId)
		{
			var user = await _ctx.Users
				.Include(u => u.UserSkills).ThenInclude(us => us.Skill)
				.Include(u => u.JobsAsFreelancer)
				.AsNoTracking()
				.SingleOrDefaultAsync(u => u.UserId == userId);

			if (user == null) return NotFound();

			return ToFreelancerProfile(user);
		}

		// GET: api/users/profile
		[Authorize]
		[HttpGet("profile")]
		public async Task<ActionResult<object>> GetMyProfile()
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var user = await _ctx.Users
				.AsNoTracking()
				.Include(u => u.UserSkills)
					.ThenInclude(us => us.Skill)
				.Include(u => u.Educations)
				.SingleOrDefaultAsync(u => u.UserId == userId.Value);

			if (user == null) return Unauthorized();

			var result = new
			{
				// Frontend uses a single "name" field
				name = $"{user.FirstName} {user.LastName}".Trim(),

				city = user.LocationCity,
				country = user.LocationCountry,

				bio = user.Bio,
				username = user.Username,
				email = user.Email,
				profilePicUrl = user.ProfilePicUrl,

				skills = user.UserSkills
					.Select(us => us.Skill.Name)
					.OrderBy(x => x)
					.ToList(),

				education = user.Educations
					.OrderByDescending(e => e.UserEducationId)
					.Select(e => new
					{
						school = e.InstitutionName,
						degree = e.Degree,
						startDate = e.StartDate,
						endDate = e.EndDate
					})
					.ToList()
			};

			return Ok(result);
		}


		// PUT: api/users/profile
		[Authorize]
		[HttpPut("profile")]
		public async Task<ActionResult<object>> UpdateMyProfile([FromBody] UpdateProfileRequestDto dto)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			// IMPORTANT: tracked entity (NO AsNoTracking)
			var user = await _ctx.Users
				.Include(u => u.UserSkills)
				.Include(u => u.Educations)
				.SingleOrDefaultAsync(u => u.UserId == userId.Value);

			if (user == null) return Unauthorized();

			// ===== Basic fields =====
			var fullName = (dto.Name ?? "").Trim();
			if (string.IsNullOrWhiteSpace(fullName))
				return BadRequest(new { message = "Name is required." });

			var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			user.FirstName = parts[0];
			user.LastName = parts.Length > 1 ? parts[1] : "";

			user.LocationCity = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim();
			user.LocationCountry = string.IsNullOrWhiteSpace(dto.Country) ? null : dto.Country.Trim();
			user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();

			if (!string.IsNullOrWhiteSpace(dto.Username))
				user.Username = dto.Username.Trim();

			if (!string.IsNullOrWhiteSpace(dto.Email))
				user.Email = dto.Email.Trim();

			// ===== Skills (replace all) =====
			// Remove existing join rows
			_ctx.UserSkills.RemoveRange(user.UserSkills);

			var normalizedSkills = (dto.Skills ?? new List<string>())
				.Select(s => (s ?? "").Trim())
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (normalizedSkills.Count > 0)
			{
				// load skills that already exist
				var existingSkills = await _ctx.Skills
					.Where(s => normalizedSkills.Contains(s.Name))
					.ToListAsync();

				var existingNames = new HashSet<string>(
					existingSkills.Select(s => s.Name),
					StringComparer.OrdinalIgnoreCase
				);

				// add missing skills
				foreach (var name in normalizedSkills.Where(n => !existingNames.Contains(n)))
					_ctx.Skills.Add(new Skill { Name = name });

				await _ctx.SaveChangesAsync(); // ensures SkillId exists for new skills

				// re-load all skills for ids
				var allSkills = await _ctx.Skills
					.Where(s => normalizedSkills.Contains(s.Name))
					.ToListAsync();

				// add join rows using FK ids (NO User = user)
				foreach (var s in allSkills)
				{
					_ctx.UserSkills.Add(new UserSkill
					{
						UserId = user.UserId,
						SkillId = s.SkillId
					});
				}
			}

			// ===== Education (replace all) =====
			_ctx.UserEducations.RemoveRange(user.Educations);

			foreach (var edu in dto.Education ?? new List<EducationDto>())
			{
				_ = _ctx.UserEducations.Add(new UserEducation
				{
					UserId = user.UserId,
					InstitutionName = edu.School!,
					Degree = edu.Degree!,
					FieldOfStudy = "",
					StartDate = AsUtc(edu.StartDate),
					EndDate = edu.EndDate.HasValue ? AsUtc(edu.EndDate!.Value) : null
				});
			}

			// ===== Password change (optional) =====
			if (dto.PasswordChange != null)
			{
				var pc = dto.PasswordChange;

				if (!_passwords.Verify(user, pc.CurrentPassword))
					return BadRequest(new { message = "Current password is incorrect." });

				if (pc.NewPassword != pc.ConfirmNewPassword)
					return BadRequest(new { message = "Passwords do not match." });

				user.PasswordHash = _passwords.Hash(user, pc.NewPassword);
			}

			await _ctx.SaveChangesAsync();

			// Return updated profile in the same shape as GET
			return await GetMyProfile();
		}

		// POST: api/users/me/photo
		[Authorize]
		[HttpPost("me/photo")]
		[Consumes("multipart/form-data")]
		[RequestSizeLimit(20_000_000)]
		public async Task<ActionResult<object>> UploadProfilePhoto([FromForm] UploadProfilePhotoRequestDto request)
		{
			var file = request.File;

			if (file == null || file.Length == 0)
				return BadRequest("No file uploaded.");

			// Validate content type (server-side)
			var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"image/jpeg",
				"image/png",
				"image/webp"
			};

			if (!allowed.Contains(file.ContentType))
				return BadRequest("Only JPG, PNG, or WebP images are allowed.");

			// Find current user id from JWT
			var uidStr = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(uidStr, out var userId))
				return Unauthorized();

			var user = await _ctx.Users.SingleOrDefaultAsync(u => u.UserId == userId);
			if (user == null) return Unauthorized();

			// Determine upload folder under wwwroot
			var relativeFolder = _config["Uploads:ProfilePhotosPath"] ?? "uploads/profile-photos";
			var webRoot = _env.WebRootPath;

			if (string.IsNullOrWhiteSpace(webRoot))
			{
				webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
			}

			Directory.CreateDirectory(webRoot);

			var folderPath = Path.Combine(webRoot, relativeFolder);
			Directory.CreateDirectory(folderPath);

			// Create unique filename
			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (ext is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
				return BadRequest("Invalid file extension.");

			var fileName = $"u{userId}_{Guid.NewGuid():N}{ext}";
			var fullPath = Path.Combine(folderPath, fileName);

			// Save file
			await using (var stream = System.IO.File.Create(fullPath))
			{
				await file.CopyToAsync(stream);
			}

			// Update user profile url
			var urlPath = $"/{relativeFolder.Replace("\\", "/")}/{fileName}";
			user.ProfilePicUrl = urlPath;

			await _ctx.SaveChangesAsync();

			return Ok(new { profilePicUrl = urlPath });
		}

		private int? GetUserId()
		{
			var uidStr = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(uidStr, out var id) ? id : null;
		}

		// -------------------------------------------------------------------------
		// Freelancer DTO helpers
		// -------------------------------------------------------------------------

		private static string? BuildLocation(User u)
		{
			var parts = new[] { u.LocationCity, u.LocationCountry }
				.Where(p => !string.IsNullOrWhiteSpace(p));
			var loc = string.Join(", ", parts);
			return loc.Length > 0 ? loc : null;
		}

		private static (int completed, int total, int successRate) ComputeJobStats(User u)
		{
			var freelancerJobs = u.JobsAsFreelancer;
			var completed = freelancerJobs.Count(j => j.Status == JobStatus.Completed);
			var total = freelancerJobs.Count(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Cancelled);
			var rate = total > 0 ? (int)Math.Round(100.0 * completed / total) : 0;
			return (completed, freelancerJobs.Count, rate);
		}

		private static decimal ComputeTotalEarned(User u)
		{
			return u.JobsAsFreelancer
				.Where(j => j.Status == JobStatus.Completed)
				.Sum(j => j.FlatAmount ?? j.HourlyRate ?? 0m);
		}

		private static FreelancerSummaryDto ToFreelancerSummary(User u)
		{
			var (_, _, successRate) = ComputeJobStats(u);
			return new FreelancerSummaryDto
			{
				UserId         = u.UserId,
				Username       = u.Username,
				FirstName      = u.FirstName,
				LastName        = u.LastName,
				Title          = null,       // not yet stored on User
				Location       = BuildLocation(u),
				HourlyRate     = null,       // not yet stored on User
				ProfilePicUrl  = u.ProfilePicUrl,
				Skills         = u.UserSkills.Select(us => us.Skill.Name).OrderBy(n => n).ToList(),
				Bio            = u.Bio,
				JobSuccessRate = successRate,
				TotalEarned    = ComputeTotalEarned(u),
				IsVerified     = u.IsVerified,
				IsTopRated     = false,
				IsAvailable    = true
			};
		}

		private static FreelancerProfileDto ToFreelancerProfile(User u)
		{
			var (completed, _, successRate) = ComputeJobStats(u);
			return new FreelancerProfileDto
			{
				UserId              = u.UserId,
				Username            = u.Username,
				FirstName           = u.FirstName,
				LastName             = u.LastName,
				Title               = null,       // not yet stored on User
				Location            = BuildLocation(u),
				HourlyRate          = null,       // not yet stored on User
				ProfilePicUrl       = u.ProfilePicUrl,
				Bio                 = u.Bio,
				Skills              = u.UserSkills.Select(us => us.Skill.Name).OrderBy(n => n).ToList(),
				IsVerified          = u.IsVerified,
				IsTopRated          = false,
				ResponseTime        = null,
				MemberSince         = u.CreatedAt,
				TotalJobsCompleted  = completed,
				TotalHoursWorked    = 0,
				JobSuccessRate      = successRate
			};
		}
	}
}
