using Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MyBettingAI.Services;
using System;
using System.Threading.Tasks;

namespace MyBettingAI
{
    /// <summary>
    /// Application entry point for MyBettingAI API.  
    /// Punto de entrada de la aplicación MyBettingAI API.
    /// </summary>
    using Data;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;  
    using System;
    using System.Threading.Tasks;

    namespace MyBettingAI
    {
        /// <summary>
        /// Application entry point for MyBettingAI API.  
        /// Punto de entrada de la aplicación MyBettingAI API.
        /// </summary>
        public class Program
        {
            /// <summary>
            /// Main method that builds and runs the web host.  
            /// Método principal que construye y ejecuta el host web.
            /// </summary>
            public static async Task Main(string[] args)
            {
                // Create and configure the host  
                // Crear y configurar el host
                var host = CreateWebHostBuilder(args).Build();

                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        // Initialize the database context  
                        // Inicializar el contexto de base de datos
                        var dbContext = services.GetRequiredService<DatabaseContext>();
                        dbContext.InitializeDatabase();
                        Console.WriteLine("Database initialized correctly");

                        // Load configuration from DI  
                        // Cargar configuración desde la inyección de dependencias
                        var config = services.GetRequiredService<IConfiguration>();
                        var footballApiKey = config["ApiKeys:FootballData"];

                        // Create services with API keys from configuration  
                        // Crear servicios con API keys desde la configuración
                        var dataService = services.GetRequiredService<DataService>();
                        var footballService = new FootballDataService(footballApiKey);

                        Console.WriteLine("Synchronizing LaLiga data...");
                        // Uncomment to enable auto-sync at startup  
                        // Descomentar para habilitar sincronización automática al inicio
                        // await dataService.SyncLeaguesFromFootballDataAsync(footballService);
                        // var teamCount = await dataService.SyncTeamsFromFootballDataAsync(footballService, 2014);
                        // var matchCount = await dataService.SyncHistoricalMatchesAsync(footballService, 2014, 2023);

                        // Console.WriteLine($"Data synchronized: {teamCount} teams, {matchCount} matches");
                    }
                    catch (Exception ex)
                    {
                        // Handle initialization errors  
                        // Manejar errores de inicialización
                        Console.WriteLine($"Error initializing database: {ex.Message}");
                    }
                }

                // Informational logs about the API endpoints  
                // Logs informativos sobre los endpoints de la API
                Console.WriteLine("MyBettingAI API is running...");
                Console.WriteLine("Available Endpoints:");
                Console.WriteLine("   - GET /api/predictions/valuebets/{leagueId}");
                Console.WriteLine("   - GET /api/predictions/leagues");
                Console.WriteLine("   - GET /api/predictions/test");
                Console.WriteLine("   - POST /api/predictions/sync/{leagueId}");
                Console.WriteLine("");

                // Run the host asynchronously  
                // Ejecutar el host de manera asíncrona
                await host.RunAsync();
            }

            /// <summary>
            /// Creates the web host builder with default configuration.  
            /// Crea el constructor del host web con la configuración por defecto.
            /// </summary>
            public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
                WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>()
                    .UseUrls("http://localhost:5100;https://localhost:7100"); // Puertos de launchSettings.json
        }
    }

        /// <summary>
        /// Startup class that configures services and middleware for the API.  
        /// Clase Startup que configura los servicios y el middleware para la API.
        /// </summary>
        public class Startup
        {
            private readonly IConfiguration _configuration;

            /// <summary>
            /// Constructor that receives configuration.  
            /// Constructor que recibe la configuración.
            /// </summary>
            public Startup(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            /// <summary>
            /// Configures dependency injection services.  
            /// Configura los servicios de inyección de dependencias.
            /// </summary>
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddControllers();

                // Register DatabaseContext as singleton  
                // Registrar DatabaseContext como singleton
                services.AddSingleton<DatabaseContext>();

                // Register DataService with proper connection string  
                // Registrar DataService proporcionando la cadena de conexión correctamente
                services.AddScoped<DataService>(provider =>
                {
                    var dbContext = provider.GetRequiredService<DatabaseContext>();
                    return new DataService(dbContext.GetConnectionString());
                });

                // Register domain services  
                // Registrar servicios de dominio
                services.AddScoped<PredictionService>();
                services.AddScoped<ValueBetService>();

                // Register FootballDataService with API key from environment/config  
                // Registrar FootballDataService con API key desde variables de entorno/config
                services.AddScoped<FootballDataService>(provider =>
                    new FootballDataService(_configuration["ApiKeys:FootballData"]));

                // Register OddsApiService with API key from environment/config  
                // Registrar OddsApiService con API key desde variables de entorno/config
                services.AddScoped<OddsApiService>(provider =>
                    new OddsApiService(_configuration["ApiKeys:OddsApi"]));
            }

            /// <summary>
            /// Configures the HTTP request pipeline.  
            /// Configura el pipeline de peticiones HTTP.
            /// </summary>
            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                if (env.IsDevelopment())
                {
                    // Use detailed exception page in development  
                    // Usar página de excepción detallada en desarrollo
                    app.UseDeveloperExceptionPage();
                }

                // Configure routing and authorization  
                // Configurar enrutamiento y autorización
                app.UseRouting();
                app.UseAuthorization();

                // Map controller endpoints  
                // Mapear endpoints de controladores
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            }
        }
    }

