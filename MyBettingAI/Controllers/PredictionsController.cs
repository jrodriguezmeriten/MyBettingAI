using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using MyBettingAI.Models;
using MyBettingAI.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyBettingAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PredictionsController : ControllerBase
    {
        private readonly ValueBetService _valueBetService;
        private readonly DataService _dataService;
        private readonly FootballDataService _footballService;
        private readonly OddsApiService _oddsApiService; 

        // Inyectar FootballDataService en el constructor
        public PredictionsController(ValueBetService valueBetService, DataService dataService, FootballDataService footballService, OddsApiService oddsApiService)
        {
            _valueBetService = valueBetService;
            _dataService = dataService;
            _footballService = footballService;
            _oddsApiService = oddsApiService;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "✅ API is working!", timestamp = DateTime.UtcNow });
        }

        [HttpPost("sync/{leagueApiId}")]
        public async Task<ActionResult> SyncData(int leagueApiId)
        {
            try
            {
                // 1. Limpiar base de datos primero
                await _dataService.CleanDatabaseAsync();

                // 2. Sincronizar liga
                var leagueCount = await _dataService.SyncLeaguesFromFootballDataAsync(_footballService);

                // 3. Sincronizar equipos
                var teamCount = await _dataService.SyncTeamsFromFootballDataAsync(_footballService, leagueApiId);

                // 4. Sincronizar partidos
                var matchCount = await _dataService.SyncHistoricalMatchesAsync(_footballService, leagueApiId, 2024);

                return Ok(new
                {
                    message = "✅ Datos sincronizados correctamente",
                    leagues = leagueCount,
                    teams = teamCount,
                    matches = matchCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error sincronizando datos: {ex.Message}");
            }
        }

        [HttpGet("valuebets/{leagueId}")]
        public async Task<ActionResult<List<ValueBet>>> GetValueBets(int leagueId, [FromQuery] double minValue = 0.1)
        {
            try
            {
                Console.WriteLine($"🔍 Searching value bets for league {leagueId}, minValue: {minValue}");

                var valueBets = await _valueBetService.FindValueBetsAsync(leagueId, minValue);

                if (!valueBets.Any())
                {
                    Console.WriteLine("ℹ️ No value bets found with current parameters");
                    return Ok(new
                    {
                        message = "No value bets found",
                        suggestion = "Try lowering the minValue parameter or sync more data"
                    });
                }

                Console.WriteLine($"✅ Found {valueBets.Count} value bets");
                return Ok(valueBets);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in value bets: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
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

        [HttpGet("odds/test")]
        public async Task<ActionResult> TestOddsApi()
        {
            try
            {
                // Probar directamente la API de odds
                var sports = await _oddsApiService.GetSportsAsync();
                var soccerOdds = await _oddsApiService.GetSoccerOddsAsync();

                return Ok(new
                {
                    message = "✅ Odds API connected successfully",
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
                return StatusCode(500, $"❌ Odds API test failed: {ex.Message}");
            }
        }

        [HttpGet("sync-get/{leagueApiId}")]
        public async Task<ActionResult> SyncDataGet(int leagueApiId)
        {
            try
            {
                // 1. Sincronizar liga
                await _dataService.SyncLeaguesFromFootballDataAsync(_footballService);

                // 2. Sincronizar equipos
                var teamCount = await _dataService.SyncTeamsFromFootballDataAsync(_footballService, leagueApiId);

                // 3. Sincronizar partidos
                var matchCount = await _dataService.SyncHistoricalMatchesAsync(_footballService, leagueApiId, 2024);

                return Ok(new
                {
                    message = "✅ Datos sincronizados correctamente",
                    teams = teamCount,
                    matches = matchCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error sincronizando datos: {ex.Message}");
            }
        }

        [HttpPost("clean-database")]
        public async Task<ActionResult> CleanDatabase()
        {
            try
            {
                // Método para limpiar la base de datos (debes implementarlo en DataService)
                await _dataService.CleanDatabaseAsync();
                return Ok(new { message = "✅ Base de datos limpiada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error limpiando base de datos: {ex.Message}");
            }
        }

        // Añade este endpoint temporal en PredictionsController.cs
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

                // Probar con valores extremos - ahora con todos los parámetros requeridos
                var prediction1 = predictionService.PredictMatch(
                    homeStrength: 90f, awayStrength: 50f,    // Fuerza de equipos
                    homeForm: 4f, awayForm: 1f,              // Forma reciente (últimos 5 partidos)
                    homeAttack: 2.5f, awayAttack: 1.2f,      // Fuerza ofensiva
                    homeDefense: 0.8f, awayDefense: 1.8f,    // Debilidad defensiva  
                    historicalDraws: 2f                      // Empates históricos
                );

                var prediction2 = predictionService.PredictMatch(
                    homeStrength: 50f, awayStrength: 90f,
                    homeForm: 1f, awayForm: 4f,
                    homeAttack: 1.2f, awayAttack: 2.5f,
                    homeDefense: 1.8f, awayDefense: 0.8f,
                    historicalDraws: 1f
                );

                var prediction3 = predictionService.PredictMatch(
                    homeStrength: 70f, awayStrength: 70f,
                    homeForm: 2f, awayForm: 2f,
                    homeAttack: 1.8f, awayAttack: 1.8f,
                    homeDefense: 1.2f, awayDefense: 1.2f,
                    historicalDraws: 3f
                );

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

                Console.WriteLine($"📊 Datos de entrenamiento: {trainingData.Count} partidos");

                if (trainingData.Count > 10)
                {
                    // Entrenar con una pequeña muestra primero
                    var sampleData = trainingData.Take(50).ToList();
                    var metrics = predictionService.TrainModel(sampleData, "SDCA");

                    // Hacer predicciones de prueba
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
                    Features = sample.Select(s => new {
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
                var footballService = new FootballDataService("f1e713f8666b42d8b29affe6c9ac478e");

                // Probar endpoints individuales
                var competitions = await footballService.GetCompetitionsAsync();
                var standings = await footballService.GetStandingsAsync(2014); // LaLiga
                var matches = await footballService.GetHistoricalMatchesAsync(2014, 2023);

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
                    Details = "¿La API key es correcta? ¿Hay conexión a internet?"
                });
            }
        }

        [HttpGet("debug/database")]
        public async Task<ActionResult> DebugDatabase()
        {
            try
            {
                var leagues = await _dataService.GetAllLeaguesAsync();
                var teams = await _dataService.GetAllTeamsAsync();
                var matches = await _dataService.GetMatchesByDateRangeAsync(
                    DateTime.Now.AddYears(-1), DateTime.Now.AddYears(1));

                return Ok(new
                {
                    LeaguesCount = leagues.Count(),
                    TeamsCount = teams.Count(),
                    MatchesCount = matches.Count(),
                    SampleLeagues = leagues.Take(3).Select(l => l.Name),
                    SampleTeams = teams.Take(5).Select(t => t.Name),
                    SampleMatches = matches.Take(3).Select(m => new {
                        m.Id,
                        m.HomeTeamId,
                        m.AwayTeamId,
                        m.MatchDate
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    Details = "Error accediendo a la base de datos"
                });
            }
        }

        [HttpGet("debug/matches")]
        public async Task<ActionResult> DebugMatchesSync()
        {
            try
            {
                var footballService = new FootballDataService("f1e713f8666b42d8b29affe6c9ac478e");

                // 1. Probar directamente la API de partidos
                var matchesResponse = await footballService.GetHistoricalMatchesAsync(2014, 2023);
                Console.WriteLine($"📊 Partidos obtenidos de API: {matchesResponse?.Matches?.Count ?? 0}");

                // 2. Intentar sincronizar solo partidos
                var syncCount = await _dataService.SyncHistoricalMatchesAsync(footballService, 2014, 2024);
                Console.WriteLine($"✅ Partidos sincronizados en BD: {syncCount}");

                // 3. Verificar de nuevo la BD
                var matchesInDb = await _dataService.GetMatchesByDateRangeAsync(
                    DateTime.Now.AddYears(-2), DateTime.Now.AddYears(1));

                return Ok(new
                {
                    ApiMatchesCount = matchesResponse?.Matches?.Count ?? 0,
                    SyncedMatchesCount = syncCount,
                    DatabaseMatchesCount = matchesInDb.Count(),
                    SampleApiMatches = matchesResponse?.Matches?.Take(3).Select(m => new {
                        HomeTeam = m.HomeTeam.Name,    // ← Cambiado de "Name" a "HomeTeam"
                        AwayTeam = m.AwayTeam.Name,    // ← Cambiado de "Name" a "AwayTeam"  
                        m.Date
                    }),
                    Message = "Revisa la consola para logs detallados"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en sync de partidos: {ex.Message}");
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    Details = ex.StackTrace
                });
            }
        }

    }
}