using Microsoft.AspNetCore.Mvc;
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
                var matchCount = await _dataService.SyncHistoricalMatchesAsync(_footballService, leagueApiId, 2023);

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
                var matchCount = await _dataService.SyncHistoricalMatchesAsync(_footballService, leagueApiId, 2023);

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
    }
}