using MyBettingAI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyBettingAI.Services
{
    public interface IDataService
    {
        // Operaciones básicas para cada entidad
        Task<int> InsertLeagueAsync(League league);
        Task<IEnumerable<League>> GetAllLeaguesAsync();

        Task<int> InsertTeamAsync(Team team);
        Task<IEnumerable<Team>> GetTeamsByLeagueAsync(int leagueId);

        Task<int> InsertMatchAsync(Match match);
        Task<IEnumerable<Match>> GetMatchesByDateRangeAsync(DateTime startDate, DateTime endDate);

        // Más métodos según necesites...
    }
}