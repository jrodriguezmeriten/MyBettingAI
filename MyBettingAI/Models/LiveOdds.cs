// Models/LiveOdds.cs
namespace MyBettingAI.Models;

public class LiveOdds
{
    public int Id { get; set; }
    public string Sport { get; set; } // e.g., "tennis"
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public string MatchId { get; set; } // ID único del partido en la API
    public string Bookmaker { get; set; } // e.g., "bet365"
    public DateTime LastUpdated { get; set; }
    public List<OddsMarket> Markets { get; set; }
}

public class OddsMarket
{
    public string MarketKey { get; set; } // e.g., "h2h" (head-to-head)
    public DateTime LastUpdated { get; set; }
    public List<Outcome> Outcomes { get; set; }
}

public class Outcome
{
    public string Name { get; set; } // Name of the player/team
    public decimal Price { get; set; } // The odds (e.g., 1.90)
    public string Point { get; set; } // For spreads/totals
}