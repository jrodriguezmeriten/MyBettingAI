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

        public PredictionsController(ValueBetService valueBetService, DataService dataService)
        {
            _valueBetService = valueBetService;
            _dataService = dataService;
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

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "✅ API is working!", timestamp = DateTime.UtcNow });
        }
    }
}