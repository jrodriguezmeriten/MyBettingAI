using Dapper;
using Microsoft.Data.Sqlite;
using MyBettingAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyBettingAI.Services
{
    public class DataService
    {
        private readonly string _connectionString;

        // Constructor que recibe la connection string
        public DataService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // MÉTODO 1: Sincronizar ligas desde Football-Data.org
        public async Task<int> SyncLeaguesFromFootballDataAsync(FootballDataService footballService)
        {
            try
            {
                var competitions = await footballService.GetCompetitionsAsync();
                int count = 0;

                foreach (var competition in competitions)
                {
                    // Verificar si la liga ya existe por ApiId
                    var existingLeague = await GetLeagueByApiIdAsync(competition.Id);
                    if (existingLeague != null)
                    {
                        Console.WriteLine($"⚠️ Liga ya existe: {competition.Name}");
                        continue; // Saltar si ya existe
                    }

                    var league = new League
                    {
                        Name = competition.Name,
                        Country = competition.Area.Name,
                        ApiId = competition.Id
                    };

                    await InsertLeagueAsync(league);
                    count++;
                    Console.WriteLine($"✅ Liga sincronizada: {league.Name}");
                }

                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sincronizando ligas: {ex.Message}");
                return 0;
            }
        }

        // MÉTODO 2: Insertar liga
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

        // MÉTODO 3: Buscar liga por ApiId
        public async Task<League> GetLeagueByApiIdAsync(int apiId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<League>(
                    "SELECT * FROM Leagues WHERE ApiId = @ApiId",
                    new { ApiId = apiId });
            }
        }

        // MÉTODO 4: Obtener todas las ligas
        public async Task<IEnumerable<League>> GetAllLeaguesAsync()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<League>("SELECT * FROM Leagues");
            }
        }

        // MÉTODO 5: Obtener equipos por liga
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

        // MÉTODO 6: Sincronizar equipos
        public async Task<int> SyncTeamsFromFootballDataAsync(FootballDataService footballService, int leagueApiId)
        {
            var league = await GetLeagueByApiIdAsync(leagueApiId);
            if (league == null)
            {
                Console.WriteLine($"❌ No se encontró la liga con ApiId {leagueApiId}");
                return 0;
            }

            var standingResponse = await footballService.GetStandingsAsync(leagueApiId);
            int count = 0;

            if (standingResponse?.Standings?[0]?.Table != null)
            {
                foreach (var teamStanding in standingResponse.Standings[0].Table)
                {
                    // VERIFICAR SI EL EQUIPO YA EXISTE antes de insertar
                    var existingTeam = await GetTeamByApiIdAsync(teamStanding.Team.Id);

                    if (existingTeam != null)
                    {
                        Console.WriteLine($"⚠️ Equipo ya existe: {teamStanding.Team.Name}");
                        continue; // Saltar si ya existe
                    }

                    var team = new Team
                    {
                        Name = teamStanding.Team.Name,
                        LeagueId = league.Id,
                        ApiId = teamStanding.Team.Id,
                        StrengthRating = CalculateStrengthRating(teamStanding)
                    };

                    await InsertTeamAsync(team);
                    count++;
                    Console.WriteLine($"✅ Equipo sincronizado: {team.Name} (Liga ID: {league.Id})");
                }
            }

            return count;
        }

        // MÉTODO 7: Insertar equipo
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

        // MÉTODO 8: Buscar equipo por ApiId
        public async Task<Team> GetTeamByApiIdAsync(int apiId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<Team>(
                    "SELECT * FROM Teams WHERE ApiId = @ApiId",
                    new { ApiId = apiId });
            }
        }

        public async Task<IEnumerable<Team>> GetAllTeamsAsync()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<Team>("SELECT Id, Name, LeagueId, StrengthRating, ApiId FROM Teams");
            }
        }

        // Y añade este método para caching
        private List<Team> _cachedTeams;
        private DateTime _lastCacheUpdate;

        public async Task<IEnumerable<Team>> GetAllTeamsCachedAsync()
        {
            // Cache por 5 minutos para mejorar performance
            if (_cachedTeams == null || DateTime.Now - _lastCacheUpdate > TimeSpan.FromMinutes(5))
            {
                _cachedTeams = (await GetAllTeamsAsync()).ToList();
                _lastCacheUpdate = DateTime.Now;
            }

            return _cachedTeams;
        }

        public async Task<Team> FindTeamByNameAsync(string teamName)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Búsqueda flexible por nombre (case insensitive y parcial)
                return await connection.QueryFirstOrDefaultAsync<Team>(
                    "SELECT * FROM Teams WHERE Name LIKE @TeamName OR @TeamName LIKE '%' || Name || '%'",
                    new { TeamName = $"%{teamName}%" });
            }
        }

        // MÉTODO 9: Calcular rating de equipo
        private float CalculateStrengthRating(TeamStanding standing)
        {
            if (standing.PlayedGames == 0) return 50.0F;

            float winRate = (float)standing.Won / standing.PlayedGames;
            float pointsPerGame = (float)standing.Points / standing.PlayedGames;

            return (winRate * 50) + (pointsPerGame * 2);
        }

        // MÉTODO 10: Sincronizar partidos históricos
        public async Task<int> SyncHistoricalMatchesAsync(FootballDataService footballService, int leagueApiId, int season = 2024)
        {
            var league = await GetLeagueByApiIdAsync(leagueApiId);
            if (league == null)
            {
                Console.WriteLine($"❌ No se encontró la liga con ApiId {leagueApiId}");
                return 0;
            }

            var matchesResponse = await footballService.GetHistoricalMatchesAsync(leagueApiId, season);
            int count = 0;

            if (matchesResponse?.Matches != null)
            {
                foreach (var matchInfo in matchesResponse.Matches)
                {
                    var homeTeam = await GetTeamByApiIdAsync(matchInfo.HomeTeam.Id);
                    var awayTeam = await GetTeamByApiIdAsync(matchInfo.AwayTeam.Id);

                    if (homeTeam == null || awayTeam == null)
                    {
                        Console.WriteLine($"⚠️ Saltando partido {matchInfo.HomeTeam.Name} vs {matchInfo.AwayTeam.Name} - equipos no encontrados");
                        continue;
                    }

                    var match = new Match
                    {
                        LeagueId = league.Id,
                        HomeTeamId = homeTeam.Id,
                        AwayTeamId = awayTeam.Id,
                        MatchDate = matchInfo.Date,
                        HomeScore = matchInfo.Score?.FullTime?.Home,
                        AwayScore = matchInfo.Score?.FullTime?.Away,
                        HomeExpectedGoals = null,
                        AwayExpectedGoals = null,
                        HomeShots = null,
                        AwayShots = null,
                        HomeShotsOnTarget = null,
                        AwayShotsOnTarget = null,
                        HomePossession = null,
                        AwayPossession = null
                    };

                    await InsertMatchAsync(match);
                    count++;

                    if (count % 10 == 0)
                        Console.WriteLine($"✅ {count} partidos sincronizados...");
                }
            }

            return count;
        }

        // MÉTODO 11: Insertar partido
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

        // MÉTODO 12: Obtener datos de entrenamiento (IMPORTANTE - este es el método que faltaba)
        public async Task<List<TrainingFeatures>> GetTrainingDataAsync(int leagueId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"
        SELECT 
            -- Features básicas
            COALESCE(ht.StrengthRating, 50.0) as HomeTeamStrength,
            COALESCE(at.StrengthRating, 50.0) as AwayTeamStrength,
    
            -- Forma reciente (últimos 5 partidos)
            (SELECT COUNT(*) FROM Matches m2 
             WHERE m2.HomeTeamId = m.HomeTeamId 
             AND m2.MatchDate < m.MatchDate 
             AND m2.HomeScore > m2.AwayScore
             ORDER BY m2.MatchDate DESC LIMIT 5) as HomeForm,  -- ← Nombre correcto
    
            (SELECT COUNT(*) FROM Matches m2 
             WHERE m2.AwayTeamId = m.AwayTeamId 
             AND m2.MatchDate < m.MatchDate 
             AND m2.AwayScore > m2.HomeScore
             ORDER BY m2.MatchDate DESC LIMIT 5) as AwayForm,  -- ← Nombre correcto
            
            -- 2. Potencia ofensiva/defensiva
            (SELECT AVG(HomeScore) FROM Matches 
             WHERE HomeTeamId = m.HomeTeamId 
             AND MatchDate < m.MatchDate) as HomeAttackStrength,
             
            (SELECT AVG(AwayScore) FROM Matches 
             WHERE AwayTeamId = m.AwayTeamId 
             AND MatchDate < m.MatchDate) as AwayAttackStrength,
             
            (SELECT AVG(AwayScore) FROM Matches 
             WHERE HomeTeamId = m.HomeTeamId 
             AND MatchDate < m.MatchDate) as HomeDefenseWeakness,
             
            (SELECT AVG(HomeScore) FROM Matches 
             WHERE AwayTeamId = m.AwayTeamId 
             AND MatchDate < m.MatchDate) as AwayDefenseWeakness,
            
            -- 3. Historial entre equipos
            (SELECT COUNT(*) FROM Matches m2 
             WHERE ((m2.HomeTeamId = m.HomeTeamId AND m2.AwayTeamId = m.AwayTeamId)
                 OR (m2.HomeTeamId = m.AwayTeamId AND m2.AwayTeamId = m.HomeTeamId))
             AND m2.MatchDate < m.MatchDate
             AND m2.HomeScore = m2.AwayScore) as HistoricalDraws,
            
            -- Label (resultado)
            CASE 
                WHEN m.HomeScore > m.AwayScore THEN 'HomeWin'
                WHEN m.HomeScore < m.AwayScore THEN 'AwayWin' 
                ELSE 'Draw'
            END as Label
            
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
        }

        // MÉTODO 13: Obtener partidos por rango de fechas
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

        public async Task CleanDatabaseAsync()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = @"
            DELETE FROM Bets;
            DELETE FROM Predictions;
            DELETE FROM Odds;
            DELETE FROM Matches;
            DELETE FROM Teams;
            DELETE FROM Leagues;
        ";
                await command.ExecuteNonQueryAsync();
            }
            Console.WriteLine("✅ Base de datos limpiada completamente");
        }
    }

    // CLASE para los datos de entrenamiento (debe estar dentro del namespace)
    public class TrainingFeatures
    {
        // Features básicas
        public float HomeTeamStrength { get; set; }
        public float AwayTeamStrength { get; set; }

        // Features de forma reciente
        public float HomeForm { get; set; }          // ← Nombre correcto
        public float AwayForm { get; set; }          // ← Nombre correcto

        // Features ofensivas/defensivas  
        public float HomeAttackStrength { get; set; }
        public float AwayAttackStrength { get; set; }
        public float HomeDefenseWeakness { get; set; }
        public float AwayDefenseWeakness { get; set; }

        // Feature histórica
        public float HistoricalDraws { get; set; }

        // Label
        public string Label { get; set; }
    }
}