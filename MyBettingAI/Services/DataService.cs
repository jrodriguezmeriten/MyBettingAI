using Dapper;
using Microsoft.Data.Sqlite;
using MyBettingAI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace MyBettingAI.Services
{
    public class DataService : IDataService
    {
        private readonly string _connectionString;

        public DataService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // LEAGUES
        public async Task<int> InsertLeagueAsync(League league)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Leagues (Name, Country, ApiId) 
                            VALUES (@Name, @Country, @ApiId);
                            SELECT last_insert_rowid();";

                return await connection.ExecuteScalarAsync<int>(sql, league);
            }
        }

        public async Task<IEnumerable<League>> GetAllLeaguesAsync()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<League>("SELECT * FROM Leagues");
            }
        }

        // TEAMS
        public async Task<int> InsertTeamAsync(Team team)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Teams (Name, LeagueId, StrengthRating, ApiId) 
                            VALUES (@Name, @LeagueId, @StrengthRating, @ApiId);
                            SELECT last_insert_rowid();";

                return await connection.ExecuteScalarAsync<int>(sql, team);
            }
        }

        public async Task<IEnumerable<Team>> GetTeamsByLeagueAsync(int leagueId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<Team>(
                    "SELECT * FROM Teams WHERE LeagueId = @LeagueId",
                    new { LeagueId = leagueId });
            }
        }

        // MATCHES
        public async Task<int> InsertMatchAsync(Match match)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Matches 
                            (LeagueId, HomeTeamId, AwayTeamId, MatchDate, HomeScore, AwayScore, 
                             HomeExpectedGoals, AwayExpectedGoals, HomeShots, AwayShots, 
                             HomeShotsOnTarget, AwayShotsOnTarget, HomePossession, AwayPossession) 
                            VALUES 
                            (@LeagueId, @HomeTeamId, @AwayTeamId, @MatchDate, @HomeScore, @AwayScore, 
                             @HomeExpectedGoals, @AwayExpectedGoals, @HomeShots, @AwayShots, 
                             @HomeShotsOnTarget, @AwayShotsOnTarget, @HomePossession, @AwayPossession);
                            SELECT last_insert_rowid();";

                return await connection.ExecuteScalarAsync<int>(sql, match);
            }
        }

        public async Task<IEnumerable<Match>> GetMatchesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<Match>(
                    "SELECT * FROM Matches WHERE MatchDate BETWEEN @StartDate AND @EndDate",
                    new { StartDate = startDate, EndDate = endDate });
            }
        }
    }
}