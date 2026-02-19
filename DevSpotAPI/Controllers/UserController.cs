using DevSpotAPI.Data;
using DevSpotAPI.Models.DTOs.User;
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

		public UsersController(Context ctx, IWebHostEnvironment env, IConfiguration config)
		{
			_ctx = ctx;
			_env = env;
			_config = config;
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
	}
}
