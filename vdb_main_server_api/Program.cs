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
			.AddJsonFile("./appsettings.json", false)
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
		builder.Services.AddSingleton<VpnNodesService>();
		builder.Services.AddHostedService(pr => pr.GetRequiredService<VpnNodesService>());

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

		app.Run();
	}
}