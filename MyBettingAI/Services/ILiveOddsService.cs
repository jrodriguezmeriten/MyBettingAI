// Services/LiveOddsService.cs
using Microsoft.Extensions.Options;
using MyBettingAI.Models;
using System.Net.Http.Json;

namespace MyBettingAI.Services;

public interface ILiveOddsService
{
    Task<List<LiveOdds>> GetLiveOddsAsync(string sport = "tennis");
}

public class LiveOddsService : ILiveOddsService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;

    public LiveOddsService(HttpClient httpClient, IOptions<ApiSettings> apiSettings)
    {
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
        // Configura la base URL y la API key (si la hay) para las llamadas
        _httpClient.BaseAddress = new Uri(_apiSettings.OddsApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiSettings.OddsApiKey);
    }

    public async Task<List<LiveOdds>> GetLiveOddsAsync(string sport = "tennis")
    {
        try
        {
            // Ejemplo de endpoint: /v4/sports/tennis/odds/?apiKey=xxx&regions=eu&markets=h2h
            var response = await _httpClient.GetFromJsonAsync<List<LiveOdds>>($"/v4/sports/{sport}/odds/?regions=eu&markets=h2h");
            return response ?? new List<LiveOdds>();
        }
        catch (HttpRequestException ex)
        {
            // Log the error (use ILogger in a real scenario)
            Console.WriteLine($"Error fetching live odds: {ex.Message}");
            return new List<LiveOdds>();
        }
    }
}

// appsettings.json configuration section
public class ApiSettings
{
    public string OddsApiBaseUrl { get; set; }
    public string OddsApiKey { get; set; }
}