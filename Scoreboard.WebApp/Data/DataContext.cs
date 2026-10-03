using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Models;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Data;

public class DataContext : DbContext
{
    private readonly int? _organizationId;

    public DataContext(DbContextOptions<DataContext> options, IOrganizationService organizationService) : base(options)
    {
        _organizationId = organizationService.GetCurrentOrganizationId();
    }

    public int CurrentOrganizationId => _organizationId ?? throw new InvalidOperationException("No organization for this request.");

    public DbSet<User> UserSet => Set<User>();
    public DbSet<Organization> OrganizationSet => Set<Organization>();
    public DbSet<StaffMember> StaffMemberSet => Set<StaffMember>();
    public DbSet<BilliardTable> BilliardTableSet => Set<BilliardTable>();
    public DbSet<Player> PlayerSet => Set<Player>();
    public DbSet<RefreshToken> RefreshTokenSet => Set<RefreshToken>();
    public DbSet<PlayerInvite> PlayerInviteSet => Set<PlayerInvite>();
    public DbSet<ExternalMatch> ExternalMatchSet => Set<ExternalMatch>();
    public DbSet<PasswordResetCode> PasswordResetCodeSet => Set<PasswordResetCode>();
    public DbSet<Club> ClubSet => Set<Club>();
    public DbSet<Association> AssociationSet => Set<Association>();
    public DbSet<Region> RegionSet => Set<Region>();
    public DbSet<Country> CountrySet => Set<Country>();
    public DbSet<City> CitySet => Set<City>();
    public DbSet<EntityChange> EntityChangeSet => Set<EntityChange>();
    public DbSet<ClientSync> ClientSyncSet => Set<ClientSync>();
    public DbSet<OrganizationCountry> OrganizationCountrySet => Set<OrganizationCountry>();
    public DbSet<OrganizationCity> OrganizationCitySet => Set<OrganizationCity>();
    public DbSet<OrganizationRegion> OrganizationRegionSet => Set<OrganizationRegion>();
    public DbSet<Team> TeamSet => Set<Team>();
    public DbSet<TeamMember> TeamMemberSet => Set<TeamMember>();
    public DbSet<PricingRule> PricingRuleSet => Set<PricingRule>();
    public DbSet<Reservation> ReservationSet => Set<Reservation>();
    public DbSet<TableSession> TableSessionSet => Set<TableSession>();
    public DbSet<ProductCategory> ProductCategorySet => Set<ProductCategory>();
    public DbSet<Product> ProductSet => Set<Product>();
    public DbSet<OrderItem> OrderItemSet => Set<OrderItem>();
    public DbSet<Payment> PaymentSet => Set<Payment>();
    public DbSet<CashRegisterShift> CashRegisterShiftSet => Set<CashRegisterShift>();

