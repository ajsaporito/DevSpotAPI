using DevSpotAPI.Data;
using DevSpotAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT config
var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"];

if (string.IsNullOrWhiteSpace(key))
	throw new InvalidOperationException("Jwt:Key is missing from configuration.");

builder.Services.AddControllers()
	.AddJsonOptions(o =>
		o.JsonSerializerOptions.ReferenceHandler =
			System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "DevSpot API",
		Version = "v1"
	});

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",        
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "a-string-secret-at-least-256-bits-long"
	});

	options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
	{
		[new OpenApiSecuritySchemeReference("Bearer", document)] = []
	});
});

builder.Services.AddDbContext<Context>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,

			ValidIssuer = jwt["Issuer"],
			ValidAudience = jwt["Audience"],

			IssuerSigningKeys = new[]
			{
				new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
			},

			ValidateIssuerSigningKey = true,

			ClockSkew = TimeSpan.FromSeconds(30),
			RequireSignedTokens = true,
		};

		options.ConfigurationManager = null;

		options.IncludeErrorDetails = true;
		options.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = ctx =>
			{
				Console.WriteLine("JWT Authentication FAILED");
				Console.WriteLine("Exception: " + ctx.Exception?.GetType().Name);
				Console.WriteLine("Message:   " + ctx.Exception?.Message);
				if (ctx.Exception?.InnerException != null)
				{
					Console.WriteLine("Inner:     " + ctx.Exception.InnerException.Message);
				}
				// Also very useful:
				Console.WriteLine("Raw token: " + ctx.Request.Headers["Authorization"]);
				return Task.CompletedTask;
			},

			OnTokenValidated = ctx =>
			{
				Console.WriteLine("JWT Token VALIDATED successfully");
				return Task.CompletedTask;
			},

			OnChallenge = ctx =>
			{
				Console.WriteLine("JWT Challenge triggered - sending 401");
				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();

builder.Services.AddSingleton<PasswordService>();
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddCors(options =>
{
	options.AddPolicy("DevCors", p =>
		p.WithOrigins("http://localhost:5173")
		 .AllowAnyHeader()
		 .AllowAnyMethod()
		 .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevSpot API v1"));
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseCors("DevCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
