using Data;
using MyBettingAI.Models;
using MyBettingAI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

await MainAsync();

static async Task MainAsync()
{
    try
    {
        Console.WriteLine("🔄 Initializing database...");
        var dbContext = new DatabaseContext();
        dbContext.InitializeDatabase();
        dbContext.CleanDatabase();
        Console.WriteLine("✅ Database ready!");

        // TU API KEY de Football-Data.org
        var apiKey = "f1e713f8666b42d8b29affe6c9ac478e"; // ← REEMPLAZA con tu API key real

        var footballService = new FootballDataService(apiKey);
        var dataService = new DataService(dbContext.GetConnectionString());

        Console.WriteLine("🌍 Obteniendo datos de Football-Data.org...");

        // 1. Sincronizar ligas
        Console.WriteLine("\n📋 Sincronizando ligas...");
        var leagueCount = await dataService.SyncLeaguesFromFootballDataAsync(footballService);
        Console.WriteLine($"✅ {leagueCount} ligas sincronizadas");

        // 2. Obtener LaLiga (ID: 2014)
        var leagues = await dataService.GetAllLeaguesAsync();
        var laliga = leagues.FirstOrDefault(l => l.Name.Contains("La Liga") || l.ApiId == 2014);

        if (laliga == null)
        {
            Console.WriteLine("❌ No se encontró LaLiga");
            return;
        }

        // 3. Sincronizar equipos de LaLiga
        Console.WriteLine($"\n📋 Sincronizando equipos de {laliga.Name}...");
        var teamCount = await dataService.SyncTeamsFromFootballDataAsync(footballService, laliga.ApiId.Value);
        Console.WriteLine($"✅ {teamCount} equipos sincronizados");

        // 4. Mostrar resultados
        Console.WriteLine("\n📊 Resumen de la sincronización:");
        Console.WriteLine($"   - Ligas: {leagueCount}");
        Console.WriteLine($"   - Equipos en {laliga.Name}: {teamCount}");

        // 5. Mostrar algunos equipos
        var teams = await dataService.GetTeamsByLeagueAsync(laliga.Id);
        Console.WriteLine($"\n🏆 Equipos de {laliga.Name}:");
        foreach (var team in teams.Take(5))
        {
            Console.WriteLine($"   - {team.Name} [Rating: {team.StrengthRating:F1}]");
        }

        Console.WriteLine("\n🎉 ¡Sincronización completada!");
        Console.WriteLine("🚀 MyBettingAI is running with real data!");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"📋 Inner: {ex.InnerException.Message}");
        }
    }
}