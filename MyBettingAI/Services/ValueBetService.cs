using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyBettingAI.Services
{
    public class ValueBetService
    {
        private readonly DataService _dataService;
        private readonly PredictionService _predictionService;

        public ValueBetService(DataService dataService, PredictionService predictionService)
        {
            _dataService = dataService;
            _predictionService = predictionService;
        }

        public async Task<List<ValueBet>> FindValueBetsAsync(int leagueId, double minValue = 0.1)
        {
            var valueBets = new List<ValueBet>();

            try
            {
                var trainingData = await _dataService.GetTrainingDataAsync(leagueId);

                // Entrenar modelo si hay datos
                if (trainingData.Count > 30)
                {
                    _predictionService.TrainModel(trainingData);
                }

                // Simular partidos para prueba
                var simulatedMatches = new[]
                {
                    new { HomeTeam = "Barcelona", AwayTeam = "Real Madrid", HomeStrength = 85.5f, AwayStrength = 87.2f },
                    new { HomeTeam = "Atlético Madrid", AwayTeam = "Sevilla", HomeStrength = 82.1f, AwayStrength = 78.5f },
                    new { HomeTeam = "Valencia", AwayTeam = "Villarreal", HomeStrength = 79.8f, AwayStrength = 80.3f }
                };

                foreach (var match in simulatedMatches)
                {
                    var prediction = _predictionService.PredictMatch(
                        match.HomeStrength,
                        match.AwayStrength,
                        5, // homeWins
                        3, // awayWins
                        1.8f, // avgHomeGoals
                        1.2f  // avgAwayGoals
                    );

                    var valueBet = AnalyzeValue(prediction, match.HomeTeam, match.AwayTeam, minValue);
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

            return valueBets;
        }

        private ValueBet AnalyzeValue((double HomeWin, double Draw, double AwayWin) prediction,
                                    string homeTeam, string awayTeam, double minValue)
        {
            // Cuotas ficticias
            var homeOdds = 2.5;
            var drawOdds = 3.2;
            var awayOdds = 2.8;

            // Calcular value
            var homeValue = (prediction.HomeWin * homeOdds) - 1;
            var drawValue = (prediction.Draw * drawOdds) - 1;
            var awayValue = (prediction.AwayWin * awayOdds) - 1;

            if (homeValue >= minValue || drawValue >= minValue || awayValue >= minValue)
            {
                return new ValueBet
                {
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    MatchDate = DateTime.Now.AddDays(1),
                    HomeWinProb = prediction.HomeWin,
                    DrawProb = prediction.Draw,
                    AwayWinProb = prediction.AwayWin,
                    HomeOdds = homeOdds,
                    DrawOdds = drawOdds,
                    AwayOdds = awayOdds,
                    HomeValue = homeValue,
                    DrawValue = drawValue,
                    AwayValue = awayValue,
                    RecommendedBet = GetRecommendedBet(homeValue, drawValue, awayValue, minValue)
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

    public class ValueBet
    {
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public DateTime MatchDate { get; set; }
        public double HomeWinProb { get; set; }
        public double DrawProb { get; set; }
        public double AwayWinProb { get; set; }
        public double HomeOdds { get; set; }
        public double DrawOdds { get; set; }
        public double AwayOdds { get; set; }
        public double HomeValue { get; set; }
        public double DrawValue { get; set; }
        public double AwayValue { get; set; }
        public string RecommendedBet { get; set; }
    }
}