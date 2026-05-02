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

    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Club { get; set; }

    [MaxLength(200)]
    public string? Association { get; set; }

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Phone { get; set; }

    public IReadOnlyCollection<Game> Games { get; set; } = [];

    public PlayerStatistics Statistics { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}