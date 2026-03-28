using System.ComponentModel.DataAnnotations;

namespace BilliardIQ.Mobile.Models;

public class Player
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public Level Level { get; set; } = Level.Intermidiate;

    public IReadOnlyCollection<Game> Games { get; set; } = [];

    public PlayerStatistics Statistics { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public override string ToString() => $"{Name} {LastName}";
}
