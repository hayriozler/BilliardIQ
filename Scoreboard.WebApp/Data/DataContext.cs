using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Models;

namespace Scoreboard.WebApp.Data;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    // Identity / organization
    public DbSet<User> UserSet => Set<User>();
    public DbSet<Organization> OrganizationSet => Set<Organization>();
    public DbSet<StaffMember> StaffMemberSet => Set<StaffMember>();
    public DbSet<BilliardTable> BilliardTableSet => Set<BilliardTable>();
    public DbSet<Device> DeviceSet => Set<Device>();

    // People
    public DbSet<Player> PlayerSet => Set<Player>();
    public DbSet<Club> ClubSet => Set<Club>();
    public DbSet<Association> AssociationSet => Set<Association>();
    public DbSet<Region> RegionSet => Set<Region>();
    public DbSet<Country> CountrySet => Set<Country>();
    public DbSet<City> CitySet => Set<City>();
    public DbSet<ClubMembership> ClubMembershipSet => Set<ClubMembership>();
    public DbSet<Team> TeamSet => Set<Team>();
    public DbSet<TeamMember> TeamMemberSet => Set<TeamMember>();
    public DbSet<CustomerMembership> CustomerMembershipSet => Set<CustomerMembership>();

    // Billing / POS
    public DbSet<PricingRule> PricingRuleSet => Set<PricingRule>();
    public DbSet<Reservation> ReservationSet => Set<Reservation>();
    public DbSet<TableSession> TableSessionSet => Set<TableSession>();
    public DbSet<SessionPlayer> SessionPlayerSet => Set<SessionPlayer>();
    public DbSet<ProductCategory> ProductCategorySet => Set<ProductCategory>();
    public DbSet<Product> ProductSet => Set<Product>();
    public DbSet<OrderItem> OrderItemSet => Set<OrderItem>();
    public DbSet<Payment> PaymentSet => Set<Payment>();
    public DbSet<CashRegisterShift> CashRegisterShiftSet => Set<CashRegisterShift>();

    // Scoring
    public DbSet<RuleSet> RuleSetSet => Set<RuleSet>();
    public DbSet<Match> MatchesSet => Set<Match>();
    public DbSet<MatchParticipant> MatchParticipantSet => Set<MatchParticipant>();
    public DbSet<MatchSet> MatchSetSet => Set<MatchSet>();
    public DbSet<Inning> InningSet => Set<Inning>();
    public DbSet<MatchEvent> MatchEventSet => Set<MatchEvent>();

    // Competition
    public DbSet<Tournament> TournamentSet => Set<Tournament>();
    public DbSet<TournamentStage> TournamentStageSet => Set<TournamentStage>();
    public DbSet<StageGroup> StageGroupSet => Set<StageGroup>();
    public DbSet<TournamentEntry> TournamentEntrySet => Set<TournamentEntry>();

    // Salon tournaments (Turnuvalar module)
    public DbSet<Cup> CupSet => Set<Cup>();
    public DbSet<CupParticipant> CupParticipantSet => Set<CupParticipant>();
    public DbSet<CupRuleBlock> CupRuleBlockSet => Set<CupRuleBlock>();
    public DbSet<CupMatch> CupMatchSet => Set<CupMatch>();
    public DbSet<StageStanding> StageStandingSet => Set<StageStanding>();
    public DbSet<League> LeagueSet => Set<League>();
    public DbSet<Season> SeasonSet => Set<Season>();
    public DbSet<SeasonTeam> SeasonTeamSet => Set<SeasonTeam>();
    public DbSet<TeamFixture> TeamFixtureSet => Set<TeamFixture>();

    // Stats
    public DbSet<PlayerStats> PlayerStatsSet => Set<PlayerStats>();
    public DbSet<PlayerOrganizationStats> PlayerOrganizationStatsSet => Set<PlayerOrganizationStats>();
    public DbSet<RatingHistory> RatingHistorySet => Set<RatingHistory>();
    public DbSet<AuditLog> AuditLogSet => Set<AuditLog>();

    // Legacy kiosk sync (Pi pushes flat match summaries here until it speaks MatchEvent)
    public DbSet<MatchStat> MatchStatSet => Set<MatchStat>();
    public DbSet<MatchStatBucket> MatchStatBucketSet => Set<MatchStatBucket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- owned / JSON value objects ----------
        modelBuilder.Entity<Organization>(e =>
        {
            e.OwnsOne(o => o.Address, a => a.ToJson());
            e.OwnsMany(o => o.OpeningHours, h => h.ToJson());
            e.HasIndex(o => o.Slug).IsUnique();
            e.HasIndex(o => o.Code).IsUnique();
            e.HasIndex(o => o.ClientId).IsUnique().HasFilter("\"ClientId\" IS NOT NULL");
            e.Property(o => o.ClientId).HasMaxLength(20);
            e.Property(o => o.Language).HasMaxLength(5);
            e.Property(o => o.CountryCode).HasMaxLength(2);
            e.Property(o => o.Name).HasMaxLength(200);
            e.Property(o => o.Slug).HasMaxLength(100);
            e.Property(o => o.Code).HasMaxLength(10);
            e.HasOne(o => o.DefaultRuleSet).WithMany().HasForeignKey(o => o.DefaultRuleSetId);
        });

        modelBuilder.Entity<PricingRule>(e =>
        {
            e.OwnsMany(p => p.TimeSlots, s => s.ToJson());
        });

        modelBuilder.Entity<Match>(e =>
        {
            e.OwnsOne(m => m.Rules, r => r.ToJson());
            e.HasIndex(m => new { m.OrganizationId, m.StartedAt });
        });

        modelBuilder.Entity<TableSession>(e =>
        {
            e.OwnsOne(s => s.PricingSnapshot, p =>
            {
                p.ToJson();
                p.OwnsMany(x => x.TimeSlots);
            });
            e.HasOne(s => s.Reservation).WithMany().HasForeignKey(s => s.ReservationId);
            e.HasIndex(s => new { s.OrganizationId, s.OpenedAt });
        });

        // ---------- identity ----------
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            e.HasIndex(u => u.Phone).IsUnique().HasFilter("\"Phone\" IS NOT NULL");
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.DisplayName).HasMaxLength(200);
            e.HasOne(u => u.Player).WithOne(p => p.User).HasForeignKey<Player>(p => p.UserId);
        });

        modelBuilder.Entity<StaffMember>(e =>
        {
            e.HasIndex(s => new { s.OrganizationId, s.UserId }).IsUnique();
        });

        // ---------- tables / devices ----------
        modelBuilder.Entity<BilliardTable>(e =>
        {
            e.HasIndex(t => new { t.OrganizationId, t.Number }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
            e.HasIndex(t => new { t.OrganizationId, t.ScoreboardNo }).IsUnique()
                .HasFilter("\"ScoreboardNo\" IS NOT NULL AND \"DeletedAt\" IS NULL");
            e.HasOne(t => t.CurrentSession).WithMany().HasForeignKey(t => t.CurrentSessionId);
            e.HasOne(t => t.PricingRule).WithMany().HasForeignKey(t => t.PricingRuleId);
            e.HasOne(t => t.Device).WithOne(d => d.Table).HasForeignKey<Device>(d => d.TableId);
        });

        modelBuilder.Entity<Device>(e =>
        {
            e.HasIndex(d => d.PairingCode).IsUnique().HasFilter("\"PairingCode\" IS NOT NULL");
            e.Property(d => d.PairingCode).HasMaxLength(10);
            e.Property(d => d.Name).HasMaxLength(200);
        });

        // ---------- people ----------
        // ---------- salon tournaments ----------
        modelBuilder.Entity<Cup>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.Description).HasMaxLength(1000);
            e.HasIndex(c => c.OrganizationId);
            e.HasOne(c => c.Organization).WithMany().HasForeignKey(c => c.OrganizationId);
        });

        modelBuilder.Entity<CupParticipant>(e =>
        {
            e.HasIndex(p => new { p.CupId, p.PlayerId }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
            e.HasOne(p => p.Cup).WithMany(c => c.Participants).HasForeignKey(p => p.CupId);
            e.HasOne(p => p.Player).WithMany().HasForeignKey(p => p.PlayerId);
        });

        modelBuilder.Entity<CupRuleBlock>(e =>
        {
            e.HasOne(b => b.Cup).WithMany(c => c.RuleBlocks).HasForeignKey(b => b.CupId);
        });

        modelBuilder.Entity<CupMatch>(e =>
        {
            e.HasIndex(m => new { m.CupId, m.Round, m.Number }).IsUnique();
            e.HasOne(m => m.Cup).WithMany(c => c.Matches).HasForeignKey(m => m.CupId);
            e.HasOne(m => m.ParticipantA).WithMany().HasForeignKey(m => m.ParticipantAId);
            e.HasOne(m => m.ParticipantB).WithMany().HasForeignKey(m => m.ParticipantBId);
            e.HasOne(m => m.Table).WithMany().HasForeignKey(m => m.TableId);
        });

        modelBuilder.Entity<Country>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
            e.HasIndex(c => new { c.OrganizationId, c.Name }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        });

        modelBuilder.Entity<City>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
            e.HasOne(c => c.Country).WithMany(c => c.Cities).HasForeignKey(c => c.CountryId);
            e.HasIndex(c => new { c.OrganizationId, c.CountryId, c.Name }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        });

        modelBuilder.Entity<Region>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(150);
            e.HasIndex(r => new { r.OrganizationId, r.Name }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        });

        modelBuilder.Entity<Association>(e =>
        {
            e.Property(a => a.Name).HasMaxLength(150);
            e.HasIndex(a => new { a.OrganizationId, a.Name }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        });

        modelBuilder.Entity<Player>(e =>
        {
            e.Property(p => p.FirstName).HasMaxLength(100);
            e.Property(p => p.LastName).HasMaxLength(100);
            e.Property(p => p.Nickname).HasMaxLength(100);
            e.Property(p => p.FederationLicenseNo).HasMaxLength(50);
            e.HasIndex(p => new { p.CreatedInOrganizationId, p.SystemSlot })
                .IsUnique()
                .HasFilter("\"SystemSlot\" IS NOT NULL");
            e.HasOne(p => p.Association).WithMany().HasForeignKey(p => p.AssociationId);
            e.HasOne(p => p.Region).WithMany().HasForeignKey(p => p.RegionId);
            e.HasOne(p => p.CountryRef).WithMany().HasForeignKey(p => p.CountryId);
            e.HasOne(p => p.CityRef).WithMany().HasForeignKey(p => p.CityId);
            e.Property(p => p.DisplayName).HasMaxLength(100);
            e.Property(p => p.Email).HasMaxLength(200);
            e.Property(p => p.City).HasMaxLength(100);
            e.Property(p => p.Nationality).HasMaxLength(100);
            e.HasIndex(p => p.CreatedInOrganizationId);
            e.HasIndex(p => new { p.CreatedInOrganizationId, p.ShortcutNumber })
                .IsUnique()
                .HasFilter("\"ShortcutNumber\" IS NOT NULL");
        });

        modelBuilder.Entity<Club>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.ShortName).HasMaxLength(20);
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(200);
            e.Property(t => t.ShortName).HasMaxLength(20);
            e.HasOne(t => t.Season).WithMany().HasForeignKey(t => t.SeasonId);
            e.HasOne(t => t.HomeOrganization).WithMany().HasForeignKey(t => t.HomeOrganizationId);
        });

        modelBuilder.Entity<TeamMember>(e =>
        {
            e.HasIndex(m => new { m.TeamId, m.PlayerId }).IsUnique();
        });

        modelBuilder.Entity<CustomerMembership>(e =>
        {
            e.HasIndex(m => new { m.OrganizationId, m.MemberNo }).IsUnique();
        });

        // ---------- billing ----------
        modelBuilder.Entity<Reservation>(e =>
        {
            e.HasOne(r => r.Session).WithMany().HasForeignKey(r => r.SessionId);
            e.HasIndex(r => new { r.OrganizationId, r.StartAt });
        });

        // ---------- scoring ----------
        modelBuilder.Entity<MatchEvent>(e =>
        {
            e.HasIndex(x => new { x.MatchId, x.Seq }).IsUnique();
            e.HasOne(x => x.Match).WithMany(m => m.Events).HasForeignKey(x => x.MatchId);
        });

        modelBuilder.Entity<Match>(e =>
        {
            e.HasOne(m => m.Table).WithMany(t => t.Matches).HasForeignKey(m => m.TableId);
            e.HasOne(m => m.Session).WithMany(s => s.Matches).HasForeignKey(m => m.SessionId);
            e.HasOne(m => m.Referee).WithMany().HasForeignKey(m => m.RefereeUserId);
            e.HasOne(m => m.ScorekeeperDevice).WithMany().HasForeignKey(m => m.ScorekeeperDeviceId);
        });

        modelBuilder.Entity<MatchParticipant>(e =>
        {
            e.HasIndex(p => new { p.MatchId, p.Side }).IsUnique();
        });

        // ---------- stats ----------
        modelBuilder.Entity<PlayerStats>(e => e.HasKey(s => new { s.PlayerId, s.Discipline, s.Scope }));
        modelBuilder.Entity<PlayerOrganizationStats>(e => e.HasKey(s => new { s.PlayerId, s.OrganizationId, s.Discipline }));

        // ---------- legacy kiosk sync ----------
        modelBuilder.Entity<MatchStat>(e =>
        {
            e.Property(s => s.Player1Name).HasMaxLength(100);
            e.Property(s => s.Player2Name).HasMaxLength(100);
            e.HasOne(s => s.Device).WithMany().HasForeignKey(s => s.DeviceId);
            e.HasOne(s => s.Organization).WithMany().HasForeignKey(s => s.OrganizationId);
            e.HasOne(s => s.Table).WithMany().HasForeignKey(s => s.TableId);
            e.HasIndex(s => new { s.OrganizationId, s.PlayedAt });
        });

        modelBuilder.Entity<MatchStatBucket>(e =>
        {
            e.HasOne(b => b.MatchStat).WithMany(s => s.Buckets).HasForeignKey(b => b.MatchStatId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(b => new { b.MatchStatId, b.PlayerSlot, b.BucketIndex }).IsUnique();
        });

        // ---------- conventions ----------
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // History matters more than cascades: deletes are explicit (or soft, via DeletedAt).
            foreach (var fk in entityType.GetForeignKeys().Where(f => !f.IsOwnership))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                var name = property.Name;
                var threeDecimals = name.Contains("Average") || name.Contains("Rating") || name.Contains("Delta") || name.Contains("Quantity");
                property.SetPrecision(18);
                property.SetScale(threeDecimals ? 3 : 2);
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampAudit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampAudit()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
