using Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MyBettingAI.Services;
using System;
using System.Threading.Tasks;

namespace MyBettingAI
{
    public class Program
    {
        public static async Task Main(string[] args) // ← Cambiado a async Task
        {
            // Crear y configurar el host
            var host = CreateWebHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<DatabaseContext>();
                    dbContext.InitializeDatabase();
                    Console.WriteLine("✅ Database initialized");

                    // SINCRONIZAR DATOS AUTOMÁTICAMENTE AL INICIAR
                    var dataService = services.GetRequiredService<DataService>();
                    var footballService = new FootballDataService("f1e713f8666b42d8b29affe6c9ac478e");

                    Console.WriteLine("🔄 Sincronizando datos de LaLiga...");
                    //await dataService.SyncLeaguesFromFootballDataAsync(footballService);
                    //var teamCount = await dataService.SyncTeamsFromFootballDataAsync(footballService, 2014);
                    //var matchCount = await dataService.SyncHistoricalMatchesAsync(footballService, 2014, 2023);

                    //Console.WriteLine($"✅ Datos sincronizados: {teamCount} equipos, {matchCount} partidos");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error initializing database: {ex.Message}");
                }
            }

            Console.WriteLine("🚀 MyBettingAI API is running on: http://localhost:5000");
            Console.WriteLine("📋 Endpoints:");
            Console.WriteLine("   - GET /api/predictions/valuebets/{leagueId}");
            Console.WriteLine("   - GET /api/predictions/leagues");
            Console.WriteLine("   - GET /api/predictions/test");
            Console.WriteLine("   - POST /api/predictions/sync/{leagueId}");
            Console.WriteLine("");
            Console.WriteLine("🔑 API Key configurada: f1e713f8666b42d8b29affe6c9ac478e");

            await host.RunAsync(); // ← Cambiado a RunAsync
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5000");
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Registrar servicios
            services.AddSingleton<DatabaseContext>();
            services.AddScoped<DataService>();
            services.AddScoped<PredictionService>();
            services.AddScoped<ValueBetService>();
            services.AddScoped<FootballDataService>(provider =>
                new FootballDataService("f1e713f8666b42d8b29affe6c9ac478e"));

            // Registrar The Odds API Service
            services.AddScoped<OddsApiService>(provider =>
                new OddsApiService("12cf3c3e8f80dd1964602e9b133077b5")); // ← Aquí tu key de The Odds API
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}