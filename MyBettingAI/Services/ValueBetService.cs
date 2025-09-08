using MyBettingAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyBettingAI.Services
{
    /// <summary>
    /// Service for calculating value bets based on predicted match outcomes.
    /// Servicio para calcular apuestas de valor basado en predicciones de resultados de partidos.
    /// </summary>
    public class ValueBetService
    {
        private readonly DataService _dataService;
        private readonly PredictionService _predictionService;

        /// <summary>
        /// Constructor injecting required services.
        /// Constructor inyectando los servicios necesarios.
        /// </summary>
        public ValueBetService(DataService dataService, PredictionService predictionService)
        {
            _dataService = dataService;
            _predictionService = predictionService;
        }

        /// <summary>
        /// Finds value bets for a given league.
        /// Busca apuestas de valor para una liga específica.
        /// </summary>
        /// <param name="leagueId">League identifier / ID de la liga</param>
        /// <param name="minValue">Minimum value threshold to consider a bet / Umbral mínimo para considerar una apuesta</param>
        /// <returns>List of ValueBet objects / Lista de objetos ValueBet</returns>
        public async Task<List<ValueBet>> FindValueBetsAsync(int leagueId, double minValue = 0.1)
        {
            var valueBets = new List<ValueBet>();

            try
            {
                // 1. Retrieve training data for the league
                // Obtener datos de entrenamiento para la liga
                var trainingData = await _dataService.GetTrainingDataAsync(leagueId);

                // 2. Train the prediction model if sufficient data
                // Entrenar el modelo si hay suficientes datos
                if (trainingData.Count > 30)
                {
                    _predictionService.TrainModel(trainingData, "SDCA");
                }

                // 3. Simulate some matches (example data)
                // Simular algunos partidos (datos de ejemplo)
                var simulatedMatches = new[]
                {
                    new { HomeTeam = "Barcelona", AwayTeam = "Real Madrid" },
                    new { HomeTeam = "Atlético Madrid", AwayTeam = "Sevilla" },
                    new { HomeTeam = "Valencia", AwayTeam = "Villarreal" }
                };

                foreach (var match in simulatedMatches)
                {
                    // Use default feature values for now
                    // Usar valores por defecto para las características por ahora
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

                    // Calculate simplified value bet
                    // Calcular valor de apuesta simplificado
                    var homeValue = (prediction.HomeWin * 2.5) - 1;
                    var drawValue = (prediction.Draw * 3.2) - 1;
                    var awayValue = (prediction.AwayWin * 2.8) - 1;

                    // Include only bets above minimum threshold
                    // Incluir solo apuestas por encima del umbral mínimo
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
                Console.WriteLine($"Error finding value bets: {ex.Message}");
            }

            return valueBets;
        }

        #region Helper Methods

        /// <summary>
        /// Finds a team by name from the database (helper method).
        /// Busca un equipo por nombre en la base de datos (método auxiliar).
        /// </summary>
        /// <param name="teamName">Team name / Nombre del equipo</param>
        /// <returns>Team object / Objeto Team</returns>
        private async Task<Team> FindTeamByName(string teamName)
        {
            var allTeams = await _dataService.GetAllTeamsAsync();
            return allTeams.FirstOrDefault(t =>
                teamName.Contains(t.Name, StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains(teamName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }

    /// <summary>
    /// Represents a calculated value bet.
    /// Representa una apuesta de valor calculada.
    /// </summary>
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
