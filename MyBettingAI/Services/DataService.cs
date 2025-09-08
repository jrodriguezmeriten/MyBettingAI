using Dapper;
using Microsoft.Data.Sqlite;
using MyBettingAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyBettingAI.Services
{
    /// <summary>
    /// Service for data access, synchronization and training data preparation.
    /// Servicio para acceso a datos, sincronización y preparación de datos de entrenamiento.
    /// </summary>
    public class DataService
    {
        private readonly string _connectionString;

        // Constructor receives connection string
        // Constructor que recibe la cadena de conexión
        public DataService(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region League Methods

        /// <summary>
        /// Sync leagues from Football-Data.org API.
        /// Sincroniza ligas desde la API de Football-Data.org.
        /// </summary>
        public async Task<int> SyncLeaguesFromFootballDataAsync(FootballDataService footballService)
        {
            try
            {
                var competitions = await footballService.GetCompetitionsAsync();
                int count = 0;

                foreach (var competition in competitions)
                {
                    var existingLeague = await GetLeagueByApiIdAsync(competition.Id);
                    if (existingLeague != null) continue;

                    var league = new League
                    {
                        Name = competition.Name,
                        Country = competition.Area.Name,
                        ApiId = competition.Id
                    };

                    await InsertLeagueAsync(league);
                    count++;
                }

                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing leagues: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> InsertLeagueAsync(League league)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var sql = @"INSERT INTO Leagues (Name, Country, ApiId) 
                        VALUES (@Name, @Country, @ApiId);
                        SELECT last_insert_rowid();";
            return await connection.ExecuteScalarAsync<int>(sql, league);
        }

        public async Task<League> GetLeagueByApiIdAsync(int apiId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<League>(
                "SELECT * FROM Leagues WHERE ApiId = @ApiId", new { ApiId = apiId });
        }

        public async Task<IEnumerable<League>> GetAllLeaguesAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<League>("SELECT * FROM Leagues");
        }

        #endregion

        #region Team Methods

        public async Task<IEnumerable<Team>> GetTeamsByLeagueAsync(int leagueId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<Team>(
                "SELECT * FROM Teams WHERE LeagueId = @LeagueId",
                new { LeagueId = leagueId });
        }

        public async Task<int> SyncTeamsFromFootballDataAsync(FootballDataService footballService, int leagueApiId)
        {
            var league = await GetLeagueByApiIdAsync(leagueApiId);
            if (league == null) return 0;

            var standingResponse = await footballService.GetStandingsAsync(leagueApiId);
            int count = 0;

            if (standingResponse?.Standings?[0]?.Table != null)
            {
                foreach (var teamStanding in standingResponse.Standings[0].Table)
                {
                    var existingTeam = await GetTeamByApiIdAsync(teamStanding.Team.Id);
                    if (existingTeam != null) continue;

                    var team = new Team
                    {
                        Name = teamStanding.Team.Name,
                        LeagueId = league.Id,
                        ApiId = teamStanding.Team.Id,
                        StrengthRating = CalculateStrengthRating(teamStanding)
                    };

                    await InsertTeamAsync(team);
                    count++;
                }
            }

            return count;
        }

        public async Task<int> InsertTeamAsync(Team team)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var sql = @"INSERT INTO Teams (Name, LeagueId, StrengthRating, ApiId) 
                        VALUES (@Name, @LeagueId, @StrengthRating, @ApiId);
                        SELECT last_insert_rowid();";
            return await connection.ExecuteScalarAsync<int>(sql, team);
        }

        public async Task<Team> GetTeamByApiIdAsync(int apiId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<Team>(
                "SELECT * FROM Teams WHERE ApiId = @ApiId", new { ApiId = apiId });
        }

        public async Task<IEnumerable<Team>> GetAllTeamsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<Team>("SELECT Id, Name, LeagueId, StrengthRating, ApiId FROM Teams");
        }

        // Cached version to improve performance
        private List<Team> _cachedTeams;
        private DateTime _lastCacheUpdate;

        public async Task<IEnumerable<Team>> GetAllTeamsCachedAsync()
        {
            if (_cachedTeams == null || DateTime.Now - _lastCacheUpdate > TimeSpan.FromMinutes(5))
            {
                _cachedTeams = (await GetAllTeamsAsync()).ToList();
                _lastCacheUpdate = DateTime.Now;
            }
            return _cachedTeams;
        }

        public async Task<Team> FindTeamByNameAsync(string teamName)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<Team>(
                "SELECT * FROM Teams WHERE Name LIKE @TeamName OR @TeamName LIKE '%' || Name || '%'",
                new { TeamName = $"%{teamName}%" });
        }

        private float CalculateStrengthRating(TeamStanding standing)
        {
            if (standing.PlayedGames == 0) return 50.0f;

            float winRate = (float)standing.Won / standing.PlayedGames;
            float pointsPerGame = (float)standing.Points / standing.PlayedGames;

            return (winRate * 50) + (pointsPerGame * 2);
        }

        #endregion

        #region Match Methods

        public async Task<int> SyncHistoricalMatchesAsync(FootballDataService footballService, int leagueApiId, int season = 2024)
        {
            var league = await GetLeagueByApiIdAsync(leagueApiId);
            if (league == null) return 0;

            var matchesResponse = await footballService.GetHistoricalMatchesAsync(leagueApiId, season);
            int count = 0;

            if (matchesResponse?.Matches != null)
            {
                foreach (var matchInfo in matchesResponse.Matches)
                {
                    var homeTeam = await GetTeamByApiIdAsync(matchInfo.HomeTeam.Id);
                    var awayTeam = await GetTeamByApiIdAsync(matchInfo.AwayTeam.Id);
                    if (homeTeam == null || awayTeam == null) continue;

                    var match = new Match
                    {
                        LeagueId = league.Id,
                        HomeTeamId = homeTeam.Id,
                        AwayTeamId = awayTeam.Id,
                        MatchDate = matchInfo.Date,
                        HomeScore = matchInfo.Score?.FullTime?.Home,
                        AwayScore = matchInfo.Score?.FullTime?.Away
                    };

                    await InsertMatchAsync(match);
                    count++;
                }
            }

            return count;
        }

        public async Task<int> InsertMatchAsync(Match match)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var sql = @"INSERT INTO Matches 
                        (LeagueId, HomeTeamId, AwayTeamId, MatchDate, HomeScore, AwayScore) 
                        VALUES (@LeagueId, @HomeTeamId, @AwayTeamId, @MatchDate, @HomeScore, @AwayScore);
                        SELECT last_insert_rowid();";
            return await connection.ExecuteScalarAsync<int>(sql, match);
        }

        public async Task<List<TrainingFeatures>> GetTrainingDataAsync(int leagueId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
SELECT 
    COALESCE(ht.StrengthRating, 50.0) AS HomeTeamStrength,
    COALESCE(at.StrengthRating, 50.0) AS AwayTeamStrength,
    -- Recent form (last 5 matches)
    (SELECT COUNT(*) FROM Matches m2 
     WHERE m2.HomeTeamId = m.HomeTeamId 
     AND m2.MatchDate < m.MatchDate 
     AND m2.HomeScore > m2.AwayScore
     ORDER BY m2.MatchDate DESC LIMIT 5) AS HomeForm,
    (SELECT COUNT(*) FROM Matches m2 
     WHERE m2.AwayTeamId = m.AwayTeamId 
     AND m2.MatchDate < m.MatchDate 
     AND m2.AwayScore > m2.HomeScore
     ORDER BY m2.MatchDate DESC LIMIT 5) AS AwayForm,
    -- Attack/Defense strength
    (SELECT AVG(HomeScore) FROM Matches WHERE HomeTeamId = m.HomeTeamId AND MatchDate < m.MatchDate) AS HomeAttackStrength,
    (SELECT AVG(AwayScore) FROM Matches WHERE AwayTeamId = m.AwayTeamId AND MatchDate < m.MatchDate) AS AwayAttackStrength,
    (SELECT AVG(AwayScore) FROM Matches WHERE HomeTeamId = m.HomeTeamId AND MatchDate < m.MatchDate) AS HomeDefenseWeakness,
    (SELECT AVG(HomeScore) FROM Matches WHERE AwayTeamId = m.AwayTeamId AND MatchDate < m.MatchDate) AS AwayDefenseWeakness,
    -- Historical draws
    (SELECT COUNT(*) FROM Matches m2 
     WHERE ((m2.HomeTeamId = m.HomeTeamId AND m2.AwayTeamId = m.AwayTeamId)
         OR (m2.HomeTeamId = m.AwayTeamId AND m2.AwayTeamId = m.HomeTeamId))
     AND m2.MatchDate < m.MatchDate
     AND m2.HomeScore = m2.AwayScore) AS HistoricalDraws,
    -- Label
    CASE 
        WHEN m.HomeScore > m.AwayScore THEN 'HomeWin'
        WHEN m.HomeScore < m.AwayScore THEN 'AwayWin' 
        ELSE 'Draw'
    END AS Label
FROM Matches m
JOIN Teams ht ON m.HomeTeamId = ht.Id
JOIN Teams at ON m.AwayTeamId = at.Id
WHERE m.LeagueId = @LeagueId 
AND m.HomeScore IS NOT NULL
AND m.AwayScore IS NOT NULL
ORDER BY m.MatchDate DESC
LIMIT 1000";

            var matches = await connection.QueryAsync<TrainingFeatures>(sql, new { LeagueId = leagueId });
            return matches.ToList();
        }

        public async Task<IEnumerable<Match>> GetMatchesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<Match>(
                "SELECT * FROM Matches WHERE MatchDate BETWEEN @StartDate AND @EndDate",
                new { StartDate = startDate, EndDate = endDate });
        }

        #endregion

        public async Task CleanDatabaseAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
DELETE FROM Bets;
DELETE FROM Predictions;
DELETE FROM Odds;
DELETE FROM Matches;
DELETE FROM Teams;
DELETE FROM Leagues;";
            await command.ExecuteNonQueryAsync();
            Console.WriteLine("Database cleaned successfully / Base de datos limpiada completamente");
        }
    }

    // Training features for ML model
    public class TrainingFeatures
    {
        public float HomeTeamStrength { get; set; }
        public float AwayTeamStrength { get; set; }
        public float HomeForm { get; set; }
        public float AwayForm { get; set; }
        public float HomeAttackStrength { get; set; }
        public float AwayAttackStrength { get; set; }
        public float HomeDefenseWeakness { get; set; }
        public float AwayDefenseWeakness { get; set; }
        public float HistoricalDraws { get; set; }
        public string Label { get; set; }
    }
}
