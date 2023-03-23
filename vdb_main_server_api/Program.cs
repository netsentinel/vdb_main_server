using DataAccessLayer.Contexts;
using Microsoft.EntityFrameworkCore;
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
			.AddEnvironmentVariables()
			.Build();

		builder.Logging.AddConsole();
		builder.Services.AddControllers();

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
			opts.UseNpgsql(builder.Configuration["ConnectionStrings:DefaultConnection"], opts => {
				opts.MigrationsAssembly(nameof(main_server_api));
			});
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

		app.UseRouting();
		app.MapControllers();


		app.Services.CreateScope().ServiceProvider.GetRequiredService<VpnContext>().Database.Migrate();
		app.Run();
	}
}