using DevSpotAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DevSpotAPI.Services
{
	public sealed class JwtTokenService
	{
		private readonly IConfiguration _config;

		public JwtTokenService(IConfiguration config) => _config = config;

		public (string token, DateTime expiresAtUtc) CreateToken(User user)
		{
			var jwt = _config.GetSection("Jwt");
			var issuer = jwt["Issuer"]!;
			var audience = jwt["Audience"]!;
			var key = jwt["Key"]!;
			var expireMinutes = int.Parse(jwt["ExpireMinutes"]!);

			var now = DateTime.UtcNow;
			var expires = now.AddMinutes(expireMinutes);

			var claims = new List<Claim>
			{
			new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
			new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
			new Claim(JwtRegisteredClaimNames.Email, user.Email),
			new Claim("uid", user.UserId.ToString()),

			new Claim(JwtRegisteredClaimNames.Iat,
				new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
				ClaimValueTypes.Integer64),

			new Claim(JwtRegisteredClaimNames.Exp,
				new DateTimeOffset(expires).ToUnixTimeSeconds().ToString(),
				ClaimValueTypes.Integer64),
			};

			if (user.IsAdmin)
				claims.Add(new Claim(ClaimTypes.Role, "Admin"));

			var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
			var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: issuer,
				audience: audience,
				claims: claims,
				expires: expires,
				signingCredentials: creds
			);

			return (new JwtSecurityTokenHandler().WriteToken(token), expires);
		}
	}
}
