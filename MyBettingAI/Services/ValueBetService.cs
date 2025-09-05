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
                // 1. Obtener datos de entrenamiento
                var trainingData = await _dataService.GetTrainingDataAsync(leagueId);

                // 2. Entrenar modelo si hay datos
                if (trainingData.Count > 30)
                {
                    _predictionService.TrainModel(trainingData, "SDCA");
                }

                // 3. Simular algunos partidos para prueba
                var simulatedMatches = new[]
                {
                    new { HomeTeam = "Barcelona", AwayTeam = "Real Madrid" },
                    new { HomeTeam = "Atlético Madrid", AwayTeam = "Sevilla" },
                    new { HomeTeam = "Valencia", AwayTeam = "Villarreal" }
                };

                foreach (var match in simulatedMatches)
                {
                    // Usar valores por defecto para las features por ahora
                    var prediction = _predictionService.PredictMatch(
                        homeStrength: 85.5f,
                        awayStrength: 87.2f,
                        homeForm: 3f,
                        awayForm: 2f,
                        homeAttack: 1.8f,
                        awayAttack: 1.5f,
                        homeDefense: 1.2f,
                        awayDefense: 1.4f,
                        historicalDraws: 1f
                    );

                    // Calcular value bet simplificado
                    var homeValue = (prediction.HomeWin * 2.5) - 1;
                    var drawValue = (prediction.Draw * 3.2) - 1;
                    var awayValue = (prediction.AwayWin * 2.8) - 1;

                    if (homeValue > minValue || drawValue > minValue || awayValue > minValue)
                    {
                        valueBets.Add(new ValueBet
                        {
                            HomeTeam = match.HomeTeam,
                            AwayTeam = match.AwayTeam,
                            HomeWinProb = prediction.HomeWin,
                            DrawProb = prediction.Draw,
                            AwayWinProb = prediction.AwayWin,
                            HomeValue = homeValue,
                            DrawValue = drawValue,
                            AwayValue = awayValue,
                            RecommendedBet = homeValue > drawValue && homeValue > awayValue ? "HOME_WIN" :
                                           drawValue > homeValue && drawValue > awayValue ? "DRAW" : "AWAY_WIN"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error buscando value bets: {ex.Message}");
            }

            return valueBets;
        }

        // Métodos auxiliares simplificados (los implementaremos luego)
        private async Task<Team> FindTeamByName(string teamName)
        {
            var allTeams = await _dataService.GetAllTeamsAsync();
            return allTeams.FirstOrDefault(t =>
                teamName.Contains(t.Name, StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains(teamName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class ValueBet
    {
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public double HomeWinProb { get; set; }
        public double DrawProb { get; set; }
        public double AwayWinProb { get; set; }
        public double HomeValue { get; set; }
        public double DrawValue { get; set; }
        public double AwayValue { get; set; }
        public string RecommendedBet { get; set; }
    }
}