using Microsoft.EntityFrameworkCore;
using Scoreboard.Client.Models;

namespace Scoreboard.Client.Services;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<ScoreboardState> ScoreboardStateSet { get; set; }
    public DbSet<Player> PlayerSet { get; set; }
    public DbSet<MatchResult> MatchResultSet { get; set; }
    public DbSet<Team> TeamSet { get; set; }
    public DbSet<Club> ClubSet { get; set; }
    public DbSet<ScoreEvent> ScoreEventSet { get; set; }
    public DbSet<Setting> SettingsSet { get; set; }
    public DbSet<MatchScoreStat> MatchScoreStatSet { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<ScoreboardState>().HasKey(p => p.Id);
        modelBuilder.Entity<ScoreboardState>().Property(e => e.Id).UseAutoincrement();
        modelBuilder.Entity<ScoreboardState>().ToTable("scoreboard_state");

        modelBuilder.Entity<Player>().HasKey(p => p.Id);
        modelBuilder.Entity<Player>().Property(p => p.Id).ValueGeneratedNever(); // the server's player id
        modelBuilder.Entity<Player>().ToTable("player");

        modelBuilder.Entity<Team>().HasKey(t => t.Id);
        modelBuilder.Entity<Team>().Property(t => t.Id).ValueGeneratedNever(); // the server's team id
        modelBuilder.Entity<Team>().ToTable("team");

        modelBuilder.Entity<Club>().HasKey(c => c.Id);
        modelBuilder.Entity<Club>().Property(c => c.Id).ValueGeneratedNever(); // the server's club id
        modelBuilder.Entity<Club>().ToTable("club");

        modelBuilder.Entity<MatchResult>().HasKey(m => m.Id);
        modelBuilder.Entity<MatchResult>().Property(m => m.Id).UseAutoincrement();
        modelBuilder.Entity<MatchResult>().ToTable("match_result");

        modelBuilder.Entity<ScoreEvent>().HasKey(e => e.Id);
        modelBuilder.Entity<ScoreEvent>().Property(e => e.Id).UseAutoincrement();
        modelBuilder.Entity<ScoreEvent>().ToTable("score_event");

        modelBuilder.Entity<MatchScoreStat>().HasKey(s => s.Id);
        modelBuilder.Entity<MatchScoreStat>().Property(s => s.Id).UseAutoincrement();
        modelBuilder.Entity<MatchScoreStat>().ToTable("match_score_stat");

        modelBuilder.Entity<Setting>().HasKey(s => s.Id);
        modelBuilder.Entity<Setting>().ToTable("Settings");
    }
}
