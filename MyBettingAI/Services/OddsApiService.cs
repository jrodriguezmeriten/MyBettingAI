using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyBettingAI.Services
{
    public class OddsApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.the-odds-api.com/v4/";
        private readonly string _apiKey;

        public OddsApiService(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _apiKey = apiKey;

            Console.WriteLine($"✅ OddsApiService initialized with key: {apiKey.Substring(0, 8)}...");
        }

        public async Task<List<OddsApiSport>> GetSportsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"sports?apiKey={_apiKey}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var sports = JsonConvert.DeserializeObject<List<OddsApiSport>>(content);

                Console.WriteLine($"✅ Found {sports?.Count ?? 0} sports");
                return sports ?? new List<OddsApiSport>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting sports: {ex.Message}");
                return new List<OddsApiSport>();
            }
        }

        public async Task<List<OddsApiOdds>> GetOddsAsync(string sportKey = "soccer_epl", string regions = "eu", string markets = "h2h")
        {
            try
            {
                var endpoint = $"sports/{sportKey}/odds?regions={regions}&markets={markets}&apiKey={_apiKey}";
                Console.WriteLine($"🔍 Fetching odds from: {endpoint}");

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var odds = JsonConvert.DeserializeObject<List<OddsApiOdds>>(content);

                Console.WriteLine($"✅ Found {odds?.Count ?? 0} odds for {sportKey}");
                return odds ?? new List<OddsApiOdds>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting odds for {sportKey}: {ex.Message}");
                return new List<OddsApiOdds>();
            }
        }

        public async Task<List<OddsApiOdds>> GetSoccerOddsAsync()
        {
            var allOdds = new List<OddsApiOdds>();
            var soccerSports = new[]
            {
                "soccer_epl", "soccer_spain_la_liga", "soccer_italy_serie_a",
                "soccer_germany_bundesliga", "soccer_france_ligue_one"
            };

            foreach (var sport in soccerSports)
            {
                try
                {
                    var odds = await GetOddsAsync(sport);
                    if (odds != null && odds.Count > 0)
                    {
                        allOdds.AddRange(odds);
                        Console.WriteLine($"✅ Added {odds.Count} matches from {sport}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Skipping {sport}: {ex.Message}");
                }

                // Pequeña pausa para no saturar la API
                await Task.Delay(100);
            }

            Console.WriteLine($"🎯 Total soccer odds found: {allOdds.Count}");
            return allOdds;
        }
    }

    // Clases para deserializar (las mismas que antes)
    public class OddsApiSport
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class OddsApiOdds
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sport_key")]
        public string SportKey { get; set; }

        [JsonProperty("sport_title")]
        public string SportTitle { get; set; }

        [JsonProperty("commence_time")]
        public DateTime CommenceTime { get; set; }

        [JsonProperty("home_team")]
        public string HomeTeam { get; set; }

        [JsonProperty("away_team")]
        public string AwayTeam { get; set; }

        [JsonProperty("bookmakers")]
        public List<Bookmaker> Bookmakers { get; set; }
    }

    public class Bookmaker
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("markets")]
        public List<Market> Markets { get; set; }
    }

    public class Market
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("outcomes")]
        public List<Outcome> Outcomes { get; set; }
    }

    public class Outcome
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}