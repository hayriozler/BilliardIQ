namespace Zeymera.BillardIQ.WebApp.Models;

public class Club
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Team> Teams { get; set; } = [];

    public List<Player> Players { get; set; } = [];
}
