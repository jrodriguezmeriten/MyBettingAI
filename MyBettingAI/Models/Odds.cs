namespace MyBettingAI.Models
{
    public class Odds
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public string Bookmaker { get; set; } = "";
        public string MarketType { get; set; } = "";
        public DateTime LastUpdated { get; set; }
        public double? HomeWinOdds { get; set; }
        public double? DrawOdds { get; set; }
        public double? AwayWinOdds { get; set; }
        public double? OverOdds { get; set; }
        public double? UnderOdds { get; set; }
    }
}
