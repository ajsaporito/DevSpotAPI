using DevSpotAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public static class TestDbFactory
{
	public static Context CreateContext()
	{ 
		var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.Test.json")
			.Build();

		var cs = config.GetConnectionString("DefaultConnection");

		var options = new DbContextOptionsBuilder<Context>()
			.UseNpgsql(cs)
			.Options;

		var ctx = new Context(options);

		ctx.Database.Migrate();

		return ctx;
	}
}