    public DbSet<Match> MatchesSet => Set<Match>();
    public DbSet<MatchParticipant> MatchParticipantSet => Set<MatchParticipant>();
    public DbSet<MatchEvent> MatchEventSet => Set<MatchEvent>();
    public DbSet<StageGroup> StageGroupSet => Set<StageGroup>();
    public DbSet<Cup> CupSet => Set<Cup>();
    public DbSet<CupParticipant> CupParticipantSet => Set<CupParticipant>();
    public DbSet<CupRuleBlock> CupRuleBlockSet => Set<CupRuleBlock>();
    public DbSet<CupMatch> CupMatchSet => Set<CupMatch>();
    public DbSet<MatchStat> MatchStatSet => Set<MatchStat>();
    public DbSet<MatchStatBucket> MatchStatBucketSet => Set<MatchStatBucket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e =>
        {
            e.OwnsOne(o => o.Address, a => a.ToJson());
            e.OwnsMany(o => o.OpeningHours, h => h.ToJson());
            e.HasIndex(o => o.Slug).IsUnique();
            e.HasIndex(o => o.ClientId).IsUnique().HasFilter("\"ClientId\" IS NOT NULL");
            e.Property(o => o.ClientId).HasMaxLength(20);
            e.Property(o => o.Language).HasMaxLength(5);
            e.Property(o => o.CountryCode).HasMaxLength(2);
            e.Property(o => o.Name).HasMaxLength(200);
            e.Property(o => o.Slug).HasMaxLength(100);
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

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            e.HasIndex(u => u.Phone).IsUnique().HasFilter("\"Phone\" IS NOT NULL");
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.SecurityStamp).HasMaxLength(64);
            e.HasOne(u => u.Organization).WithMany().HasForeignKey(u => u.OrganizationId);
            e.Property(u => u.DisplayName).HasMaxLength(200);
            e.HasOne(u => u.Player).WithOne(p => p.User).HasForeignKey<Player>(p => p.UserId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.Property(t => t.TokenHash).HasMaxLength(100);
            e.Property(t => t.DeviceName).HasMaxLength(100);
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId);
        });

        modelBuilder.Entity<PlayerInvite>(e =>
        {
            e.HasIndex(i => i.Code).IsUnique();
            e.Property(i => i.Code).HasMaxLength(20);
            e.HasOne(i => i.Player).WithMany().HasForeignKey(i => i.PlayerId);
        });

        modelBuilder.Entity<PasswordResetCode>(e =>
        {
            e.HasIndex(c => new { c.UserId, c.CreatedAt });
            e.Property(c => c.CodeHash).HasMaxLength(100);
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
        });

        modelBuilder.Entity<ExternalMatch>(e =>
        {
            e.HasIndex(m => new { m.OrganizationId, m.PlayerId, m.PlayedOn });
            e.Property(m => m.OpponentName).HasMaxLength(100);
            e.Property(m => m.Venue).HasMaxLength(100);
            e.HasOne(m => m.Player).WithMany().HasForeignKey(m => m.PlayerId);
        });

        modelBuilder.Entity<StaffMember>(e =>
        {
            e.HasIndex(s => new { s.OrganizationId, s.UserId }).IsUnique();
        });

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

        modelBuilder.HasSequence<long>("EntityChangeSeq");
        modelBuilder.Entity<EntityChange>(e =>
        {
            e.HasKey(x => new { x.OrganizationId, x.TableId, x.EntityName, x.EntityId });
            e.Property(x => x.EntityName).HasMaxLength(30);
            e.Property(x => x.Seq).HasDefaultValueSql("nextval('\"EntityChangeSeq\"')");
        });

        modelBuilder.Entity<ClientSync>(e =>
        {
            e.HasKey(x => new { x.OrganizationId, x.TableId });
            e.Property(x => x.InstanceId).HasMaxLength(40);
        });

        modelBuilder.Entity<Country>(e =>
        {
            e.Property(c => c.Code).HasMaxLength(2);
            e.Property(c => c.Name).HasMaxLength(100);
            e.HasIndex(c => c.Code).IsUnique();
        });

        modelBuilder.Entity<City>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
            e.HasOne(c => c.Country).WithMany(c => c.Cities).HasForeignKey(c => c.CountryId);
            e.HasIndex(c => new { c.CountryId, c.Name }).IsUnique();
        });

        modelBuilder.Entity<Region>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(150);
            e.HasOne(r => r.Country).WithMany().HasForeignKey(r => r.CountryId);
            e.HasIndex(r => new { r.CountryId, r.Name }).IsUnique();
        });

        modelBuilder.Entity<OrganizationCountry>(e =>
        {
            e.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId);
            e.HasIndex(x => new { x.OrganizationId, x.CountryId }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        });

        modelBuilder.Entity<OrganizationCity>(e =>
        {
            e.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId);
            e.HasIndex(x => new { x.OrganizationId, x.CityId }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        });

        modelBuilder.Entity<OrganizationRegion>(e =>
        {
            e.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId);
            e.HasIndex(x => new { x.OrganizationId, x.RegionId }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
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
            e.HasIndex(p => p.SystemSlot)
                .IsUnique()
                .HasFilter("\"SystemSlot\" IS NOT NULL");
            e.ToTable(t => t.HasCheckConstraint("CK_PlayerSet_ReservedIds", "\"IsSystem\" OR \"Id\" > 2"));
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

        modelBuilder.Entity<Reservation>(e =>
        {
            e.HasOne(r => r.Session).WithMany().HasForeignKey(r => r.SessionId);
            e.HasIndex(r => new { r.OrganizationId, r.StartAt });
        });

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

        modelBuilder.Entity<PlayerStats>(e => e.HasKey(s => new { s.PlayerId, s.Discipline, s.Scope }));
        modelBuilder.Entity<PlayerOrganizationStats>(e => e.HasKey(s => new { s.PlayerId, s.OrganizationId, s.Discipline }));

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

        modelBuilder.Entity<PricingRule>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Reservation>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<TableSession>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<ProductCategory>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Payment>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<CashRegisterShift>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Tournament>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<League>().HasQueryFilter(e => e.OrganizationId == _organizationId || e.OrganizationId == null);
        modelBuilder.Entity<TeamFixture>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Cup>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<BilliardTable>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Device>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Association>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<PlayerInvite>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<ExternalMatch>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<EntityChange>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<ClientSync>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<OrganizationCountry>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<OrganizationCity>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<OrganizationRegion>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Player>().HasQueryFilter(e => e.CreatedInOrganizationId == _organizationId || e.IsSystem);
        modelBuilder.Entity<Club>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Team>().HasQueryFilter(e => e.Club.OrganizationId == _organizationId);
        modelBuilder.Entity<CustomerMembership>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<RuleSet>().HasQueryFilter(e => e.OrganizationId == _organizationId || e.OrganizationId == null);
        modelBuilder.Entity<Match>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<PlayerOrganizationStats>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<MatchStat>().HasQueryFilter(e => e.OrganizationId == _organizationId);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is null && !entityType.IsOwned())
            {
                entityType.SetTableName(entityType.ClrType.Name);
            }

            foreach (var fk in entityType.GetForeignKeys().Where(f => !f.IsOwnership))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                var name = property.Name;
                var threeDecimals = name.Contains("Average") || name.Contains("Rating") || name.Contains("Delta") || name.Contains("Quantity") || name == "Price" || name == "UnitPriceSnapshot";
                property.SetPrecision(18);
                property.SetScale(threeDecimals ? 3 : 2);
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampAudit();
        var pending = CollectChanges();
        if (pending.Count == 0)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        using var transaction = Database.CurrentTransaction is null ? Database.BeginTransaction() : null;
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        foreach (var change in ResolveChanges(pending))
        {
            Database.ExecuteSqlInterpolated(EntityChangeSql.Upsert(change.OrganizationId, change.Entity, change.Id, change.Deleted));
        }

        transaction?.Commit();
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampAudit();
        var pending = CollectChanges();
        if (pending.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        await using var transaction = Database.CurrentTransaction is null ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        foreach (var change in ResolveChanges(pending))
        {
            await Database.ExecuteSqlInterpolatedAsync(EntityChangeSql.Upsert(change.OrganizationId, change.Entity, change.Id, change.Deleted), cancellationToken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    private sealed record PendingChange(object Entity, bool Deleted);

    private List<PendingChange> CollectChanges()
    {
        var pending = new List<PendingChange>();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not (Player or Team or Club or TeamMember) || entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            var deleted = entry.State == EntityState.Deleted || (entry.Entity is BaseEntity baseEntity && baseEntity.DeletedAt != null);
            pending.Add(new PendingChange(entry.Entity, deleted));
        }

        return pending;
    }

    private List<(int OrganizationId, string Entity, int Id, bool Deleted)> ResolveChanges(List<PendingChange> pending)
    {
        var resolved = new Dictionary<(int, string, int), bool>();
        foreach (var change in pending)
        {
            switch (change.Entity)
            {
                case Club { OrganizationId: int organizationId } club:
                    resolved[(organizationId, nameof(Club), club.Id)] = change.Deleted;
                    break;
                case Player { IsSystem: true } systemPlayer:
                    foreach (var id in OrganizationSet.AsNoTracking().Select(o => o.Id).ToList())
                    {
                        resolved[(id, nameof(Player), systemPlayer.Id)] = change.Deleted;
                    }

                    break;
                case Player player when (player.CreatedInOrganizationId ?? _organizationId) is int playerOrganizationId:
                    resolved[(playerOrganizationId, nameof(Player), player.Id)] = change.Deleted;
                    break;
                case Team team when _organizationId is int teamOrganizationId:
                    resolved[(teamOrganizationId, nameof(Team), team.Id)] = change.Deleted;
                    break;
                case TeamMember member when _organizationId is int memberOrganizationId:
                    resolved.TryAdd((memberOrganizationId, nameof(Team), member.TeamId), false);
                    break;
            }
        }

        return resolved.Select(r => (r.Key.Item1, r.Key.Item2, r.Key.Item3, r.Value)).ToList();
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
