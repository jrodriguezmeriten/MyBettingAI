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

        // Inyectar FootballDataService en el constructor
        public PredictionsController(ValueBetService valueBetService, DataService dataService, FootballDataService footballService)
        {
            _valueBetService = valueBetService;
            _dataService = dataService;
            _footballService = footballService;
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

        [HttpGet("valuebets/{leagueId}")]
        public async Task<ActionResult<List<ValueBet>>> GetValueBets(int leagueId, [FromQuery] double minValue = 0.1)
        {
            try
            {
                var valueBets = await _valueBetService.FindValueBetsAsync(leagueId, minValue);
                return Ok(valueBets);
            }
            catch (Exception ex)
            {
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
    }
}