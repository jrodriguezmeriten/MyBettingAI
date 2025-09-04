namespace MyBettingAI.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int LeagueId { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public DateTime MatchDate { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public double? HomeExpectedGoals { get; set; }
        public double? AwayExpectedGoals { get; set; }
        public int? HomeShots { get; set; }
        public int? AwayShots { get; set; }
        public int? HomeShotsOnTarget { get; set; }
        public int? AwayShotsOnTarget { get; set; }
        public int? HomePossession { get; set; }
        public int? AwayPossession { get; set; }
    }
}