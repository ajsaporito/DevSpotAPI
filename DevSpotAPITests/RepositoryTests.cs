using DevSpotAPI.Models;
using DevSpotAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevSpotAPITests
{
	public class RepositoryTests
	{
		[Fact]
		public void Create_Read_SaveChanges_Works()
		{
			using var ctx = TestDbFactory.CreateContext();
			var repo = new Repository(ctx);

			var prefix = "test_" + Guid.NewGuid().ToString("N")[..8];

			var user = new User
			{
				FirstName = "Ada",
				LastName = "Lovelace",
				Email = $"{prefix}@example.com",
				Username = prefix,
				PasswordHash = "x",
				CreatedAt = DateTime.UtcNow
			};

			repo.Create(user);
			repo.SaveChanges();

			Assert.NotEqual(0, user.UserId);

			// Read
			var loaded = repo.Read<User>(user.UserId);
			Assert.NotNull(loaded);
			Assert.Equal(prefix, loaded!.Username);
		}

		[Fact]
		public void Update_Works()
		{
			using var ctx = TestDbFactory.CreateContext();
			var repo = new Repository(ctx);

			var prefix = "test_" + Guid.NewGuid().ToString("N")[..8];

			var user = new User
			{
				FirstName = "Grace",
				LastName = "Hopper",
				Email = $"{prefix}@example.com",
				Username = prefix,
				PasswordHash = "x",
				CreatedAt = DateTime.UtcNow
			};

			repo.Create(user);
			repo.SaveChanges();

			user.Bio = "Updated bio";
			repo.Update(user);
			repo.SaveChanges();

			var reloaded = ctx.Users.AsNoTracking().Single(u => u.UserId == user.UserId);
			Assert.Equal("Updated bio", reloaded.Bio);
		}

		[Fact]
		public void Delete_Works()
		{
			using var ctx = TestDbFactory.CreateContext();
			var repo = new Repository(ctx);

			var prefix = "test_" + Guid.NewGuid().ToString("N")[..8];

			var user = new User
			{
				FirstName = "Linus",
				LastName = "Torvalds",
				Email = $"{prefix}@example.com",
				Username = prefix,
				PasswordHash = "x",
				CreatedAt = DateTime.UtcNow
			};

			repo.Create(user);
			repo.SaveChanges();

			var loaded = repo.Read<User>(user.UserId);
			Assert.NotNull(loaded);

			repo.Delete(loaded!);
			repo.SaveChanges();

			Assert.False(ctx.Users.Any(u => u.UserId == user.UserId));
		}

		[Fact]
		public void Unique_Email_Constraint_Is_Enforced()
		{
			using var ctx = TestDbFactory.CreateContext();
			var repo = new Repository(ctx);

			var prefix = "test_" + Guid.NewGuid().ToString("N")[..8];
			var email = $"{prefix}@example.com";

			var u1 = new User
			{
				FirstName = "A",
				LastName = "A",
				Email = email,
				Username = prefix + "_1",
				PasswordHash = "x",
				CreatedAt = DateTime.UtcNow
			};

			var u2 = new User
			{
				FirstName = "B",
				LastName = "B",
				Email = email, // violates unique constraint
				Username = prefix + "_2",
				PasswordHash = "x",
				CreatedAt = DateTime.UtcNow
			};

			repo.Create(u1);
			repo.SaveChanges();

			repo.Create(u2);

			Assert.Throws<DbUpdateException>(() => repo.SaveChanges());
		}
	}
}