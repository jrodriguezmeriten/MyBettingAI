using Data;
using MyBettingAI.Models;
using MyBettingAI.Services;
using System;
using System.Threading.Tasks;

// Punto de entrada asíncrono
await MainAsync();

static async Task MainAsync()
{
    try
    {
        Console.WriteLine("🔄 Initializing database...");
        var dbContext = new DatabaseContext();
        dbContext.InitializeDatabase();

        // LIMPIAR base de datos antes de empezar (Opción 2)
        dbContext.CleanDatabase();
        Console.WriteLine("✅ Database cleaned and ready!");

        // Crear el DataService
        var dataService = new DataService(dbContext.GetConnectionString());

        Console.WriteLine("📝 Inserting test data...");

        // Insertar una liga de prueba
        var leagueId = await dataService.InsertLeagueAsync(new League
        {
            Name = "LaLiga Santander",
            Country = "Spain",
            ApiId = 140
        });
        Console.WriteLine($"✅ Liga insertada con ID: {leagueId}");

        // Insertar equipos de prueba
        var team1Id = await dataService.InsertTeamAsync(new Team
        {
            Name = "FC Barcelona",
            LeagueId = leagueId,
            StrengthRating = 85.5,
            ApiId = 529
        });
        Console.WriteLine($"✅ Equipo insertado: FC Barcelona (ID: {team1Id})");

        var team2Id = await dataService.InsertTeamAsync(new Team
        {
            Name = "Real Madrid",
            LeagueId = leagueId,
            StrengthRating = 87.2,
            ApiId = 541
        });
        Console.WriteLine($"✅ Equipo insertado: Real Madrid (ID: {team2Id})");

        // Insertar un partido de prueba
        var matchId = await dataService.InsertMatchAsync(new Match
        {
            LeagueId = leagueId,
            HomeTeamId = team1Id,
            AwayTeamId = team2Id,
            MatchDate = new DateTime(2024, 3, 10, 18, 45, 0),
            HomeScore = 2,
            AwayScore = 2,
            HomeExpectedGoals = 1.8,
            AwayExpectedGoals = 1.5,
            HomeShots = 15,
            AwayShots = 12,
            HomeShotsOnTarget = 6,
            AwayShotsOnTarget = 5,
            HomePossession = 65,
            AwayPossession = 35
        });
        Console.WriteLine($"✅ Partido insertado con ID: {matchId}");

        // Leer y mostrar todas las ligas
        Console.WriteLine("\n📋 Listando todas las ligas:");
        var leagues = await dataService.GetAllLeaguesAsync();
        foreach (var league in leagues)
        {
            Console.WriteLine($"   - {league.Name} ({league.Country}) [API ID: {league.ApiId}]");
        }

        // Leer y mostrar equipos de la liga
        Console.WriteLine($"\n📋 Listando equipos de LaLiga:");
        var teams = await dataService.GetTeamsByLeagueAsync(leagueId);
        foreach (var team in teams)
        {
            Console.WriteLine($"   - {team.Name} [Rating: {team.StrengthRating}]");
        }

        // Leer partidos de los últimos 30 días
        Console.WriteLine($"\n📋 Partidos recientes:");
        var recentMatches = await dataService.GetMatchesByDateRangeAsync(
            DateTime.Now.AddDays(-30),
            DateTime.Now.AddDays(1));

        foreach (var match in recentMatches)
        {
            Console.WriteLine($"   - Partido ID: {match.Id} | {match.MatchDate:dd/MM/yyyy} | " +
                            $"Resultado: {match.HomeScore}-{match.AwayScore}");
        }

        Console.WriteLine("\n🎉 ¡Datos insertados y leídos correctamente!");
        Console.WriteLine("🚀 MyBettingAI is running!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        Console.WriteLine($"📋 Detalles: {ex.StackTrace}");
    }
}