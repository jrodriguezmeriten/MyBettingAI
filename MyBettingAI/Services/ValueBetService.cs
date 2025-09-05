using MyBettingAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyBettingAI.Services
{
    public class ValueBetService
    {
        private readonly DataService _dataService;
        private readonly PredictionService _predictionService;
        private readonly OddsApiService _oddsApiService;

        public ValueBetService(DataService dataService, PredictionService predictionService, OddsApiService oddsApiService)
        {
            _dataService = dataService;
            _predictionService = predictionService;
            _oddsApiService = oddsApiService;
        }

        private readonly Dictionary<string, string> _teamNameMappings = new(StringComparer.OrdinalIgnoreCase)
{
    // Mapeos comunes para LaLiga
    {"Real Madrid", "Real Madrid CF"},
    {"Barcelona", "FC Barcelona"},
    {"Atletico Madrid", "Club Atlético de Madrid"},
    {"Atlético Madrid", "Club Atlético de Madrid"},
    {"Sevilla", "Sevilla FC"},
    {"Valencia", "Valencia CF"},
    {"Villarreal", "Villarreal CF"},
    {"Real Sociedad", "Real Sociedad de Fútbol"},
    {"Athletic Bilbao", "Athletic Club"},
    {"Real Betis", "Real Betis Balompié"},
    {"Celta Vigo", "RC Celta de Vigo"},
    {"Espanyol", "RCD Espanyol de Barcelona"},
    {"Getafe", "Getafe CF"},
    {"Osasuna", "CA Osasuna"},
    {"Mallorca", "RCD Mallorca"},
    {"Alaves", "Deportivo Alavés"},
    {"Levante", "Levante UD"},
    {"Granada", "Granada CF"},
    {"Cadiz", "Cádiz CF"},
    {"Elche", "Elche CF"},
    
    // Mapeos para Premier League
    {"Manchester United", "Manchester United"},
    {"Manchester City", "Manchester City"},
    {"Liverpool", "Liverpool"},
    {"Chelsea", "Chelsea"},
    {"Arsenal", "Arsenal"},
    {"Tottenham", "Tottenham Hotspur"},
    // ... añadir más mapeos según necesites
};

        public async Task<List<ValueBet>> FindValueBetsAsync(int leagueId, double minValue = 0.1)
        {
            var valueBets = new List<ValueBet>();

            try
            {
                // 1. Obtener cuotas reales de la API
                var realOdds = await _oddsApiService.GetSoccerOddsAsync();

                // 2. Obtener datos de entrenamiento
                var trainingData = await _dataService.GetTrainingDataAsync(leagueId);

                // 3. Entrenar modelo si hay datos
                if (trainingData.Count > 30)
                {
                    _predictionService.TrainModel(trainingData);
                }

                // 4. Analizar cada partido con cuotas reales
                foreach (var odds in realOdds)
                {
                    var valueBet = await AnalyzeGameWithRealOdds(odds, minValue);
                    if (valueBet != null)
                    {
                        valueBets.Add(valueBet);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error buscando value bets: {ex.Message}");
            }

            return valueBets.OrderByDescending(v => v.BestValue).ToList();
        }

        private async Task<ValueBet> AnalyzeGameWithRealOdds(OddsApiOdds odds, double minValue)
        {
            try
            {
                // Buscar equipos en nuestra base de datos
                var homeTeam = await FindTeamByName(odds.HomeTeam);
                var awayTeam = await FindTeamByName(odds.AwayTeam);

                if (homeTeam == null || awayTeam == null)
                    return null;

                // Hacer predicción
                var prediction = _predictionService.PredictMatch(
                    homeTeam.StrengthRating,
                    awayTeam.StrengthRating,
                    5, // homeWins
                    3, // awayWins
                    1.8f, // avgHomeGoals
                    1.2f  // avgAwayGoals
                );

                // Encontrar las mejores cuotas del mercado
                var bestOdds = FindBestOdds(odds.Bookmakers);

                // Calcular value bets con cuotas reales
                return CalculateValueBet(odds, prediction, bestOdds, homeTeam, awayTeam, minValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error analyzing game: {ex.Message}");
                return null;
            }
        }

        private async Task<Team> FindTeamByName(string teamName)
        {
            try
            {
                // Verificar si tenemos un mapeo manual
                if (_teamNameMappings.TryGetValue(teamName, out var mappedName))
                {
                    teamName = mappedName;
                }

                var cleanedName = CleanTeamName(teamName);
                var allTeams = await _dataService.GetAllTeamsAsync();

                // Búsqueda exacta primero
                var team = allTeams.FirstOrDefault(t =>
                    CleanTeamName(t.Name).Equals(cleanedName, StringComparison.OrdinalIgnoreCase));

                if (team != null)
                    return team;

                // Búsqueda por contención
                team = allTeams.FirstOrDefault(t =>
                    CleanTeamName(t.Name).Contains(cleanedName, StringComparison.OrdinalIgnoreCase) ||
                    cleanedName.Contains(CleanTeamName(t.Name), StringComparison.OrdinalIgnoreCase));

                if (team != null)
                {
                    Console.WriteLine($"✅ Mapping found: {teamName} -> {team.Name}");
                    return team;
                }

                Console.WriteLine($"⚠️ Team not found in database: {teamName}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error finding team {teamName}: {ex.Message}");
                return null;
            }
        }

        private string CleanTeamName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Remover palabras comunes y normalizar
            return name
                .Replace("FC", "", StringComparison.OrdinalIgnoreCase)
                .Replace("CF", "", StringComparison.OrdinalIgnoreCase)
                .Replace("AFC", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Club", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Team", "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToLower();
        }

        private BestOdds FindBestOdds(List<Bookmaker> bookmakers)
        {
            var bestOdds = new BestOdds();

            foreach (var bookmaker in bookmakers)
            {
                foreach (var market in bookmaker.Markets.Where(m => m.Key == "h2h"))
                {
                    foreach (var outcome in market.Outcomes)
                    {
                        if (outcome.Name.Equals("Home", StringComparison.OrdinalIgnoreCase))
                            bestOdds.HomeOdds = Math.Max(bestOdds.HomeOdds, outcome.Price);
                        else if (outcome.Name.Equals("Away", StringComparison.OrdinalIgnoreCase))
                            bestOdds.AwayOdds = Math.Max(bestOdds.AwayOdds, outcome.Price);
                        else if (outcome.Name.Equals("Draw", StringComparison.OrdinalIgnoreCase))
                            bestOdds.DrawOdds = Math.Max(bestOdds.DrawOdds, outcome.Price);
                    }
                }
            }

            return bestOdds;
        }

        private ValueBet CalculateValueBet(OddsApiOdds odds, (double HomeWin, double Draw, double AwayWin) prediction,
                                         BestOdds bestOdds, Team homeTeam, Team awayTeam, double minValue)
        {
            var homeValue = (prediction.HomeWin * (double)bestOdds.HomeOdds) - 1;
            var drawValue = (prediction.Draw * (double)bestOdds.DrawOdds) - 1;
            var awayValue = (prediction.AwayWin * (double)bestOdds.AwayOdds) - 1;

            var bestValue = Math.Max(homeValue, Math.Max(drawValue, awayValue));

            if (bestValue >= minValue)
            {
                return new ValueBet
                {
                    Id = odds.Id,
                    HomeTeam = odds.HomeTeam,
                    AwayTeam = odds.AwayTeam,
                    MatchDate = odds.CommenceTime,
                    HomeWinProb = prediction.HomeWin,
                    DrawProb = prediction.Draw,
                    AwayWinProb = prediction.AwayWin,
                    HomeOdds = bestOdds.HomeOdds,
                    DrawOdds = bestOdds.DrawOdds,
                    AwayOdds = bestOdds.AwayOdds,
                    HomeValue = homeValue,
                    DrawValue = drawValue,
                    AwayValue = awayValue,
                    BestValue = bestValue,
                    RecommendedBet = GetRecommendedBet(homeValue, drawValue, awayValue, minValue),
                    Bookmakers = odds.Bookmakers.Select(b => b.Title).ToList()
                };
            }

            return null;
        }

        private string GetRecommendedBet(double homeValue, double drawValue, double awayValue, double minValue)
        {
            if (homeValue >= minValue && homeValue >= drawValue && homeValue >= awayValue)
                return "HOME_WIN";
            if (drawValue >= minValue && drawValue >= homeValue && drawValue >= awayValue)
                return "DRAW";
            if (awayValue >= minValue && awayValue >= homeValue && awayValue >= drawValue)
                return "AWAY_WIN";

            return "NO_VALUE";
        }
    }

    public class BestOdds
    {
        public decimal HomeOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayOdds { get; set; }
    }

    public class ValueBet
    {
        public string Id { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public DateTime MatchDate { get; set; }
        public double HomeWinProb { get; set; }
        public double DrawProb { get; set; }
        public double AwayWinProb { get; set; }
        public decimal HomeOdds { get; set; }
        public decimal DrawOdds { get; set; }
        public decimal AwayOdds { get; set; }
        public double HomeValue { get; set; }
        public double DrawValue { get; set; }
        public double AwayValue { get; set; }
        public double BestValue { get; set; }
        public string RecommendedBet { get; set; }
        public List<string> Bookmakers { get; set; }
    }
}