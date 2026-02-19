using DevSpotAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace DevSpotAPI.Services
{
	public sealed class PasswordService
	{
		private readonly PasswordHasher<User> _hasher = new();

		public string Hash(User user, string password) =>
			_hasher.HashPassword(user, password);

		public bool Verify(User user, string password)
		{
			try
			{
				var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
				return result == PasswordVerificationResult.Success ||
					   result == PasswordVerificationResult.SuccessRehashNeeded;
			} catch
			{
				return false;
			}
		}
	}
}
