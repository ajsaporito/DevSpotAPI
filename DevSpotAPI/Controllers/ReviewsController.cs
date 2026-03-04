using DevSpotAPI.Data;
using DevSpotAPI.Models;
using DevSpotAPI.Models.DTOs.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DevSpotAPI.Controllers
{
	[ApiController]
	[Route("api/reviews")]
	public class ReviewsController : ControllerBase
	{
		private readonly Context _ctx;

		public ReviewsController(Context ctx)
		{
			_ctx = ctx;
		}

		// POST: api/reviews
		// Client submits a review for a completed job
		[Authorize]
		[HttpPost]
		public async Task<ActionResult<ReviewResponseDto>> CreateReview([FromBody] CreateReviewDto dto)
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			if (dto.Rating < 1 || dto.Rating > 5)
				return BadRequest("Rating must be between 1 and 5.");

			var job = await _ctx.Jobs
				.Include(j => j.Client)
				.Include(j => j.Freelancer)
				.SingleOrDefaultAsync(j => j.JobId == dto.JobId);

			if (job == null) return NotFound("Job not found.");
			if (job.ClientId != userId) return Forbid();
			if (job.Status != JobStatus.Completed) return BadRequest("Job must be completed before leaving a review.");
			if (job.HasReview) return BadRequest("A review has already been submitted for this job.");
			if (job.FreelancerId == null) return BadRequest("No freelancer assigned to this job.");

			var review = new Review
			{
				JobId        = dto.JobId,
				ClientId     = userId.Value,
				FreelancerId = job.FreelancerId.Value,
				Rating       = dto.Rating,
				Comments     = dto.Comments,
				CreatedAt    = DateTime.UtcNow
			};

			job.HasReview = true;

			_ctx.Reviews.Add(review);
			await _ctx.SaveChangesAsync();

			return CreatedAtAction(nameof(GetJobReview), new { jobId = dto.JobId }, ToDto(review, job));
		}

		// GET: api/reviews/job/{jobId}
		// Get the review for a specific job (public)
		[HttpGet("job/{jobId}")]
		public async Task<ActionResult<ReviewResponseDto>> GetJobReview(int jobId)
		{
			var review = await _ctx.Reviews
				.Include(r => r.Job)
				.Include(r => r.Client)
				.Include(r => r.Freelancer)
				.SingleOrDefaultAsync(r => r.JobId == jobId);

			if (review == null) return NotFound();

			return ToDto(review, review.Job);
		}

		// GET: api/reviews/my
		// Get all reviews submitted by the current user as a client
		[Authorize]
		[HttpGet("my")]
		public async Task<ActionResult<List<ReviewResponseDto>>> GetMyReviews()
		{
			var userId = GetUserId();
			if (userId == null) return Unauthorized();

			var reviews = await _ctx.Reviews
				.Where(r => r.ClientId == userId)
				.Include(r => r.Job)
				.Include(r => r.Client)
				.Include(r => r.Freelancer)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			return reviews.Select(r => ToDto(r, r.Job)).ToList();
		}

		// GET: api/reviews/user/{userId}
		// Get all reviews received by a freelancer (public)
		[HttpGet("user/{userId}")]
		public async Task<ActionResult<List<ReviewResponseDto>>> GetUserReviews(int userId)
		{
			var reviews = await _ctx.Reviews
				.Where(r => r.FreelancerId == userId)
				.Include(r => r.Job)
				.Include(r => r.Client)
				.Include(r => r.Freelancer)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			return reviews.Select(r => ToDto(r, r.Job)).ToList();
		}

		// -------------------------------------------------------------------------
		// Helpers
		// -------------------------------------------------------------------------

		private int? GetUserId()
		{
			var uidStr = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(uidStr, out var id) ? id : null;
		}

		private static ReviewResponseDto ToDto(Review r, Job job) => new()
		{
			ReviewId             = r.ReviewId,
			JobId                = r.JobId,
			JobTitle             = job.Title,
			Rating               = r.Rating,
			Comments             = r.Comments,
			CreatedAt            = r.CreatedAt,
			ClientId             = r.ClientId,
			ClientUsername       = r.Client.Username,
			ClientProfilePicUrl  = r.Client.ProfilePicUrl,
			FreelancerId         = r.FreelancerId,
			FreelancerUsername   = r.Freelancer.Username,
			FreelancerProfilePicUrl = r.Freelancer.ProfilePicUrl
		};
	}
}
