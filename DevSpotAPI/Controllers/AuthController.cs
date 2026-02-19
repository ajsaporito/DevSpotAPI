using DevSpotAPI.Data;
using DevSpotAPI.Models.DTOs.Auth;
using DevSpotAPI.Models;
using DevSpotAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevSpotAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly Context _ctx;
		private readonly PasswordService _pw;
		private readonly JwtTokenService _jwt;

		public AuthController(Context ctx, PasswordService pw, JwtTokenService jwt)
		{
			_ctx = ctx;
			_pw = pw;
			_jwt = jwt;
		}

		[HttpPost("register")]
		public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto)
		{
			var email = dto.Email.Trim().ToLowerInvariant();
			var username = dto.Username.Trim();

			if (await _ctx.Users.AnyAsync(u => u.Email == email))
				return BadRequest("Email already in use.");

			if (await _ctx.Users.AnyAsync(u => u.Username == username))
				return BadRequest("Username already in use.");

			var skillNames = (dto.Skills ?? new List<string>())
				.Select(s => (s ?? "").Trim())
				.Where(s => s.Length > 0)
				.Distinct()
				.Take(10)
				.ToList();

			await using var tx = await _ctx.Database.BeginTransactionAsync();

			var user = new User
			{
				FirstName = dto.FirstName.Trim(),
				LastName = dto.LastName.Trim(),
				Email = email,
				Username = username,
				Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim(),
				LocationCountry = string.IsNullOrWhiteSpace(dto.LocationCountry) ? null : dto.LocationCountry.Trim(),
				LocationCity = string.IsNullOrWhiteSpace(dto.LocationCity) ? null : dto.LocationCity.Trim(),
				ProfilePicUrl = string.IsNullOrWhiteSpace(dto.ProfilePicUrl) ? null : dto.ProfilePicUrl.Trim(),
				CreatedAt = DateTime.UtcNow,
				IsAdmin = false,
				IsVerified = false,
				PasswordHash = ""
			};

			user.PasswordHash = _pw.Hash(user, dto.Password);

			_ctx.Users.Add(user);
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

				var joins = existingSkills.Select(s => new UserSkill
				{
					UserId = user.UserId,
					SkillId = s.SkillId
				});

				_ctx.UserSkills.AddRange(joins);
				await _ctx.SaveChangesAsync();
			}

			await tx.CommitAsync();


			var (token, exp) = _jwt.CreateToken(user);

			return new AuthResponseDto
			{
				AccessToken = token,
				ExpiresAtUtc = exp,
				UserId = user.UserId,
				Username = user.Username
			};
		}

		[HttpPost("login")]
		public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
		{
			var input = dto.EmailOrUsername.Trim();

			var user = await _ctx.Users.SingleOrDefaultAsync(u =>
				u.Email == input.ToLower() || u.Username == input);

			if (user == null) return Unauthorized("Invalid credentials.");

			if (!_pw.Verify(user, dto.Password))
				return Unauthorized("Invalid credentials.");

			var (token, exp) = _jwt.CreateToken(user);

			return new AuthResponseDto
			{
				AccessToken = token,
				ExpiresAtUtc = exp,
				UserId = user.UserId,
				Username = user.Username,
				ProfilePicUrl = user.ProfilePicUrl
			};
		}
	}
}
