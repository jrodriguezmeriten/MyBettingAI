namespace MyBettingAI.Models
{
    public class Bet
    {
        public int Id { get; set; }
        public int PredictionId { get; set; }
        public string BetType { get; set; } = "";
        public double Odds { get; set; }
        public double? Stake { get; set; }
        public string? Result { get; set; }
        public double? ProfitLoss { get; set; }
    }
}