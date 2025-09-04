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
        var apiKey = "f1e713f8666b42d8b29affe6c9ac478e";

        var footballService = new FootballDataService(apiKey);
        var dataService = new DataService(dbContext.GetConnectionString());

        Console.WriteLine("🌍 Obteniendo datos de Football-Data.org...");

        // 1. Sincronizar ligas
        Console.WriteLine("\n📋 Sincronizando ligas...");
        var leagueCount = await dataService.SyncLeaguesFromFootballDataAsync(footballService);
        Console.WriteLine($"✅ {leagueCount} ligas sincronizadas");

        // 2. Obtener LaLiga
        var leagues = await dataService.GetAllLeaguesAsync();
        var laliga = leagues.FirstOrDefault(l => l.Name.Contains("Primera Division") || l.ApiId == 2014);

        if (laliga == null)
        {
            Console.WriteLine("❌ No se encontró LaLiga");
            return;
        }

        // 3. Sincronizar equipos de LaLiga
        Console.WriteLine($"\n📋 Sincronizando equipos de {laliga.Name}...");
        var teamCount = await dataService.SyncTeamsFromFootballDataAsync(footballService, laliga.ApiId.Value);
        Console.WriteLine($"✅ {teamCount} equipos sincronizados");

        // 4. Sincronizar partidos históricos
        Console.WriteLine($"\n📋 Sincronizando partidos históricos de {laliga.Name}...");
        var matchesCount = await dataService.SyncHistoricalMatchesAsync(footballService, laliga.ApiId.Value, 2023);
        Console.WriteLine($"✅ {matchesCount} partidos históricos sincronizados");

        // 5. Mostrar resultados
        Console.WriteLine("\n📊 Resumen de la sincronización:");
        Console.WriteLine($"   - Ligas: {leagueCount}");
        Console.WriteLine($"   - Equipos en {laliga.Name}: {teamCount}");
        Console.WriteLine($"   - Partidos históricos: {matchesCount}");

        // 6. Mostrar algunos equipos
        var teams = await dataService.GetTeamsByLeagueAsync(laliga.Id);
        Console.WriteLine($"\n🏆 Equipos de {laliga.Name}:");
        foreach (var team in teams.Take(5))
        {
            Console.WriteLine($"   - {team.Name} [Rating: {team.StrengthRating:F1}]");
        }

        // 7. Entrenar modelo ML
        Console.WriteLine("\n🤖 Entrenando modelo de Machine Learning...");
        var trainingData = await dataService.GetTrainingDataAsync(laliga.Id);
        Console.WriteLine($"📊 {trainingData.Count} partidos históricos para entrenar");

        if (trainingData.Count > 30)
        {
            var predictionService = new PredictionService();
            predictionService.TrainModel(trainingData);

            // Ejemplo de predicción
            var probabilities = predictionService.PredictMatch(
                homeStrength: 85.5f,
                awayStrength: 87.2f,
                homeWins: 8f,
                awayWins: 7f,
                avgHomeGoals: 2.1f,
                avgAwayGoals: 2.3f
            );

            Console.WriteLine($"\n🔮 Predicción de ejemplo:");
            Console.WriteLine($"   - Victoria local: {probabilities.HomeWin:P1}");
            Console.WriteLine($"   - Empate: {probabilities.Draw:P1}");
            Console.WriteLine($"   - Victoria visitante: {probabilities.AwayWin:P1}");

            // Análisis de value bet
            var homeValue = (probabilities.HomeWin * 2.5) - 1;
            var drawValue = (probabilities.Draw * 3.2) - 1;
            var awayValue = (probabilities.AwayWin * 2.8) - 1;

            Console.WriteLine($"\n💰 Value Bet Analysis:");
            Console.WriteLine($"   - Home Value: {homeValue:F3}");
            Console.WriteLine($"   - Draw Value: {drawValue:F3}");
            Console.WriteLine($"   - Away Value: {awayValue:F3}");

            if (homeValue > 0.1) Console.WriteLine("   ✅ VALUE BET: Home Win");
            else if (drawValue > 0.1) Console.WriteLine("   ✅ VALUE BET: Draw");
            else if (awayValue > 0.1) Console.WriteLine("   ✅ VALUE BET: Away Win");
            else Console.WriteLine("   ❌ No clear value bet");
        }
        else
        {
            Console.WriteLine("⚠️ No hay suficientes partidos para entrenar el modelo");
        }

        Console.WriteLine("\n🎉 ¡Proceso completado!");
        Console.WriteLine("🚀 MyBettingAI is running!");

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