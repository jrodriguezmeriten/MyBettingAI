using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyBettingAI.Services
{
    public class FootballDataService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.football-data.org/v4/";

        public FootballDataService(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", apiKey);
            Console.WriteLine($"✅ FootballDataService initialized with API key: {apiKey.Substring(0, 8)}..."); // Muestra solo los primeros 8 caracteres por seguridad
        }

        // Método genérico para hacer peticiones
        private async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(content);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calling {endpoint}: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Competition>> GetCompetitionsAsync()
        {
            var response = await GetAsync<CompetitionResponse>("competitions");
            return response?.Competitions ?? new List<Competition>();
        }

        public async Task<StandingResponse> GetStandingsAsync(int competitionId)
        {
            return await GetAsync<StandingResponse>($"competitions/{competitionId}/standings");
        }

        public async Task<MatchResponse> GetMatchesAsync(int competitionId)
        {
            return await GetAsync<MatchResponse>($"competitions/{competitionId}/matches");
        }

        public async Task<MatchResponse> GetHistoricalMatchesAsync(int competitionId, int season = 2023)
        {
            return await GetAsync<MatchResponse>($"competitions/{competitionId}/matches?season={season}");
        }
    }

    // Clases para la respuesta de la API
    public class CompetitionResponse
    {
        [JsonProperty("competitions")]
        public List<Competition> Competitions { get; set; }
    }

    public class Competition
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("area")]
        public Area Area { get; set; }

        [JsonProperty("currentSeason")]
        public CurrentSeason CurrentSeason { get; set; }
    }

    public class Area
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class CurrentSeason
    {
        [JsonProperty("currentMatchday")]
        public int CurrentMatchday { get; set; }
    }

    public class StandingResponse
    {
        [JsonProperty("standings")]
        public List<Standing> Standings { get; set; }
    }

    public class Standing
    {
        [JsonProperty("table")]
        public List<TeamStanding> Table { get; set; }
    }

    public class TeamStanding
    {
        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("team")]
        public TeamInfo Team { get; set; }

        [JsonProperty("playedGames")]
        public int PlayedGames { get; set; }

        [JsonProperty("won")]
        public int Won { get; set; }

        [JsonProperty("draw")]
        public int Draw { get; set; }

        [JsonProperty("lost")]
        public int Lost { get; set; }

        [JsonProperty("points")]
        public int Points { get; set; }
    }

    public class TeamInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("tla")]
        public string Tla { get; set; }

        [JsonProperty("crest")]
        public string Crest { get; set; }
    }

    public class MatchResponse
    {
        [JsonProperty("matches")]
        public List<MatchInfo> Matches { get; set; }
    }

    public class MatchInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("utcDate")]
        public DateTime Date { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("matchday")]
        public int Matchday { get; set; }

        [JsonProperty("homeTeam")]
        public TeamInfo HomeTeam { get; set; }

        [JsonProperty("awayTeam")]
        public TeamInfo AwayTeam { get; set; }

        [JsonProperty("score")]
        public MatchScore Score { get; set; }
    }

    public class MatchScore
    {
        [JsonProperty("fullTime")]
        public Score FullTime { get; set; }

        [JsonProperty("halfTime")]
        public Score HalfTime { get; set; }
    }

    public class Score
    {
        [JsonProperty("home")]
        public int? Home { get; set; }

        [JsonProperty("away")]
        public int? Away { get; set; }
    }


    }