using DevSpotAPI.Data;
using DevSpotAPI.Models;
using DevSpotAPI.Models.DTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static DevSpotAPI.Models.Request;
using static DevSpotAPI.Models.Job;

namespace DevSpotAPI.Controllers
{
	[ApiController]
	[Route("api/requests")]
	public class RequestsController : ControllerBase
	{
		private readonly Context _ctx;

		public RequestsController(Context ctx)
		{
			_ctx = ctx;
		}

		// POST: api/requests
		// Freelancer applies to a job
		[Authorize]
		[HttpPost]
		public async Task<ActionResult<RequestSummaryResponseDto>> CreateRequest([FromBody] CreateRequestDto dto)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var job = await _ctx.Jobs.SingleOrDefaultAsync(j => j.JobId == dto.JobId);
			if (job == null) return NotFound("Job not found.");
			if (job.Status != JobStatus.Open) return BadRequest("Job is no longer open.");
			if (job.ClientId == userId) return BadRequest("You cannot apply to your own job.");

			var alreadyApplied = await _ctx.Requests
				.AnyAsync(r => r.JobId == dto.JobId && r.RequestedById == userId);
			if (alreadyApplied) return BadRequest("You have already applied to this job.");

			var request = new Request
			{
				JobId = dto.JobId,
				RequestedById = userId.Value,
				Status = RequestStatus.Pending,
				CreatedAt = DateTime.UtcNow
			};

			_ctx.Requests.Add(request);
			await _ctx.SaveChangesAsync();

			var created = await LoadRequest(request.RequestId);
			return CreatedAtAction(nameof(GetRequest), new { id = request.RequestId }, ToDto(created!));
		}

		// GET: api/requests/{id}
		[Authorize]
		[HttpGet("{id}")]
		public async Task<ActionResult<RequestSummaryResponseDto>> GetRequest(int id)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var request = await LoadRequest(id);
			if (request == null) return NotFound();

			// Only the client of the job or the applicant can view
			if (request.Job.ClientId != userId && request.RequestedById != userId)
				return Forbid();

			return ToDto(request);
		}

		// GET: api/requests/my
		// Freelancer views their own submitted requests
		[Authorize]
		[HttpGet("my")]
		public async Task<ActionResult<List<RequestSummaryResponseDto>>> GetMyRequests()
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var requests = await _ctx.Requests
				.Where(r => r.RequestedById == userId)
				.Include(r => r.Job)
				.Include(r => r.RequestedBy).ThenInclude(u => u.UserSkills).ThenInclude(us => us.Skill)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			return requests.Select(ToDto).ToList();
		}

		// GET: api/requests/job/{jobId}
		// Client views all requests for their job
		[Authorize]
		[HttpGet("job/{jobId}")]
		public async Task<ActionResult<List<RequestSummaryResponseDto>>> GetJobRequests(int jobId)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var job = await _ctx.Jobs.SingleOrDefaultAsync(j => j.JobId == jobId);
			if (job == null) return NotFound("Job not found.");
			if (job.ClientId != userId) return Forbid();

			var requests = await _ctx.Requests
				.Where(r => r.JobId == jobId)
				.Include(r => r.Job)
				.Include(r => r.RequestedBy).ThenInclude(u => u.UserSkills).ThenInclude(us => us.Skill)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			return requests.Select(ToDto).ToList();
		}

		// PUT: api/requests/{id}/status
		// Client accepts or rejects; freelancer cancels
		[Authorize]
		[HttpPut("{id}/status")]
		public async Task<ActionResult<RequestSummaryResponseDto>> UpdateStatus(int id, [FromBody] UpdateRequestStatusDto dto)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var request = await LoadRequest(id);
			if (request == null) return NotFound();

			if (!Enum.TryParse<RequestStatus>(dto.Status, ignoreCase: true, out var newStatus))
				return BadRequest("Invalid status value.");

			var isClient = request.Job.ClientId == userId;
			var isApplicant = request.RequestedById == userId;

			// Validate who can set what
			if (newStatus == RequestStatus.Accepted || newStatus == RequestStatus.Rejected)
			{
				if (!isClient) return Forbid();
			}
			else if (newStatus == RequestStatus.Cancelled)
			{
				if (!isApplicant) return Forbid();
			}
			else
			{
				return BadRequest("Invalid status value.");
			}

			if (request.Status != RequestStatus.Pending)
				return BadRequest("Only pending requests can be updated.");

			request.Status = newStatus;

			// Accepting: assign freelancer to job, close job, reject other pending requests
			if (newStatus == RequestStatus.Accepted)
			{
				request.AcceptedAt = DateTime.UtcNow;

				request.Job.FreelancerId = request.RequestedById;
				request.Job.Status = JobStatus.InProgress;
				request.Job.UpdatedAt = DateTime.UtcNow;

				var otherPending = await _ctx.Requests
					.Where(r => r.JobId == request.JobId && r.RequestId != id && r.Status == RequestStatus.Pending)
					.ToListAsync();

				foreach (var other in otherPending)
					other.Status = RequestStatus.Rejected;
			}

			await _ctx.SaveChangesAsync();

			return ToDto(request);
		}

		// DELETE: api/requests/{id}
		// Freelancer withdraws a pending request
		[Authorize]
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteRequest(int id)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var request = await _ctx.Requests.SingleOrDefaultAsync(r => r.RequestId == id);
			if (request == null) return NotFound();
			if (request.RequestedById != userId) return Forbid();
			if (request.Status != RequestStatus.Pending)
				return BadRequest("Only pending requests can be withdrawn.");

			_ctx.Requests.Remove(request);
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

		private Task<Request?> LoadRequest(int id) =>
			_ctx.Requests
				.Include(r => r.Job)
				.Include(r => r.RequestedBy).ThenInclude(u => u.UserSkills).ThenInclude(us => us.Skill)
				.SingleOrDefaultAsync(r => r.RequestId == id);

		private static RequestSummaryResponseDto ToDto(Request r) => new()
		{
			RequestId             = r.RequestId,
			JobId                 = r.JobId,
			Status                = r.Status.ToString(),
			CreatedAt             = r.CreatedAt,
			JobTitle              = r.Job.Title,
			JobDuration           = r.Job.Duration.HasValue ? DurationLabel(r.Job.Duration) : null,
			JobPostedAt           = r.Job.CreatedAt,
			PayType               = r.Job.PayType.ToString(),
			HourlyRate            = r.Job.HourlyRate,
			FlatAmount            = r.Job.FlatAmount,
			FreelancerId          = r.RequestedById,
			FreelancerUsername    = r.RequestedBy.Username,
			FreelancerFirstName   = r.RequestedBy.FirstName,
			FreelancerLastName    = r.RequestedBy.LastName,
			FreelancerProfilePicUrl   = r.RequestedBy.ProfilePicUrl,
			FreelancerLocationCity    = r.RequestedBy.LocationCity,
			FreelancerLocationCountry = r.RequestedBy.LocationCountry,
			FreelancerSkills      = r.RequestedBy.UserSkills.Select(us => us.Skill.Name).ToList()
		};

		private static string DurationLabel(JobDuration? duration) => duration switch
		{
			JobDuration.LessThanOneMonth  => "Less than 1 month",
			JobDuration.OneToThreeMonths  => "1-3 months",
			JobDuration.ThreeToSixMonths  => "3-6 months",
			JobDuration.MoreThanSixMonths => "More than 6 months",
			_                             => "Unknown"
		};
	}
}
