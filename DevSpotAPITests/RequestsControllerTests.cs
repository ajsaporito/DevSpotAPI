using DevSpotAPI.Controllers;
using DevSpotAPI.Models;
using DevSpotAPI.Models.DTOs.Request;
using Microsoft.AspNetCore.Mvc;
using static DevSpotAPI.Models.Request;

namespace DevSpotAPITests
{
    public class RequestsControllerTests
    {
        // -------------------------------------------------------------------------
        // POST /api/requests
        // -------------------------------------------------------------------------

        [Fact]
        public async Task CreateRequest_CreatesRequest()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), freelancer.UserId);
            var result     = await controller.CreateRequest(new CreateRequestDto { JobId = job.JobId });
            var created    = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto        = Assert.IsType<RequestSummaryResponseDto>(created.Value);

            Assert.Equal(job.JobId, dto.JobId);
            Assert.Equal(freelancer.UserId, dto.FreelancerId);
            Assert.Equal("Pending", dto.Status);
        }

        [Fact]
        public async Task CreateRequest_ReturnsBadRequest_WhenJobNotOpen()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId, JobStatus.Completed);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), freelancer.UserId);
            var result     = await controller.CreateRequest(new CreateRequestDto { JobId = job.JobId });

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("no longer open", bad.Value!.ToString());
        }

        [Fact]
        public async Task CreateRequest_ReturnsBadRequest_WhenApplyingToOwnJob()
        {
            using var ctx = TestDbFactory.CreateContext();
            var client    = TestSeeder.CreateUser(ctx);
            var job       = TestSeeder.CreateJob(ctx, client.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), client.UserId);
            var result     = await controller.CreateRequest(new CreateRequestDto { JobId = job.JobId });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateRequest_ReturnsBadRequest_WhenAlreadyApplied()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);

            TestSeeder.CreateRequest(ctx, job.JobId, freelancer.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), freelancer.UserId);
            var result     = await controller.CreateRequest(new CreateRequestDto { JobId = job.JobId });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // -------------------------------------------------------------------------
        // GET /api/requests/job/{jobId}
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetJobRequests_ReturnsRequests_ForJobOwner()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);

            TestSeeder.CreateRequest(ctx, job.JobId, freelancer.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), client.UserId);
            var result     = await controller.GetJobRequests(job.JobId);

            Assert.NotNull(result.Value);
            Assert.Single(result.Value!);
            Assert.Equal(freelancer.UserId, result.Value![0].FreelancerId);
        }

        [Fact]
        public async Task GetJobRequests_ReturnsForbid_ForNonOwner()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var other      = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), other.UserId);
            var result     = await controller.GetJobRequests(job.JobId);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // -------------------------------------------------------------------------
        // GET /api/requests/my
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetMyRequests_ReturnsFreelancerRequests()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var other      = TestSeeder.CreateUser(ctx);
            var job1       = TestSeeder.CreateJob(ctx, client.UserId);
            var job2       = TestSeeder.CreateJob(ctx, client.UserId);

            TestSeeder.CreateRequest(ctx, job1.JobId, freelancer.UserId);
            TestSeeder.CreateRequest(ctx, job2.JobId, freelancer.UserId);
            TestSeeder.CreateRequest(ctx, job1.JobId, other.UserId); // should not appear

            var controller = TestSeeder.WithUser(new RequestsController(ctx), freelancer.UserId);
            var result     = await controller.GetMyRequests();

            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value!.Count);
            Assert.All(result.Value!, r => Assert.Equal(freelancer.UserId, r.FreelancerId));
        }

        // -------------------------------------------------------------------------
        // PUT /api/requests/{id}/status
        // -------------------------------------------------------------------------

        [Fact]
        public async Task UpdateStatus_Accept_AssignsFreelancerAndSetsJobInProgress()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);
            var request    = TestSeeder.CreateRequest(ctx, job.JobId, freelancer.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), client.UserId);
            var result     = await controller.UpdateStatus(request.RequestId, new UpdateRequestStatusDto { Status = "Accepted" });
            var dto        = TestSeeder.Val(result);

            Assert.Equal("Accepted", dto.Status);

            var updatedJob = ctx.Jobs.Single(j => j.JobId == job.JobId);
            Assert.Equal(freelancer.UserId, updatedJob.FreelancerId);
            Assert.Equal(JobStatus.InProgress, updatedJob.Status);
        }

        [Fact]
        public async Task UpdateStatus_Accept_RejectsOtherPendingRequests()
        {
            using var ctx   = TestDbFactory.CreateContext();
            var client      = TestSeeder.CreateUser(ctx);
            var freelancer1 = TestSeeder.CreateUser(ctx);
            var freelancer2 = TestSeeder.CreateUser(ctx);
            var job         = TestSeeder.CreateJob(ctx, client.UserId);
            var req1        = TestSeeder.CreateRequest(ctx, job.JobId, freelancer1.UserId);
            var req2        = TestSeeder.CreateRequest(ctx, job.JobId, freelancer2.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), client.UserId);
            await controller.UpdateStatus(req1.RequestId, new UpdateRequestStatusDto { Status = "Accepted" });

            var rejectedReq = ctx.Requests.Single(r => r.RequestId == req2.RequestId);
            Assert.Equal(RequestStatus.Rejected, rejectedReq.Status);
        }

        [Fact]
        public async Task UpdateStatus_Reject_RejectsRequest()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);
            var request    = TestSeeder.CreateRequest(ctx, job.JobId, freelancer.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), client.UserId);
            var result     = await controller.UpdateStatus(request.RequestId, new UpdateRequestStatusDto { Status = "Rejected" });
            var dto        = TestSeeder.Val(result);

            Assert.Equal("Rejected", dto.Status);

            var updatedJob = ctx.Jobs.Single(j => j.JobId == job.JobId);
            Assert.Equal(JobStatus.Open, updatedJob.Status);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsForbid_WhenFreelancerTriesToAccept()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);
            var request    = TestSeeder.CreateRequest(ctx, job.JobId, freelancer.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), freelancer.UserId);
            var result     = await controller.UpdateStatus(request.RequestId, new UpdateRequestStatusDto { Status = "Accepted" });

            Assert.IsType<ForbidResult>(result.Result);
        }

        // -------------------------------------------------------------------------
        // DELETE /api/requests/{id}
        // -------------------------------------------------------------------------

        [Fact]
        public async Task DeleteRequest_WithdrawsPendingRequest()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);
            var request    = TestSeeder.CreateRequest(ctx, job.JobId, freelancer.UserId);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), freelancer.UserId);
            var result     = await controller.DeleteRequest(request.RequestId);

            Assert.IsType<NoContentResult>(result);
            Assert.False(ctx.Requests.Any(r => r.RequestId == request.RequestId));
        }

        [Fact]
        public async Task DeleteRequest_ReturnsBadRequest_WhenNotPending()
        {
            using var ctx  = TestDbFactory.CreateContext();
            var client     = TestSeeder.CreateUser(ctx);
            var freelancer = TestSeeder.CreateUser(ctx);
            var job        = TestSeeder.CreateJob(ctx, client.UserId);
            var request    = TestSeeder.CreateRequest(ctx, job.JobId, freelancer.UserId, RequestStatus.Accepted);

            var controller = TestSeeder.WithUser(new RequestsController(ctx), freelancer.UserId);
            var result     = await controller.DeleteRequest(request.RequestId);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
