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

        public OddsApiService(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        public async Task<List<OddsApiSport>> GetSportsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("sports?apiKey=TU_API_KEY");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<OddsApiSport>>(content);
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
                var endpoint = $"sports/{sportKey}/odds?regions={regions}&markets={markets}";
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<OddsApiOdds>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting odds: {ex.Message}");
                return new List<OddsApiOdds>();
            }
        }

        public async Task<List<OddsApiOdds>> GetSoccerOddsAsync()
        {
            // Deportes de fútbol disponibles
            var soccerSports = new[] { "soccer_epl", "soccer_laliga", "soccer_serie_a", "soccer_bundesliga", "soccer_ligue1" };
            var allOdds = new List<OddsApiOdds>();

            foreach (var sport in soccerSports)
            {
                var odds = await GetOddsAsync(sport);
                allOdds.AddRange(odds);
            }

            return allOdds;
        }
    }

    // Clases para deserializar la respuesta de la API
    public class OddsApiSport
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
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