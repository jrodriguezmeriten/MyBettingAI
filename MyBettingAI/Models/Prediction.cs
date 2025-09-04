namespace MyBettingAI.Models
{
    public class Prediction
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public DateTime CalculationDate { get; set; }
        public double HomeWinProbability { get; set; }
        public double DrawProbability { get; set; }
        public double AwayWinProbability { get; set; }
        public string? RecommendedBet { get; set; }
        public double CalculatedValue { get; set; }
        public double? KellyFraction { get; set; }
    }
}