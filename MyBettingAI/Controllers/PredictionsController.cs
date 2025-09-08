using Microsoft.AspNetCore.Mvc;
using MyBettingAI.Models;
using MyBettingAI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyBettingAI.Controllers
{
    /// <summary>
    /// Controller for handling predictions, value bets, and football data synchronization.
    /// Controlador para manejar predicciones, apuestas de valor y sincronización de datos de fútbol.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PredictionsController : ControllerBase
    {
        private readonly ValueBetService _valueBetService;
        private readonly DataService _dataService;
        private readonly FootballDataService _footballService;
        private readonly OddsApiService _oddsApiService;

        /// <summary>
        /// Constructor injecting required services.
        /// Constructor inyectando los servicios necesarios.
        /// </summary>
        public PredictionsController(
            ValueBetService valueBetService,
            DataService dataService,
            FootballDataService footballService,
            OddsApiService oddsApiService)
        {
            _valueBetService = valueBetService;
            _dataService = dataService;
            _footballService = footballService;
            _oddsApiService = oddsApiService;
        }

        #region Basic Endpoints

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "API is working!", timestamp = DateTime.UtcNow });
        }

        [HttpGet("leagues")]
        public async Task<ActionResult<IEnumerable<League>>> GetLeagues()
        {
            try
            {
                var leagues = await _dataService.GetAllLeaguesAsync();
                return Ok(leagues);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("valuebets/{leagueId}")]
        public async Task<ActionResult<List<ValueBet>>> GetValueBets(int leagueId, [FromQuery] double minValue = 0.1)
        {
            try
            {
                var valueBets = await _valueBetService.FindValueBetsAsync(leagueId, minValue);

                if (!valueBets.Any())
                {
                    return Ok(new
                    {
                        message = "No value bets found",
                        suggestion = "Try lowering the minValue parameter or sync more data"
                    });
                }

                return Ok(valueBets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("sync/{leagueApiId}")]
        public async Task<ActionResult> SyncData(int leagueApiId)
        {
            try
            {
                await _dataService.CleanDatabaseAsync();
                var leagueCount = await _dataService.SyncLeaguesFromFootballDataAsync(_footballService);
                var teamCount = await _dataService.SyncTeamsFromFootballDataAsync(_footballService, leagueApiId);
                var matchCount = await _dataService.SyncHistoricalMatchesAsync(_footballService, leagueApiId, 2024);

                return Ok(new
                {
                    message = "Datos sincronizados correctamente",
                    leagues = leagueCount,
                    teams = teamCount,
                    matches = matchCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sincronizando datos: {ex.Message}");
            }
        }

        [HttpGet("sync-get/{leagueApiId}")]
        public async Task<ActionResult> SyncDataGet(int leagueApiId)
        {
            try
            {
                await _dataService.SyncLeaguesFromFootballDataAsync(_footballService);
                var teamCount = await _dataService.SyncTeamsFromFootballDataAsync(_footballService, leagueApiId);
                var matchCount = await _dataService.SyncHistoricalMatchesAsync(_footballService, leagueApiId, 2024);

                return Ok(new
                {
                    message = "Datos sincronizados correctamente",
                    teams = teamCount,
                    matches = matchCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sincronizando datos: {ex.Message}");
            }
        }

        [HttpPost("clean-database")]
        public async Task<ActionResult> CleanDatabase()
        {
            try
            {
                await _dataService.CleanDatabaseAsync();
                return Ok(new { message = "Base de datos limpiada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error limpiando base de datos: {ex.Message}");
            }
        }

        [HttpGet("odds/test")]
        public async Task<ActionResult> TestOddsApi()
        {
            try
            {
                var sports = await _oddsApiService.GetSportsAsync();
                var soccerOdds = await _oddsApiService.GetSoccerOddsAsync();

                return Ok(new
                {
                    message = "Odds API connected successfully",
                    sportsCount = sports.Count,
                    oddsCount = soccerOdds.Count,
                    sampleMatches = soccerOdds.Take(3).Select(o => new
                    {
                        o.HomeTeam,
                        o.AwayTeam,
                        o.CommenceTime,
                        Bookmakers = o.Bookmakers?.Count
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Odds API test failed: {ex.Message}");
            }
        }

        #endregion

        #region Debug / Development Endpoints

        [HttpGet("debug/data")]
        public async Task<ActionResult> DebugData(int leagueId = 2014)
        {
            try
            {
                var trainingData = await _dataService.GetTrainingDataAsync(leagueId);
                var teams = await _dataService.GetAllTeamsAsync();

                return Ok(new
                {
                    TrainingDataCount = trainingData.Count,
                    TeamsCount = teams.Count(),
                    SampleData = trainingData.Take(3),
                    TeamStrengths = teams.Select(t => new { t.Name, t.StrengthRating })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("debug/predictions")]
        public async Task<ActionResult> DebugPredictions()
        {
            try
            {
                var predictionService = new PredictionService();

                var prediction1 = predictionService.PredictMatch(90f, 50f, 4f, 1f, 2.5f, 1.2f, 0.8f, 1.8f, 2f);
                var prediction2 = predictionService.PredictMatch(50f, 90f, 1f, 4f, 1.2f, 2.5f, 1.8f, 0.8f, 1f);
                var prediction3 = predictionService.PredictMatch(70f, 70f, 2f, 2f, 1.8f, 1.8f, 1.2f, 1.2f, 3f);

                return Ok(new
                {
                    StrongHome = prediction1,
                    StrongAway = prediction2,
                    EvenMatch = prediction3
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("debug/model")]
        public async Task<ActionResult> DebugModel()
        {
            try
            {
                var trainingData = await _dataService.GetTrainingDataAsync(2014);
                var predictionService = new PredictionService();

                if (trainingData.Count > 10)
                {
                    var sampleData = trainingData.Take(50).ToList();
                    var metrics = predictionService.TrainModel(sampleData, "SDCA");

                    var prediction1 = predictionService.PredictSimple(90f, 50f);
                    var prediction2 = predictionService.PredictSimple(50f, 90f);
                    var prediction3 = predictionService.PredictSimple(70f, 70f);

                    return Ok(new
                    {
                        TrainingDataCount = trainingData.Count,
                        SampleSize = sampleData.Count,
                        ModelAccuracy = metrics.Accuracy,
                        Predictions = new
                        {
                            StrongHome = prediction1,
                            StrongAway = prediction2,
                            EvenMatch = prediction3
                        },
                        Message = "Modelo entrenado con muestra de 50 partidos"
                    });
                }
                else
                {
                    return Ok(new
                    {
                        TrainingDataCount = trainingData.Count,
                        Message = "Necesitas más datos de entrenamiento. Ejecuta: POST /api/predictions/sync/2014"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace,
                    Details = "Revisa la consola para más información"
                });
            }
        }

        [HttpGet("debug/rawdata")]
        public async Task<ActionResult> DebugRawData()
        {
            try
            {
                var trainingData = await _dataService.GetTrainingDataAsync(2014);
                var sample = trainingData.Take(5).ToList();

                return Ok(new
                {
                    TotalCount = trainingData.Count,
                    SampleData = sample,
                    Features = sample.Select(s => new
                    {
                        s.HomeTeamStrength,
                        s.AwayTeamStrength,
                        s.HomeForm,
                        s.AwayForm,
                        s.Label
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("debug/football-api")]
        public async Task<ActionResult> DebugFootballApi()
        {
            try
            {
                var competitions = await _footballService.GetCompetitionsAsync();
                var standings = await _footballService.GetStandingsAsync(2014);
                var matches = await _footballService.GetHistoricalMatchesAsync(2014, 2023);

                return Ok(new
                {
                    CompetitionsCount = competitions?.Count ?? 0,
                    HasStandings = standings?.Standings?.Count > 0,
                    MatchesCount = matches?.Matches?.Count ?? 0,
                    SampleCompetitions = competitions?.Take(3).Select(c => c.Name),
                    Message = "Revisa la consola para más detalles"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    Details = ex.StackTrace
                });
            }
        }

    }
}
#endregion