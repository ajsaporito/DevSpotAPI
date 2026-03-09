using DevSpotAPI.Data;
using DevSpotAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static DevSpotAPI.Models.Request;

namespace DevSpotAPITests
{
    /// <summary>
    /// Shared seed helpers for controller tests.
    /// Each method creates and persists data to the test DB using a unique prefix to avoid collisions.
    /// </summary>
    public static class TestSeeder
    {
        public static User CreateUser(Context ctx, string? prefix = null)
        {
            prefix ??= "t_" + Guid.NewGuid().ToString("N")[..8];
            var user = new User
            {
                FirstName    = "Test",
                LastName     = "User",
                Email        = $"{prefix}@test.com",
                Username     = prefix,
                PasswordHash = "x",
                CreatedAt    = DateTime.UtcNow
            };
            ctx.Users.Add(user);
            ctx.SaveChanges();
            return user;
        }

        public static Job CreateJob(Context ctx, int clientId,
            JobStatus status = JobStatus.Open,
            PayType payType  = PayType.Flat,
            decimal? budget  = 500)
        {
            var job = new Job
            {
                ClientId        = clientId,
                Title           = "Test Job " + Guid.NewGuid().ToString("N")[..6],
                Description     = "Description",
                Status          = status,
                PayType         = payType,
                FlatAmount      = payType == PayType.Flat ? budget : null,
                HourlyRate      = payType == PayType.Hourly ? budget : null,
                Duration        = JobDuration.OneToThreeMonths,
                ExperienceLevel = ExperienceLevel.Intermediate,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow
            };
            ctx.Jobs.Add(job);
            ctx.SaveChanges();
            return job;
        }

        public static Request CreateRequest(Context ctx, int jobId, int freelancerId,
            RequestStatus status = RequestStatus.Pending)
        {
            var request = new Request
            {
                JobId          = jobId,
                RequestedById  = freelancerId,
                Status         = status,
                CreatedAt      = DateTime.UtcNow
            };
            ctx.Requests.Add(request);
            ctx.SaveChanges();
            return request;
        }

        public static Review CreateReview(Context ctx, int jobId, int clientId, int freelancerId, decimal rating = 5)
        {
            var review = new Review
            {
                JobId        = jobId,
                ClientId     = clientId,
                FreelancerId = freelancerId,
                Rating       = rating,
                Comments     = "Great work!",
                CreatedAt    = DateTime.UtcNow
            };
            ctx.Reviews.Add(review);
            ctx.SaveChanges();
            return review;
        }

        /// <summary>Sets the JWT uid claim on a controller so GetUserId() returns the given userId.</summary>
        public static T WithUser<T>(T controller, int userId) where T : ControllerBase
        {
            var claims    = new[] { new Claim("uid", userId.ToString()) };
            var identity  = new ClaimsIdentity(claims, "Test");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        /// <summary>Unwraps ActionResult&lt;T&gt; — handles both direct value and ObjectResult wrapping.</summary>
        public static T Val<T>(ActionResult<T> result) where T : class =>
            result.Value ?? (T)((ObjectResult)result.Result!).Value!;
    }
}
