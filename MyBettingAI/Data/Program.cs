// Program.cs
using Data;
using MyBettingAI.Models; // ← Añade este using

Console.WriteLine("🔄 Initializing database...");
var context = new DatabaseContext();
context.InitializeDatabase();
Console.WriteLine("✅ Database ready!");

// Ejemplo de cómo crear un objeto (opcional, para probar)
var league = new League
{
    Name = "LaLiga Santander",
    Country = "Spain"
};

Console.WriteLine($"Liga creada: {league.Name}");
Console.WriteLine("🚀 MyBettingAI is running!");