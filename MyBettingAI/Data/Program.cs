using Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MyBettingAI.Services;
using System;

namespace MyBettingAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Crear y configurar el host
            var host = CreateWebHostBuilder(args).Build();

            // Inicializar base de datos
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<DatabaseContext>();
                    dbContext.InitializeDatabase();
                    Console.WriteLine("✅ Database initialized");
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
            Console.WriteLine("");
            Console.WriteLine("🔑 API Key configurada: f1e713f8666b42d8b29affe6c9ac478e");

            host.Run();
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

            // Registrar DatabaseContext
            services.AddSingleton<DatabaseContext>();

            // Registrar DataService con la connection string correctamente
            services.AddScoped<DataService>(provider =>
            {
                var dbContext = provider.GetRequiredService<DatabaseContext>();
                return new DataService(dbContext.GetConnectionString());
            });

            // Registrar otros servicios
            services.AddScoped<PredictionService>();
            services.AddScoped<ValueBetService>();
            services.AddScoped<FootballDataService>(provider =>
                new FootballDataService("f1e713f8666b42d8b29affe6c9ac478e"));
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