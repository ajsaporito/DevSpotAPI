using DevSpotAPI.Data;
using DevSpotAPI.Models.DTOs.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DevSpotAPI.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly Context _ctx;

        public NotificationsController(Context ctx)
        {
            _ctx = ctx;
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(value!);
        }

        // GET /api/notifications — current user's notifications, newest first
        [HttpGet]
        public async Task<ActionResult<List<NotificationResponseDto>>> GetNotifications()
        {
            var userId = GetCurrentUserId();

            var notifications = await _ctx.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationResponseDto
                {
                    NotificationId = n.NotificationId,
                    Type = n.Type.ToString(),
                    Title = n.Title,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt,
                    Rating = n.Rating,
                    ChatId = n.ChatId,
                    JobId = n.JobId,
                    RequestId = n.RequestId,
                    ReviewId = n.ReviewId
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // POST /api/notifications/mark-read?chatId={chatId}
        // Marks all unread Message notifications for the given chatId as read
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromQuery] int chatId)
        {
            var userId = GetCurrentUserId();

            var unread = await _ctx.Notifications
                .Where(n => n.UserId == userId
                         && n.ChatId == chatId
                         && n.Type == Models.NotificationType.Message
                         && !n.IsRead)
                .ToListAsync();

            if (unread.Count == 0)
                return NoContent();

            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/notifications/{id}/mark-read
        // Marks a single notification as read
        [HttpPost("{id:int}/mark-read")]
        public async Task<IActionResult> MarkOneRead(int id)
        {
            var userId = GetCurrentUserId();

            var notification = await _ctx.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (notification == null)
                return NotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _ctx.SaveChangesAsync();
            }

            return NoContent();
        }

        // DELETE /api/notifications
        // Clears all notifications for the current user
        [HttpDelete]
        public async Task<IActionResult> ClearAll()
        {
            var userId = GetCurrentUserId();

            await _ctx.Notifications
                .Where(n => n.UserId == userId)
                .ExecuteDeleteAsync();

            return NoContent();
        }
    }
}
