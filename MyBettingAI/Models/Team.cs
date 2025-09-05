namespace MyBettingAI.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int LeagueId { get; set; }
        public float StrengthRating { get; set; }
        public int? ApiId { get; set; }
    }
}