using DataAccessLayer.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using vdb_main_server_api.Services;

#if DEBUG
#endif

namespace vdb_main_server_api;

internal class Program
{
	private static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Configuration
			.AddJsonFile("./appsettings.json", true)
			.AddJsonFile("/run/secrets/aspsecrets.json", true)
			.AddJsonFile("/run/secrets/generated_sig.json", true)
			.AddEnvironmentVariables()
			.Build();

		builder.Logging.AddConsole();
		builder.Services.AddControllers();
		builder.Services.AddAuthentication(opts => {
			opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
		}).AddJwtBearer(opts => {
			opts.RequireHttpsMetadata = false;
			opts.SaveToken = false;

			var env = new EnvironmentProvider(null);
			opts.TokenValidationParameters = new TokenValidationParameters {
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(
					builder.Configuration["JwtServiceSettings:SigningKeyBase64"]!)), // ok to throw here
				ValidateAudience = false,
				ValidateIssuer = false,
			};
		}
		);

#if DEBUG
		if (builder.Environment.IsDevelopment())
		{
			builder.Services.AddSwaggerGen();
		}
#endif

		builder.Services.AddSingleton<EnvironmentProvider>();
		builder.Services.AddSingleton<SettingsProviderService>();
		builder.Services.AddSingleton<JwtService>();
		builder.Services.AddSingleton<VpnNodesService>();
		builder.Services.AddHostedService(pr => pr.GetRequiredService<VpnNodesService>());


		builder.Services.AddDbContext<VpnContext>(opts => {
			opts.UseNpgsql(builder.Environment.IsDevelopment()
				? builder.Configuration["ConnectionStrings:LocalhostConnection"]
				: builder.Configuration["ConnectionStrings:DatabaseConnection"]
				, opts => { opts.MigrationsAssembly(nameof(main_server_api)); });
		});


		var app = builder.Build();


		app.UseCors(opts =>
		{
			opts.AllowAnyOrigin();
			opts.AllowAnyMethod();
			opts.AllowAnyHeader();
		});

#if DEBUG
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}
#endif

		app.UseAuthentication();
		app.UseRouting();
		app.UseAuthorization();
		app.MapControllers();

		app.Services.CreateScope().ServiceProvider.GetRequiredService<ILogger<Program>>().LogInformation($"Env: {app.Environment.EnvironmentName}");
		app.Services.CreateScope().ServiceProvider.GetRequiredService<VpnContext>().Database.Migrate();
		app.Run();
	}
}