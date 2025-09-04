using Microsoft.Data.Sqlite;
using System.IO;

namespace Data
{
    public class DatabaseContext
    {
        private readonly string _connectionString;

        public DatabaseContext()
        {
            var databasePath = Path.Combine(Directory.GetCurrentDirectory(), "betting_data.db");
            _connectionString = $"Data Source={databasePath}";
        }

        public string GetConnectionString() => _connectionString;

        public void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // SCRIPT SQL COMPLETO para crear todas las tablas
                var command = connection.CreateCommand();
                command.CommandText = @"
                -- 1. Tabla de Ligas
                CREATE TABLE IF NOT EXISTS Leagues (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Country TEXT NOT NULL,
                    ApiId INTEGER NULL
                );

                -- 2. Tabla de Equipos
                CREATE TABLE IF NOT EXISTS Teams (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    LeagueId INTEGER NOT NULL,
                    StrengthRating REAL DEFAULT 0,
                    ApiId INTEGER NULL,
                    FOREIGN KEY (LeagueId) REFERENCES Leagues (Id)
                );

                -- 3. Tabla de Partidos
                CREATE TABLE IF NOT EXISTS Matches (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LeagueId INTEGER NOT NULL,
                    HomeTeamId INTEGER NOT NULL,
                    AwayTeamId INTEGER NOT NULL,
                    MatchDate DATE NOT NULL,
                    HomeScore INTEGER NULL,
                    AwayScore INTEGER NULL,
                    HomeExpectedGoals REAL NULL,
                    AwayExpectedGoals REAL NULL,
                    HomeShots INTEGER NULL,
                    AwayShots INTEGER NULL,
                    HomeShotsOnTarget INTEGER NULL,
                    AwayShotsOnTarget INTEGER NULL,
                    HomePossession INTEGER NULL,
                    AwayPossession INTEGER NULL,
                    FOREIGN KEY (LeagueId) REFERENCES Leagues (Id),
                    FOREIGN KEY (HomeTeamId) REFERENCES Teams (Id),
                    FOREIGN KEY (AwayTeamId) REFERENCES Teams (Id)
                );

                -- 4. Tabla de Cuotas
                CREATE TABLE IF NOT EXISTS Odds (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MatchId INTEGER NOT NULL,
                    Bookmaker TEXT NOT NULL,
                    MarketType TEXT NOT NULL,
                    LastUpdated DATETIME NOT NULL,
                    HomeWinOdds REAL NULL,
                    DrawOdds REAL NULL,
                    AwayWinOdds REAL NULL,
                    OverOdds REAL NULL,
                    UnderOdds REAL NULL,
                    FOREIGN KEY (MatchId) REFERENCES Matches (Id),
                    UNIQUE(MatchId, Bookmaker, MarketType)
                );

                -- 5. Tabla de Predicciones
                CREATE TABLE IF NOT EXISTS Predictions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MatchId INTEGER NOT NULL,
                    CalculationDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    HomeWinProbability REAL NOT NULL,
                    DrawProbability REAL NOT NULL,
                    AwayWinProbability REAL NOT NULL,
                    RecommendedBet TEXT NULL,
                    CalculatedValue REAL NOT NULL,
                    KellyFraction REAL NULL,
                    FOREIGN KEY (MatchId) REFERENCES Matches (Id)
                );

                -- 6. Tabla de Apuestas
                CREATE TABLE IF NOT EXISTS Bets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PredictionId INTEGER NOT NULL,
                    BetType TEXT NOT NULL,
                    Odds REAL NOT NULL,
                    Stake REAL NULL,
                    Result TEXT NULL,
                    ProfitLoss REAL NULL,
                    FOREIGN KEY (PredictionId) REFERENCES Predictions (Id)
                );

                -- Crear Índices para mejorar el rendimiento
                CREATE INDEX IF NOT EXISTS idx_matches_date ON Matches(MatchDate);
                CREATE INDEX IF NOT EXISTS idx_matches_league ON Matches(LeagueId);
                CREATE INDEX IF NOT EXISTS idx_odds_match ON Odds(MatchId);
                CREATE INDEX IF NOT EXISTS idx_odds_bookmaker ON Odds(Bookmaker);
                CREATE INDEX IF NOT EXISTS idx_predictions_match ON Predictions(MatchId);
                ";

                command.ExecuteNonQuery(); // Ejecuta el script completo
            }
        }
    }
}