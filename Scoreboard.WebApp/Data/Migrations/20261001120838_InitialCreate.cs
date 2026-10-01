using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    ActorDeviceId = table.Column<int>(type: "integer", nullable: true),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true),
                    Ip = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "League",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: true),
                    IsTeamLeague = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_League", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    TargetPoints = table.Column<int>(type: "integer", nullable: false),
                    SetsToWin = table.Column<int>(type: "integer", nullable: true),
                    InningLimit = table.Column<int>(type: "integer", nullable: true),
                    ShotClockSeconds = table.Column<int>(type: "integer", nullable: true),
                    ExtensionsPerPlayer = table.Column<int>(type: "integer", nullable: false),
                    ExtensionSeconds = table.Column<int>(type: "integer", nullable: false),
                    EqualizingInning = table.Column<bool>(type: "boolean", nullable: false),
                    PenaltyShootoutOnTie = table.Column<bool>(type: "boolean", nullable: false),
                    TimeoutsPerPlayer = table.Column<int>(type: "integer", nullable: false),
                    WarmupSeconds = table.Column<int>(type: "integer", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsPlatformAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LegalName = table.Column<string>(type: "text", nullable: true),
                    TaxNumber = table.Column<string>(type: "text", nullable: true),
                    TaxOffice = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "text", nullable: true),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    BillingRoundingMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinimumBillableMinutes = table.Column<int>(type: "integer", nullable: false),
                    DefaultRuleSetId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Plan = table.Column<int>(type: "integer", nullable: false),
                    PlanExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Address = table.Column<string>(type: "jsonb", nullable: false),
                    OpeningHours = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organization_RuleSet_DefaultRuleSetId",
                        column: x => x.DefaultRuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Season",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeagueId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RuleSetId = table.Column<int>(type: "integer", nullable: false),
                    BoardsPerFixture = table.Column<int>(type: "integer", nullable: false),
                    PointsForWin = table.Column<int>(type: "integer", nullable: false),
                    PointsForDraw = table.Column<int>(type: "integer", nullable: false),
                    PointsForLoss = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Season", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Season_League_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "League",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Season_RuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Association",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Association", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Association_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashRegisterShift",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    OpenedByStaffId = table.Column<int>(type: "integer", nullable: false),
                    ClosedByStaffId = table.Column<int>(type: "integer", nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OpeningCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CountedCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Difference = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashRegisterShift", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashRegisterShift_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Club",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    FoundedYear = table.Column<int>(type: "integer", nullable: true),
                    FederationClubNo = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    PrimaryColor = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Club", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Club_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Country",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Country_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MaxInnings = table.Column<int>(type: "integer", nullable: true),
                    TotalRounds = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cup_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PricingRule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    DefaultHourlyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PerPlayerSurcharge = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MemberDiscountPercent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TimeSlots = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingRule_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategory_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Region",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Region", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Region_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Roles = table.Column<int[]>(type: "integer[]", nullable: false),
                    PinCodeHash = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HiredAt = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffMember_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffMember_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tournament",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    DefaultRuleSetId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RegistrationDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaxEntries = table.Column<int>(type: "integer", nullable: true),
                    EntryFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PrizePool = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxAverageLimit = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    IsHandicap = table.Column<bool>(type: "boolean", nullable: false),
                    PosterUrl = table.Column<string>(type: "text", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournament", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tournament_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tournament_RuleSet_DefaultRuleSetId",
                        column: x => x.DefaultRuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Team",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SeasonId = table.Column<int>(type: "integer", nullable: true),
                    HomeOrganizationId = table.Column<int>(type: "integer", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    AvatarId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Team_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Team_Organization_HomeOrganizationId",
                        column: x => x.HomeOrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Team_Season_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "City",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                    table.ForeignKey(
                        name: "FK_City_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_City_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CupRuleBlock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CupId = table.Column<int>(type: "integer", nullable: false),
                    FromRound = table.Column<int>(type: "integer", nullable: false),
                    ToRound = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    FixedTarget = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CupRuleBlock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CupRuleBlock_Cup_CupId",
                        column: x => x.CupId,
                        principalTable: "Cup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TrackStock = table.Column<bool>(type: "boolean", nullable: false),
                    StockQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Product_ProductCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentStage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    RuleSetId = table.Column<int>(type: "integer", nullable: true),
                    QualifiersPerGroup = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Tiebreakers = table.Column<int[]>(type: "integer[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentStage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentStage_RuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentStage_Tournament_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeasonTeam",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Played = table.Column<int>(type: "integer", nullable: false),
                    Won = table.Column<int>(type: "integer", nullable: false),
                    Drawn = table.Column<int>(type: "integer", nullable: false),
                    Lost = table.Column<int>(type: "integer", nullable: false),
                    BoardsWon = table.Column<int>(type: "integer", nullable: false),
                    BoardsLost = table.Column<int>(type: "integer", nullable: false),
                    LeaguePoints = table.Column<int>(type: "integer", nullable: false),
                    GeneralAverage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonTeam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonTeam_Season_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeasonTeam_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamFixture",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    HomeTeamId = table.Column<int>(type: "integer", nullable: false),
                    AwayTeamId = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HomeBoardsWon = table.Column<int>(type: "integer", nullable: false),
                    AwayBoardsWon = table.Column<int>(type: "integer", nullable: false),
                    HomeLeaguePoints = table.Column<int>(type: "integer", nullable: true),
                    AwayLeaguePoints = table.Column<int>(type: "integer", nullable: true),
                    RefereeUserId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RefereeId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamFixture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamFixture_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamFixture_Season_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamFixture_Team_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamFixture_Team_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamFixture_User_RefereeId",
                        column: x => x.RefereeId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShortcutNumber = table.Column<int>(type: "integer", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    Handedness = table.Column<int>(type: "integer", nullable: true),
                    FederationLicenseNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LicenseValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    AssociationId = table.Column<int>(type: "integer", nullable: true),
                    RegionId = table.Column<int>(type: "integer", nullable: true),
                    CountryId = table.Column<int>(type: "integer", nullable: true),
                    CityId = table.Column<int>(type: "integer", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    SystemSlot = table.Column<int>(type: "integer", nullable: true),
                    UmbPlayerId = table.Column<string>(type: "text", nullable: true),
                    DefaultTargetPoints = table.Column<int>(type: "integer", nullable: true),
                    IsGuest = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedInOrganizationId = table.Column<int>(type: "integer", nullable: true),
                    IsPublicProfile = table.Column<bool>(type: "boolean", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AvatarId = table.Column<int>(type: "integer", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => x.Id);
                    table.CheckConstraint("CK_PlayerSet_ReservedIds", "\"IsSystem\" OR \"Id\" > 2");
                    table.ForeignKey(
                        name: "FK_Player_Association_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Association",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Player_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Player_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Player_Region_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Region",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Player_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StageGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StageId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    TableIds = table.Column<List<int>>(type: "integer[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageGroup_TournamentStage_StageId",
                        column: x => x.StageId,
                        principalTable: "TournamentStage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubMembership",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    LicenseSeason = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    LeftAt = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubMembership", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubMembership_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubMembership_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CupParticipant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CupId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    HandicapTarget = table.Column<int>(type: "integer", nullable: true),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CupParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CupParticipant_Cup_CupId",
                        column: x => x.CupId,
                        principalTable: "Cup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CupParticipant_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerMembership",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    MemberNo = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrepaidBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrepaidMinutes = table.Column<int>(type: "integer", nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMembership", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerMembership_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerMembership_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerOrganizationStats",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Draws = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    TotalInnings = table.Column<int>(type: "integer", nullable: false),
                    GeneralAverage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BestGameAverage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    HighRun = table.Column<int>(type: "integer", nullable: false),
                    TotalPlayMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastPlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerOrganizationStats", x => new { x.PlayerId, x.OrganizationId, x.Discipline });
                    table.ForeignKey(
                        name: "FK_PlayerOrganizationStats_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerOrganizationStats_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStats",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Draws = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    TotalInnings = table.Column<int>(type: "integer", nullable: false),
                    GeneralAverage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BestGameAverage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BestGameAverageMatchId = table.Column<int>(type: "integer", nullable: true),
                    HighRun = table.Column<int>(type: "integer", nullable: false),
                    HighRunMatchId = table.Column<int>(type: "integer", nullable: true),
                    Last10Average = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStats", x => new { x.PlayerId, x.Discipline, x.Scope });
                    table.ForeignKey(
                        name: "FK_PlayerStats_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    BoardOrder = table.Column<int>(type: "integer", nullable: true),
                    JoinedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    LeftAt = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMember_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMember_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ClubId = table.Column<int>(type: "integer", nullable: true),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HandicapTargetPoints = table.Column<int>(type: "integer", nullable: true),
                    EntryAverage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    FeePaid = table.Column<bool>(type: "boolean", nullable: false),
                    FinalRank = table.Column<int>(type: "integer", nullable: true),
                    PrizeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentEntry_Club_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Club",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentEntry_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentEntry_Tournament_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournament",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StageStanding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StageId = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: true),
                    EntryId = table.Column<int>(type: "integer", nullable: false),
                    Played = table.Column<int>(type: "integer", nullable: false),
                    Won = table.Column<int>(type: "integer", nullable: false),
                    Drawn = table.Column<int>(type: "integer", nullable: false),
                    Lost = table.Column<int>(type: "integer", nullable: false),
                    MatchPoints = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    TotalInnings = table.Column<int>(type: "integer", nullable: false),
                    GeneralAverage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    HighRun = table.Column<int>(type: "integer", nullable: false),
                    BestGameAverage = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: true),
                    Qualified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageStanding", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageStanding_StageGroup_GroupId",
                        column: x => x.GroupId,
                        principalTable: "StageGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StageStanding_TournamentEntry_EntryId",
                        column: x => x.EntryId,
                        principalTable: "TournamentEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StageStanding_TournamentStage_StageId",
                        column: x => x.StageId,
                        principalTable: "TournamentStage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BilliardTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    ScoreboardNo = table.Column<int>(type: "integer", nullable: true),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    IsHeated = table.Column<bool>(type: "boolean", nullable: false),
                    ClothBrand = table.Column<string>(type: "text", nullable: true),
                    ClothChangedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PricingRuleId = table.Column<int>(type: "integer", nullable: true),
                    CurrentSessionId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    FloorPlanX = table.Column<double>(type: "double precision", nullable: true),
                    FloorPlanY = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BilliardTable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BilliardTable_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BilliardTable_PricingRule_PricingRuleId",
                        column: x => x.PricingRuleId,
                        principalTable: "PricingRule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CupMatch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CupId = table.Column<int>(type: "integer", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    ParticipantAId = table.Column<int>(type: "integer", nullable: true),
                    ParticipantBId = table.Column<int>(type: "integer", nullable: true),
                    TargetA = table.Column<int>(type: "integer", nullable: true),
                    TargetB = table.Column<int>(type: "integer", nullable: true),
                    TableId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScoreA = table.Column<int>(type: "integer", nullable: false),
                    ScoreB = table.Column<int>(type: "integer", nullable: false),
                    Innings = table.Column<int>(type: "integer", nullable: false),
                    HighRunA = table.Column<int>(type: "integer", nullable: false),
                    HighRunB = table.Column<int>(type: "integer", nullable: false),
                    WinnerParticipantId = table.Column<int>(type: "integer", nullable: true),
                    PlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CupMatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CupMatch_BilliardTable_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CupMatch_CupParticipant_ParticipantAId",
                        column: x => x.ParticipantAId,
                        principalTable: "CupParticipant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CupMatch_CupParticipant_ParticipantBId",
                        column: x => x.ParticipantBId,
                        principalTable: "CupParticipant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CupMatch_Cup_CupId",
                        column: x => x.CupId,
                        principalTable: "Cup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Device",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    TableId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PairingCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PairedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeviceTokenHash = table.Column<string>(type: "text", nullable: true),
                    Platform = table.Column<int>(type: "integer", nullable: true),
                    AppVersion = table.Column<string>(type: "text", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Device", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Device_BilliardTable_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Device_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchStat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: true),
                    TableId = table.Column<int>(type: "integer", nullable: true),
                    TableNo = table.Column<int>(type: "integer", nullable: true),
                    DeviceId = table.Column<int>(type: "integer", nullable: true),
                    Player1ExternalId = table.Column<int>(type: "integer", nullable: true),
                    Player1Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Player1Score = table.Column<int>(type: "integer", nullable: false),
                    Player1Avg = table.Column<double>(type: "double precision", nullable: false),
                    Player1HighRun = table.Column<int>(type: "integer", nullable: false),
                    Player2ExternalId = table.Column<int>(type: "integer", nullable: true),
                    Player2Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Player2Score = table.Column<int>(type: "integer", nullable: false),
                    Player2Avg = table.Column<double>(type: "double precision", nullable: false),
                    Player2HighRun = table.Column<int>(type: "integer", nullable: false),
                    Inning = table.Column<int>(type: "integer", nullable: false),
                    MatchTarget = table.Column<int>(type: "integer", nullable: false),
                    Winner = table.Column<int>(type: "integer", nullable: false),
                    PlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BucketMinutes = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchStat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchStat_BilliardTable_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchStat_Device_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchStat_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchStatBucket",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchStatId = table.Column<int>(type: "integer", nullable: false),
                    PlayerSlot = table.Column<int>(type: "integer", nullable: false),
                    BucketIndex = table.Column<int>(type: "integer", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchStatBucket", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchStatBucket_MatchStat_MatchStatId",
                        column: x => x.MatchStatId,
                        principalTable: "MatchStat",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    SetNo = table.Column<int>(type: "integer", nullable: true),
                    InningNo = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    ExtensionsUsed = table.Column<int>(type: "integer", nullable: false),
                    IsEqualizing = table.Column<bool>(type: "boolean", nullable: false),
                    ScoreAfter = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inning", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Match",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    TableId = table.Column<int>(type: "integer", nullable: true),
                    SessionId = table.Column<int>(type: "integer", nullable: true),
                    Context = table.Column<int>(type: "integer", nullable: false),
                    TournamentStageId = table.Column<int>(type: "integer", nullable: true),
                    StageGroupId = table.Column<int>(type: "integer", nullable: true),
                    BracketRound = table.Column<int>(type: "integer", nullable: true),
                    BracketPosition = table.Column<int>(type: "integer", nullable: true),
                    TeamFixtureId = table.Column<int>(type: "integer", nullable: true),
                    BoardNumber = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    BreakingSide = table.Column<int>(type: "integer", nullable: true),
                    CurrentSide = table.Column<int>(type: "integer", nullable: true),
                    CurrentInning = table.Column<int>(type: "integer", nullable: false),
                    CurrentSetNo = table.Column<int>(type: "integer", nullable: true),
                    WinnerSide = table.Column<int>(type: "integer", nullable: true),
                    RefereeUserId = table.Column<int>(type: "integer", nullable: true),
                    ScorekeeperDeviceId = table.Column<int>(type: "integer", nullable: true),
                    LastEventSeq = table.Column<long>(type: "bigint", nullable: false),
                    IsStreamed = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Rules = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Match", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Match_BilliardTable_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_Device_ScorekeeperDeviceId",
                        column: x => x.ScorekeeperDeviceId,
                        principalTable: "Device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_StageGroup_StageGroupId",
                        column: x => x.StageGroupId,
                        principalTable: "StageGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_TeamFixture_TeamFixtureId",
                        column: x => x.TeamFixtureId,
                        principalTable: "TeamFixture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_TournamentStage_TournamentStageId",
                        column: x => x.TournamentStageId,
                        principalTable: "TournamentStage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Match_User_RefereeUserId",
                        column: x => x.RefereeUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchEvent",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    Seq = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    ClientTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ServerTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeviceId = table.Column<int>(type: "integer", nullable: true),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchEvent_Match_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Match",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchParticipant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: true),
                    GuestName = table.Column<string>(type: "text", nullable: true),
                    BallColor = table.Column<int>(type: "integer", nullable: false),
                    TargetPoints = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Innings = table.Column<int>(type: "integer", nullable: false),
                    HighRun = table.Column<int>(type: "integer", nullable: false),
                    Average = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    SetsWon = table.Column<int>(type: "integer", nullable: false),
                    ExtensionsUsed = table.Column<int>(type: "integer", nullable: false),
                    TimeoutsUsed = table.Column<int>(type: "integer", nullable: false),
                    PenaltyScore = table.Column<int>(type: "integer", nullable: true),
                    Result = table.Column<int>(type: "integer", nullable: true),
                    MatchPoints = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchParticipant_Match_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Match",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchParticipant_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    SetNo = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScoreA = table.Column<int>(type: "integer", nullable: false),
                    ScoreB = table.Column<int>(type: "integer", nullable: false),
                    InningsA = table.Column<int>(type: "integer", nullable: false),
                    InningsB = table.Column<int>(type: "integer", nullable: false),
                    HighRunA = table.Column<int>(type: "integer", nullable: false),
                    HighRunB = table.Column<int>(type: "integer", nullable: false),
                    WinnerSide = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchSet_Match_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Match",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatingHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    System = table.Column<int>(type: "integer", nullable: false),
                    MatchId = table.Column<int>(type: "integer", nullable: true),
                    RatingBefore = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    RatingAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Delta = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingHistory_Match_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Match",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RatingHistory_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "text", nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRateSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderedByStaffId = table.Column<int>(type: "integer", nullable: true),
                    SessionPlayerId = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItem_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<int>(type: "integer", nullable: true),
                    SessionPlayerId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TipAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExternalRef = table.Column<string>(type: "text", nullable: true),
                    ReceivedByStaffId = table.Column<int>(type: "integer", nullable: false),
                    CashRegisterShiftId = table.Column<int>(type: "integer", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_CashRegisterShift_CashRegisterShiftId",
                        column: x => x.CashRegisterShiftId,
                        principalTable: "CashRegisterShift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payment_StaffMember_ReceivedByStaffId",
                        column: x => x.ReceivedByStaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    TableId = table.Column<int>(type: "integer", nullable: true),
                    PlayerId = table.Column<int>(type: "integer", nullable: true),
                    ContactName = table.Column<string>(type: "text", nullable: false),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PartySize = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    SessionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservation_BilliardTable_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TableSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    TableId = table.Column<int>(type: "integer", nullable: false),
                    ReservationId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PausedMinutes = table.Column<int>(type: "integer", nullable: false),
                    BilledMinutes = table.Column<int>(type: "integer", nullable: true),
                    TableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ItemsAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OpenedByStaffId = table.Column<int>(type: "integer", nullable: false),
                    ClosedByStaffId = table.Column<int>(type: "integer", nullable: true),
                    VoidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VoidedByStaffId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PricingSnapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableSession_BilliardTable_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSession_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSession_Reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSession_StaffMember_ClosedByStaffId",
                        column: x => x.ClosedByStaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSession_StaffMember_OpenedByStaffId",
                        column: x => x.OpenedByStaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionPlayer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: true),
                    GuestName = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CustomerMembershipId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPlayer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionPlayer_CustomerMembership_CustomerMembershipId",
                        column: x => x.CustomerMembershipId,
                        principalTable: "CustomerMembership",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionPlayer_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionPlayer_TableSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TableSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Association_OrganizationId_Name",
                table: "Association",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BilliardTable_CurrentSessionId",
                table: "BilliardTable",
                column: "CurrentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BilliardTable_OrganizationId_Number",
                table: "BilliardTable",
                columns: new[] { "OrganizationId", "Number" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BilliardTable_OrganizationId_ScoreboardNo",
                table: "BilliardTable",
                columns: new[] { "OrganizationId", "ScoreboardNo" },
                unique: true,
                filter: "\"ScoreboardNo\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BilliardTable_PricingRuleId",
                table: "BilliardTable",
                column: "PricingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterShift_OrganizationId",
                table: "CashRegisterShift",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_City_CountryId",
                table: "City",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_City_OrganizationId_CountryId_Name",
                table: "City",
                columns: new[] { "OrganizationId", "CountryId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Club_OrganizationId",
                table: "Club",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembership_ClubId",
                table: "ClubMembership",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembership_PlayerId",
                table: "ClubMembership",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Country_OrganizationId_Name",
                table: "Country",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cup_OrganizationId",
                table: "Cup",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CupMatch_CupId_Round_Number",
                table: "CupMatch",
                columns: new[] { "CupId", "Round", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CupMatch_ParticipantAId",
                table: "CupMatch",
                column: "ParticipantAId");

            migrationBuilder.CreateIndex(
                name: "IX_CupMatch_ParticipantBId",
                table: "CupMatch",
                column: "ParticipantBId");

            migrationBuilder.CreateIndex(
                name: "IX_CupMatch_TableId",
                table: "CupMatch",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_CupParticipant_CupId_PlayerId",
                table: "CupParticipant",
                columns: new[] { "CupId", "PlayerId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CupParticipant_PlayerId",
                table: "CupParticipant",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CupRuleBlock_CupId",
                table: "CupRuleBlock",
                column: "CupId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMembership_OrganizationId_MemberNo",
                table: "CustomerMembership",
                columns: new[] { "OrganizationId", "MemberNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMembership_PlayerId",
                table: "CustomerMembership",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Device_OrganizationId",
                table: "Device",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Device_PairingCode",
                table: "Device",
                column: "PairingCode",
                unique: true,
                filter: "\"PairingCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Device_TableId",
                table: "Device",
                column: "TableId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inning_MatchId",
                table: "Inning",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_OrganizationId_StartedAt",
                table: "Match",
                columns: new[] { "OrganizationId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Match_RefereeUserId",
                table: "Match",
                column: "RefereeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_ScorekeeperDeviceId",
                table: "Match",
                column: "ScorekeeperDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_SessionId",
                table: "Match",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_StageGroupId",
                table: "Match",
                column: "StageGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_TableId",
                table: "Match",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_TeamFixtureId",
                table: "Match",
                column: "TeamFixtureId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_TournamentStageId",
                table: "Match",
                column: "TournamentStageId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvent_MatchId_Seq",
                table: "MatchEvent",
                columns: new[] { "MatchId", "Seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipant_MatchId_Side",
                table: "MatchParticipant",
                columns: new[] { "MatchId", "Side" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipant_PlayerId",
                table: "MatchParticipant",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSet_MatchId",
                table: "MatchSet",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStat_DeviceId",
                table: "MatchStat",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStat_OrganizationId_PlayedAt",
                table: "MatchStat",
                columns: new[] { "OrganizationId", "PlayedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchStat_TableId",
                table: "MatchStat",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatBucket_MatchStatId_PlayerSlot_BucketIndex",
                table: "MatchStatBucket",
                columns: new[] { "MatchStatId", "PlayerSlot", "BucketIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_ProductId",
                table: "OrderItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_SessionId",
                table: "OrderItem",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_SessionPlayerId",
                table: "OrderItem",
                column: "SessionPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_ClientId",
                table: "Organization",
                column: "ClientId",
                unique: true,
                filter: "\"ClientId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_Code",
                table: "Organization",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organization_DefaultRuleSetId",
                table: "Organization",
                column: "DefaultRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_Slug",
                table: "Organization",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_CashRegisterShiftId",
                table: "Payment",
                column: "CashRegisterShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ReceivedByStaffId",
                table: "Payment",
                column: "ReceivedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_SessionId",
                table: "Payment",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_SessionPlayerId",
                table: "Payment",
                column: "SessionPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_AssociationId",
                table: "Player",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_CityId",
                table: "Player",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_CountryId",
                table: "Player",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_CreatedInOrganizationId",
                table: "Player",
                column: "CreatedInOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_CreatedInOrganizationId_ShortcutNumber",
                table: "Player",
                columns: new[] { "CreatedInOrganizationId", "ShortcutNumber" },
                unique: true,
                filter: "\"ShortcutNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Player_RegionId",
                table: "Player",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_SystemSlot",
                table: "Player",
                column: "SystemSlot",
                unique: true,
                filter: "\"SystemSlot\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Player_UserId",
                table: "Player",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerOrganizationStats_OrganizationId",
                table: "PlayerOrganizationStats",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRule_OrganizationId",
                table: "PricingRule",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryId",
                table: "Product",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_OrganizationId",
                table: "Product",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategory_OrganizationId",
                table: "ProductCategory",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistory_MatchId",
                table: "RatingHistory",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistory_PlayerId",
                table: "RatingHistory",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Region_OrganizationId_Name",
                table: "Region",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_StartAt",
                table: "Reservation",
                columns: new[] { "OrganizationId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_PlayerId",
                table: "Reservation",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_SessionId",
                table: "Reservation",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_TableId",
                table: "Reservation",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_Season_LeagueId",
                table: "Season",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_Season_RuleSetId",
                table: "Season",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTeam_SeasonId",
                table: "SeasonTeam",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTeam_TeamId",
                table: "SeasonTeam",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPlayer_CustomerMembershipId",
                table: "SessionPlayer",
                column: "CustomerMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPlayer_PlayerId",
                table: "SessionPlayer",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPlayer_SessionId",
                table: "SessionPlayer",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMember_OrganizationId_UserId",
                table: "StaffMember",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMember_UserId",
                table: "StaffMember",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StageGroup_StageId",
                table: "StageGroup",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_StageStanding_EntryId",
                table: "StageStanding",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_StageStanding_GroupId",
                table: "StageStanding",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StageStanding_StageId",
                table: "StageStanding",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSession_ClosedByStaffId",
                table: "TableSession",
                column: "ClosedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSession_OpenedByStaffId",
                table: "TableSession",
                column: "OpenedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSession_OrganizationId_OpenedAt",
                table: "TableSession",
                columns: new[] { "OrganizationId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TableSession_ReservationId",
                table: "TableSession",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSession_TableId",
                table: "TableSession",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_ClubId",
                table: "Team",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_HomeOrganizationId",
                table: "Team",
                column: "HomeOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_SeasonId",
                table: "Team",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixture_AwayTeamId",
                table: "TeamFixture",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixture_HomeTeamId",
                table: "TeamFixture",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixture_OrganizationId",
                table: "TeamFixture",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixture_RefereeId",
                table: "TeamFixture",
                column: "RefereeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixture_SeasonId",
                table: "TeamFixture",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_PlayerId",
                table: "TeamMember",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TeamId_PlayerId",
                table: "TeamMember",
                columns: new[] { "TeamId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournament_DefaultRuleSetId",
                table: "Tournament",
                column: "DefaultRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournament_OrganizationId",
                table: "Tournament",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEntry_ClubId",
                table: "TournamentEntry",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEntry_PlayerId",
                table: "TournamentEntry",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEntry_TournamentId",
                table: "TournamentEntry",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStage_RuleSetId",
                table: "TournamentStage",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStage_TournamentId",
                table: "TournamentStage",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_User_Phone",
                table: "User",
                column: "Phone",
                unique: true,
                filter: "\"Phone\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_BilliardTable_TableSession_CurrentSessionId",
                table: "BilliardTable",
                column: "CurrentSessionId",
                principalTable: "TableSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inning_Match_MatchId",
                table: "Inning",
                column: "MatchId",
                principalTable: "Match",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Match_TableSession_SessionId",
                table: "Match",
                column: "SessionId",
                principalTable: "TableSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_SessionPlayer_SessionPlayerId",
                table: "OrderItem",
                column: "SessionPlayerId",
                principalTable: "SessionPlayer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_TableSession_SessionId",
                table: "OrderItem",
                column: "SessionId",
                principalTable: "TableSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_SessionPlayer_SessionPlayerId",
                table: "Payment",
                column: "SessionPlayerId",
                principalTable: "SessionPlayer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_TableSession_SessionId",
                table: "Payment",
                column: "SessionId",
                principalTable: "TableSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_TableSession_SessionId",
                table: "Reservation",
                column: "SessionId",
                principalTable: "TableSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Association_Organization_OrganizationId",
                table: "Association");

            migrationBuilder.DropForeignKey(
                name: "FK_BilliardTable_Organization_OrganizationId",
                table: "BilliardTable");

            migrationBuilder.DropForeignKey(
                name: "FK_City_Organization_OrganizationId",
                table: "City");

            migrationBuilder.DropForeignKey(
                name: "FK_Country_Organization_OrganizationId",
                table: "Country");

            migrationBuilder.DropForeignKey(
                name: "FK_PricingRule_Organization_OrganizationId",
                table: "PricingRule");

            migrationBuilder.DropForeignKey(
                name: "FK_Region_Organization_OrganizationId",
                table: "Region");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_Organization_OrganizationId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMember_Organization_OrganizationId",
                table: "StaffMember");

            migrationBuilder.DropForeignKey(
                name: "FK_TableSession_Organization_OrganizationId",
                table: "TableSession");

            migrationBuilder.DropForeignKey(
                name: "FK_BilliardTable_PricingRule_PricingRuleId",
                table: "BilliardTable");

            migrationBuilder.DropForeignKey(
                name: "FK_BilliardTable_TableSession_CurrentSessionId",
                table: "BilliardTable");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_TableSession_SessionId",
                table: "Reservation");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "ClubMembership");

            migrationBuilder.DropTable(
                name: "CupMatch");

            migrationBuilder.DropTable(
                name: "CupRuleBlock");

            migrationBuilder.DropTable(
                name: "Inning");

            migrationBuilder.DropTable(
                name: "MatchEvent");

            migrationBuilder.DropTable(
                name: "MatchParticipant");

            migrationBuilder.DropTable(
                name: "MatchSet");

            migrationBuilder.DropTable(
                name: "MatchStatBucket");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "PlayerOrganizationStats");

            migrationBuilder.DropTable(
                name: "PlayerStats");

            migrationBuilder.DropTable(
                name: "RatingHistory");

            migrationBuilder.DropTable(
                name: "SeasonTeam");

            migrationBuilder.DropTable(
                name: "StageStanding");

            migrationBuilder.DropTable(
                name: "TeamMember");

            migrationBuilder.DropTable(
                name: "CupParticipant");

            migrationBuilder.DropTable(
                name: "MatchStat");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "CashRegisterShift");

            migrationBuilder.DropTable(
                name: "SessionPlayer");

            migrationBuilder.DropTable(
                name: "Match");

            migrationBuilder.DropTable(
                name: "TournamentEntry");

            migrationBuilder.DropTable(
                name: "Cup");

            migrationBuilder.DropTable(
                name: "ProductCategory");

            migrationBuilder.DropTable(
                name: "CustomerMembership");

            migrationBuilder.DropTable(
                name: "Device");

            migrationBuilder.DropTable(
                name: "StageGroup");

            migrationBuilder.DropTable(
                name: "TeamFixture");

            migrationBuilder.DropTable(
                name: "TournamentStage");

            migrationBuilder.DropTable(
                name: "Team");

            migrationBuilder.DropTable(
                name: "Tournament");

            migrationBuilder.DropTable(
                name: "Club");

            migrationBuilder.DropTable(
                name: "Season");

            migrationBuilder.DropTable(
                name: "League");

            migrationBuilder.DropTable(
                name: "Organization");

            migrationBuilder.DropTable(
                name: "RuleSet");

            migrationBuilder.DropTable(
                name: "PricingRule");

            migrationBuilder.DropTable(
                name: "TableSession");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "StaffMember");

            migrationBuilder.DropTable(
                name: "BilliardTable");

            migrationBuilder.DropTable(
                name: "Player");

            migrationBuilder.DropTable(
                name: "Association");

            migrationBuilder.DropTable(
                name: "City");

            migrationBuilder.DropTable(
                name: "Region");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Country");
        }
    }
}
