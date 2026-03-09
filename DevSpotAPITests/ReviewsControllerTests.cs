using DevSpotAPI.Controllers;
using DevSpotAPI.Models;
using DevSpotAPI.Models.DTOs.Review;
using Microsoft.AspNetCore.Mvc;

namespace DevSpotAPITests
{
    public class ReviewsControllerTests
    {
        // -------------------------------------------------------------------------
        // POST /api/reviews
        // -------------------------------------------------------------------------

        [Fact]
        public async Task CreateReview_CreatesReview_ForCompletedJob()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);

            job.FreelancerId = freelancer.UserId;
            ctx.SaveChanges();

            var controller = TestSeeder.WithUser(new ReviewsController(ctx), client.UserId);
            var result     = await controller.CreateReview(new CreateReviewDto
            {
                JobId    = job.JobId,
                Rating   = 5,
                Comments = "Excellent!"
            });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto     = Assert.IsType<ReviewResponseDto>(created.Value);

            Assert.Equal(job.JobId, dto.JobId);
            Assert.Equal(5, dto.Rating);
            Assert.Equal("Excellent!", dto.Comments);
            Assert.Equal(client.Username, dto.ClientUsername);
            Assert.Equal(freelancer.Username, dto.FreelancerUsername);

            // HasReview should be set
            Assert.True(ctx.Jobs.Single(j => j.JobId == job.JobId).HasReview);
        }

        [Fact]
        public async Task CreateReview_ReturnsBadRequest_WhenJobNotCompleted()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.InProgress);

            job.FreelancerId = freelancer.UserId;
            ctx.SaveChanges();

            var controller = TestSeeder.WithUser(new ReviewsController(ctx), client.UserId);
            var result     = await controller.CreateReview(new CreateReviewDto { JobId = job.JobId, Rating = 5 });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateReview_ReturnsBadRequest_WhenAlreadyReviewed()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);

            job.FreelancerId = freelancer.UserId;
            job.HasReview    = true;
            ctx.SaveChanges();

            var controller = TestSeeder.WithUser(new ReviewsController(ctx), client.UserId);
            var result     = await controller.CreateReview(new CreateReviewDto { JobId = job.JobId, Rating = 4 });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateReview_ReturnsBadRequest_WhenRatingOutOfRange()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);

            job.FreelancerId = freelancer.UserId;
            ctx.SaveChanges();

            var controller = TestSeeder.WithUser(new ReviewsController(ctx), client.UserId);

            var tooHigh = await controller.CreateReview(new CreateReviewDto { JobId = job.JobId, Rating = 6 });
            Assert.IsType<BadRequestObjectResult>(tooHigh.Result);

            var tooLow = await controller.CreateReview(new CreateReviewDto { JobId = job.JobId, Rating = 0 });
            Assert.IsType<BadRequestObjectResult>(tooLow.Result);
        }

        [Fact]
        public async Task CreateReview_ReturnsForbid_WhenNotClient()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var other      = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);

            job.FreelancerId = freelancer.UserId;
            ctx.SaveChanges();

            var controller = TestSeeder.WithUser(new ReviewsController(ctx), other.UserId);
            var result     = await controller.CreateReview(new CreateReviewDto { JobId = job.JobId, Rating = 3 });

            Assert.IsType<ForbidResult>(result.Result);
        }

        // -------------------------------------------------------------------------
        // GET /api/reviews/job/{jobId}
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetJobReview_ReturnsReview()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);

            TestSeeder.CreateReview(ctx, job.JobId, client.UserId, freelancer.UserId, 4);

            var controller = new ReviewsController(ctx);
            var result     = await controller.GetJobReview(job.JobId);
            var dto        = TestSeeder.Val(result);

            Assert.Equal(job.JobId, dto.JobId);
            Assert.Equal(4, dto.Rating);
        }

        [Fact]
        public async Task GetJobReview_ReturnsNotFound_WhenNoReview()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);
            var job       = TestSeeder.CreateJob(ctx, client.UserId);

            var controller = new ReviewsController(ctx);
            var result     = await controller.GetJobReview(job.JobId);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // -------------------------------------------------------------------------
        // GET /api/reviews/user/{userId}
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetUserReviews_ReturnsFreelancerReviews()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job1       = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);
            var job2       = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);

            TestSeeder.CreateReview(ctx, job1.JobId, client.UserId, freelancer.UserId, 5);
            TestSeeder.CreateReview(ctx, job2.JobId, client.UserId, freelancer.UserId, 4);

            var controller = new ReviewsController(ctx);
            var result     = await controller.GetUserReviews(freelancer.UserId);

            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value!.Count);
            Assert.All(result.Value!, r => Assert.Equal(freelancer.UserId, r.FreelancerId));
        }

        [Fact]
        public async Task GetUserReviews_ReturnsEmpty_WhenNoReviews()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var freelancer = TestSeeder.CreateUser(ctx);

            var controller = new ReviewsController(ctx);
            var result     = await controller.GetUserReviews(freelancer.UserId);

            Assert.NotNull(result.Value);
            Assert.Empty(result.Value!);
        }

        // -------------------------------------------------------------------------
        // GET /api/reviews/my
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetMyReviews_ReturnsReviewsSubmittedByClient()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var other      = TestSeeder.CreateUser(ctx);
            var job1       = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);
            var job2       = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);
            var job3       = TestSeeder.CreateJob(ctx, other.UserId,  JobStatus.Completed);

            TestSeeder.CreateReview(ctx, job1.JobId, client.UserId, freelancer.UserId, 5);
            TestSeeder.CreateReview(ctx, job2.JobId, client.UserId, freelancer.UserId, 4);
            TestSeeder.CreateReview(ctx, job3.JobId, other.UserId,  freelancer.UserId, 3); // other client — should not appear

            var controller = TestSeeder.WithUser(new ReviewsController(ctx), client.UserId);
            var result     = await controller.GetMyReviews();

            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value!.Count);
            Assert.All(result.Value!, r => Assert.Equal(client.UserId, r.ClientId));
        }

        [Fact]
        public async Task GetMyReviews_ReturnsEmpty_WhenNoReviewsSubmitted()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);

            var controller = TestSeeder.WithUser(new ReviewsController(ctx), client.UserId);
            var result     = await controller.GetMyReviews();

            Assert.NotNull(result.Value);
            Assert.Empty(result.Value!);
        }
    }
}
