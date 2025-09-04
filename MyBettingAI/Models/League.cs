namespace MyBettingAI.Models
{
    public class League
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public int? ApiId { get; set; }
    }
}
