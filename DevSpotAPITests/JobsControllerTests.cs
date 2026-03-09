using DevSpotAPI.Controllers;
using DevSpotAPI.Models;
using DevSpotAPI.Models.DTOs.Job;
using Microsoft.AspNetCore.Mvc;

namespace DevSpotAPITests
{
    public class JobsControllerTests
    {
        // -------------------------------------------------------------------------
        // GET /api/jobs
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetJobs_ReturnsOnlyOpenJobs()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client = TestSeeder.CreateUser(ctx);

            TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Open);
            TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);
            TestSeeder.CreateJob(ctx, client.UserId, JobStatus.InProgress);

            var controller = new JobsController(ctx);
            var result = await controller.GetJobs();

            Assert.NotNull(result.Value);
            Assert.All(result.Value!, j => Assert.Equal("Open", j.Status));
        }

        // -------------------------------------------------------------------------
        // GET /api/jobs/{id}
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetJob_ReturnsNotFound_WhenJobDoesNotExist()
        {
            using var ctx = TestDbFactory.CreateContext();
            var controller = new JobsController(ctx);

            var result = await controller.GetJob(int.MaxValue);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetJob_ReturnsJobDetail()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client = TestSeeder.CreateUser(ctx);
            var job    = TestSeeder.CreateJob(ctx, client.UserId);

            var controller = new JobsController(ctx);
            var result     = await controller.GetJob(job.JobId);
            var dto        = TestSeeder.Val(result);

            Assert.Equal(job.JobId, dto.JobId);
            Assert.Equal(job.Title, dto.Title);
            Assert.Equal(client.Username, dto.ClientUsername);
        }

        // -------------------------------------------------------------------------
        // GET /api/jobs/my
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetMyJobs_ReturnsOnlyCallerJobs()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client1    = TestSeeder.CreateUser(ctx);
            var client2    = TestSeeder.CreateUser(ctx);

            TestSeeder.CreateJob(ctx, client1.UserId);
            TestSeeder.CreateJob(ctx, client1.UserId);
            TestSeeder.CreateJob(ctx, client2.UserId);

            var controller = TestSeeder.WithUser(new JobsController(ctx), client1.UserId);
            var result     = await controller.GetMyJobs();

            Assert.NotNull(result.Value);
            Assert.All(result.Value!, j => Assert.Equal(client1.UserId, j.ClientId));
        }

        // -------------------------------------------------------------------------
        // POST /api/jobs
        // -------------------------------------------------------------------------

        [Fact]
        public async Task CreateJob_CreatesJobWithSkills()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);

            var dto = new CreateJobDto
            {
                Title           = "Build a Website",
                Description     = "Need a website built",
                Skills          = ["React", "CSS"],
                PayType         = PayType.Flat,
                Budget          = 1000,
                Duration        = JobDuration.OneToThreeMonths,
                ExperienceLevel = ExperienceLevel.Intermediate
            };

            var controller = TestSeeder.WithUser(new JobsController(ctx), client.UserId);
            var result     = await controller.CreateJob(dto);
            var created    = Assert.IsType<CreatedAtActionResult>(result.Result);
            var job        = Assert.IsType<JobDetailResponseDto>(created.Value);

            Assert.Equal(dto.Title, job.Title);
            Assert.Equal(client.UserId, job.ClientId);
            Assert.Contains("React", job.Skills);
            Assert.Contains("CSS", job.Skills);
            Assert.Equal(1000, job.FlatAmount);
            Assert.Null(job.HourlyRate);
        }

        [Fact]
        public async Task CreateJob_MapsHourlyBudget_ToHourlyRate()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);

            var dto = new CreateJobDto
            {
                Title           = "Hourly Job",
                Description     = "Hourly work",
                Skills          = [],
                PayType         = PayType.Hourly,
                Budget          = 75,
                Duration        = JobDuration.LessThanOneMonth,
                ExperienceLevel = ExperienceLevel.EntryLevel
            };

            var controller = TestSeeder.WithUser(new JobsController(ctx), client.UserId);
            var result     = await controller.CreateJob(dto);
            var created    = Assert.IsType<CreatedAtActionResult>(result.Result);
            var job        = Assert.IsType<JobDetailResponseDto>(created.Value);

            Assert.Equal(75, job.HourlyRate);
            Assert.Null(job.FlatAmount);
        }

        // -------------------------------------------------------------------------
        // PUT /api/jobs/{id}
        // -------------------------------------------------------------------------

        [Fact]
        public async Task UpdateJob_UpdatesFields()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);
            var job       = TestSeeder.CreateJob(ctx, client.UserId);

            var dto = new CreateJobDto
            {
                Title           = "Updated Title",
                Description     = "Updated description",
                Skills          = ["Vue"],
                PayType         = PayType.Flat,
                Budget          = 2000,
                Duration        = JobDuration.ThreeToSixMonths,
                ExperienceLevel = ExperienceLevel.Expert
            };

            var controller = TestSeeder.WithUser(new JobsController(ctx), client.UserId);
            var result     = await controller.UpdateJob(job.JobId, dto);
            var updated    = TestSeeder.Val(result);

            Assert.Equal("Updated Title", updated.Title);
            Assert.Equal(2000, updated.FlatAmount);
            Assert.Contains("Vue", updated.Skills);
        }

        [Fact]
        public async Task UpdateJob_ReturnsForbid_WhenNotOwner()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);
            var other     = TestSeeder.CreateUser(ctx);
            var job       = TestSeeder.CreateJob(ctx, client.UserId);

            var dto = new CreateJobDto
            {
                Title           = "Hijacked",
                Description     = "x",
                Skills          = [],
                PayType         = PayType.Flat,
                Budget          = 100,
                Duration        = JobDuration.LessThanOneMonth,
                ExperienceLevel = ExperienceLevel.EntryLevel
            };

            var controller = TestSeeder.WithUser(new JobsController(ctx), other.UserId);
            var result     = await controller.UpdateJob(job.JobId, dto);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // -------------------------------------------------------------------------
        // DELETE /api/jobs/{id}
        // -------------------------------------------------------------------------

        [Fact]
        public async Task DeleteJob_DeletesJob()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);
            var job       = TestSeeder.CreateJob(ctx, client.UserId);

            var controller = TestSeeder.WithUser(new JobsController(ctx), client.UserId);
            var result     = await controller.DeleteJob(job.JobId);

            Assert.IsType<NoContentResult>(result);
            Assert.False(ctx.Jobs.Any(j => j.JobId == job.JobId));
        }

        [Fact]
        public async Task DeleteJob_ReturnsForbid_WhenNotOwner()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);
            var other     = TestSeeder.CreateUser(ctx);
            var job       = TestSeeder.CreateJob(ctx, client.UserId);

            var controller = TestSeeder.WithUser(new JobsController(ctx), other.UserId);
            var result     = await controller.DeleteJob(job.JobId);

            Assert.IsType<ForbidResult>(result);
            Assert.True(ctx.Jobs.Any(j => j.JobId == job.JobId));
        }
    }
}
