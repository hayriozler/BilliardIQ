using BilliardIQ.Mobile.Utilities;
using System.ComponentModel.DataAnnotations;

namespace BilliardIQ.Mobile.Models;


public class Game
{
    [Key]
    public int Id { get; set; }
    public DateTime Date { get; set; }

    [MaxLength(100)]
    [DbFieldName("OpponentName")]
    public string? Opponent { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }
    public string? Ball { get; set; }
    public int PlayerScore { get; set; }
    public int OpponentScore { get; set; }
    public int HighestRun { get; set; }
    public double Innings { get; set; }
    [MaxLength(300)]
    public string? Notes { get; set; }    
    public byte[]? ScoreboardThumbnail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
}
