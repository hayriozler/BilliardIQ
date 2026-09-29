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
                name: "AuditLogSet",
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
                    table.PrimaryKey("PK_AuditLogSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeagueSet",
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
                    table.PrimaryKey("PK_LeagueSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleSetSet",
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
                    table.PrimaryKey("PK_RuleSetSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSet",
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
                    table.PrimaryKey("PK_UserSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LegalName = table.Column<string>(type: "text", nullable: true),
                    TaxNumber = table.Column<string>(type: "text", nullable: true),
                    TaxOffice = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "text", nullable: true),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_OrganizationSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSet_RuleSetSet_DefaultRuleSetId",
                        column: x => x.DefaultRuleSetId,
                        principalTable: "RuleSetSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeasonSet",
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
                    table.PrimaryKey("PK_SeasonSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonSet_LeagueSet_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "LeagueSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeasonSet_RuleSetSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSetSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    Handedness = table.Column<int>(type: "integer", nullable: true),
                    FederationLicenseNo = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_PlayerSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSet_UserSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UserSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashRegisterShiftSet",
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
                    table.PrimaryKey("PK_CashRegisterShiftSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashRegisterShiftSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubSet",
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
                    table.PrimaryKey("PK_ClubSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PricingRuleSet",
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
                    table.PrimaryKey("PK_PricingRuleSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingRuleSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategorySet",
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
                    table.PrimaryKey("PK_ProductCategorySet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategorySet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffMemberSet",
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
                    table.PrimaryKey("PK_StaffMemberSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffMemberSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffMemberSet_UserSet_UserId",
                        column: x => x.UserId,
                        principalTable: "UserSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentSet",
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
                    table.PrimaryKey("PK_TournamentSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentSet_RuleSetSet_DefaultRuleSetId",
                        column: x => x.DefaultRuleSetId,
                        principalTable: "RuleSetSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerMembershipSet",
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
                    table.PrimaryKey("PK_CustomerMembershipSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerMembershipSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerMembershipSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerOrganizationStatsSet",
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
                    table.PrimaryKey("PK_PlayerOrganizationStatsSet", x => new { x.PlayerId, x.OrganizationId, x.Discipline });
                    table.ForeignKey(
                        name: "FK_PlayerOrganizationStatsSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerOrganizationStatsSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStatsSet",
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
                    table.PrimaryKey("PK_PlayerStatsSet", x => new { x.PlayerId, x.Discipline, x.Scope });
                    table.ForeignKey(
                        name: "FK_PlayerStatsSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClubMembershipSet",
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
                    table.PrimaryKey("PK_ClubMembershipSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubMembershipSet_ClubSet_ClubId",
                        column: x => x.ClubId,
                        principalTable: "ClubSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubMembershipSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamSet",
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSet_ClubSet_ClubId",
                        column: x => x.ClubId,
                        principalTable: "ClubSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamSet_OrganizationSet_HomeOrganizationId",
                        column: x => x.HomeOrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamSet_SeasonSet_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "SeasonSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductSet",
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
                    table.PrimaryKey("PK_ProductSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductSet_ProductCategorySet_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategorySet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentEntrySet",
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
                    table.PrimaryKey("PK_TournamentEntrySet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentEntrySet_ClubSet_ClubId",
                        column: x => x.ClubId,
                        principalTable: "ClubSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentEntrySet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentEntrySet_TournamentSet_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "TournamentSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentStageSet",
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
                    table.PrimaryKey("PK_TournamentStageSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentStageSet_RuleSetSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "RuleSetSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentStageSet_TournamentSet_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "TournamentSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeasonTeamSet",
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
                    table.PrimaryKey("PK_SeasonTeamSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonTeamSet_SeasonSet_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "SeasonSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeasonTeamSet_TeamSet_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TeamSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamFixtureSet",
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
                    table.PrimaryKey("PK_TeamFixtureSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamFixtureSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamFixtureSet_SeasonSet_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "SeasonSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamFixtureSet_TeamSet_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "TeamSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamFixtureSet_TeamSet_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "TeamSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamFixtureSet_UserSet_RefereeId",
                        column: x => x.RefereeId,
                        principalTable: "UserSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamMemberSet",
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
                    table.PrimaryKey("PK_TeamMemberSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMemberSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMemberSet_TeamSet_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TeamSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StageGroupSet",
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
                    table.PrimaryKey("PK_StageGroupSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageGroupSet_TournamentStageSet_StageId",
                        column: x => x.StageId,
                        principalTable: "TournamentStageSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StageStandingSet",
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
                    table.PrimaryKey("PK_StageStandingSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageStandingSet_StageGroupSet_GroupId",
                        column: x => x.GroupId,
                        principalTable: "StageGroupSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StageStandingSet_TournamentEntrySet_EntryId",
                        column: x => x.EntryId,
                        principalTable: "TournamentEntrySet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StageStandingSet_TournamentStageSet_StageId",
                        column: x => x.StageId,
                        principalTable: "TournamentStageSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BilliardTableSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_BilliardTableSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BilliardTableSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BilliardTableSet_PricingRuleSet_PricingRuleId",
                        column: x => x.PricingRuleId,
                        principalTable: "PricingRuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceSet",
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
                    table.PrimaryKey("PK_DeviceSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceSet_BilliardTableSet_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTableSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchStatSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
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
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchStatSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchStatSet_DeviceSet_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DeviceSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InningSet",
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
                    table.PrimaryKey("PK_InningSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchesSet",
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
                    table.PrimaryKey("PK_MatchesSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchesSet_BilliardTableSet_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTableSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchesSet_DeviceSet_ScorekeeperDeviceId",
                        column: x => x.ScorekeeperDeviceId,
                        principalTable: "DeviceSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchesSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchesSet_StageGroupSet_StageGroupId",
                        column: x => x.StageGroupId,
                        principalTable: "StageGroupSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchesSet_TeamFixtureSet_TeamFixtureId",
                        column: x => x.TeamFixtureId,
                        principalTable: "TeamFixtureSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchesSet_TournamentStageSet_TournamentStageId",
                        column: x => x.TournamentStageId,
                        principalTable: "TournamentStageSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchesSet_UserSet_RefereeUserId",
                        column: x => x.RefereeUserId,
                        principalTable: "UserSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchEventSet",
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
                    table.PrimaryKey("PK_MatchEventSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchEventSet_MatchesSet_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MatchesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchParticipantSet",
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
                    table.PrimaryKey("PK_MatchParticipantSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchParticipantSet_MatchesSet_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MatchesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchParticipantSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchSetSet",
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
                    table.PrimaryKey("PK_MatchSetSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchSetSet_MatchesSet_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MatchesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatingHistorySet",
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
                    table.PrimaryKey("PK_RatingHistorySet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingHistorySet_MatchesSet_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MatchesSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RatingHistorySet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemSet",
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
                    table.PrimaryKey("PK_OrderItemSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemSet_ProductSet_ProductId",
                        column: x => x.ProductId,
                        principalTable: "ProductSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSet",
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
                    table.PrimaryKey("PK_PaymentSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSet_CashRegisterShiftSet_CashRegisterShiftId",
                        column: x => x.CashRegisterShiftId,
                        principalTable: "CashRegisterShiftSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentSet_StaffMemberSet_ReceivedByStaffId",
                        column: x => x.ReceivedByStaffId,
                        principalTable: "StaffMemberSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationSet",
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
                    table.PrimaryKey("PK_ReservationSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationSet_BilliardTableSet_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTableSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TableSessionSet",
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
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PricingSnapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableSessionSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableSessionSet_BilliardTableSet_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTableSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSessionSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSessionSet_ReservationSet_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "ReservationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSessionSet_StaffMemberSet_ClosedByStaffId",
                        column: x => x.ClosedByStaffId,
                        principalTable: "StaffMemberSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TableSessionSet_StaffMemberSet_OpenedByStaffId",
                        column: x => x.OpenedByStaffId,
                        principalTable: "StaffMemberSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionPlayerSet",
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
                    table.PrimaryKey("PK_SessionPlayerSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionPlayerSet_CustomerMembershipSet_CustomerMembershipId",
                        column: x => x.CustomerMembershipId,
                        principalTable: "CustomerMembershipSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionPlayerSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionPlayerSet_TableSessionSet_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TableSessionSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BilliardTableSet_CurrentSessionId",
                table: "BilliardTableSet",
                column: "CurrentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BilliardTableSet_OrganizationId_Number",
                table: "BilliardTableSet",
                columns: new[] { "OrganizationId", "Number" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BilliardTableSet_PricingRuleId",
                table: "BilliardTableSet",
                column: "PricingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterShiftSet_OrganizationId",
                table: "CashRegisterShiftSet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembershipSet_ClubId",
                table: "ClubMembershipSet",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembershipSet_PlayerId",
                table: "ClubMembershipSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubSet_OrganizationId",
                table: "ClubSet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMembershipSet_OrganizationId_MemberNo",
                table: "CustomerMembershipSet",
                columns: new[] { "OrganizationId", "MemberNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMembershipSet_PlayerId",
                table: "CustomerMembershipSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSet_OrganizationId",
                table: "DeviceSet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSet_PairingCode",
                table: "DeviceSet",
                column: "PairingCode",
                unique: true,
                filter: "\"PairingCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSet_TableId",
                table: "DeviceSet",
                column: "TableId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InningSet_MatchId",
                table: "InningSet",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchesSet_OrganizationId_StartedAt",
                table: "MatchesSet",
                columns: new[] { "OrganizationId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchesSet_RefereeUserId",
                table: "MatchesSet",
                column: "RefereeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchesSet_ScorekeeperDeviceId",
                table: "MatchesSet",
                column: "ScorekeeperDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchesSet_SessionId",
                table: "MatchesSet",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchesSet_StageGroupId",
                table: "MatchesSet",
                column: "StageGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchesSet_TableId",
                table: "MatchesSet",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchesSet_TeamFixtureId",
                table: "MatchesSet",
                column: "TeamFixtureId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchesSet_TournamentStageId",
                table: "MatchesSet",
                column: "TournamentStageId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEventSet_MatchId_Seq",
                table: "MatchEventSet",
                columns: new[] { "MatchId", "Seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipantSet_MatchId_Side",
                table: "MatchParticipantSet",
                columns: new[] { "MatchId", "Side" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchParticipantSet_PlayerId",
                table: "MatchParticipantSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchSetSet_MatchId",
                table: "MatchSetSet",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatSet_DeviceId",
                table: "MatchStatSet",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemSet_ProductId",
                table: "OrderItemSet",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemSet_SessionId",
                table: "OrderItemSet",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemSet_SessionPlayerId",
                table: "OrderItemSet",
                column: "SessionPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSet_Code",
                table: "OrganizationSet",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSet_DefaultRuleSetId",
                table: "OrganizationSet",
                column: "DefaultRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSet_Slug",
                table: "OrganizationSet",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSet_CashRegisterShiftId",
                table: "PaymentSet",
                column: "CashRegisterShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSet_ReceivedByStaffId",
                table: "PaymentSet",
                column: "ReceivedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSet_SessionId",
                table: "PaymentSet",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSet_SessionPlayerId",
                table: "PaymentSet",
                column: "SessionPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerOrganizationStatsSet_OrganizationId",
                table: "PlayerOrganizationStatsSet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_CreatedInOrganizationId",
                table: "PlayerSet",
                column: "CreatedInOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_UserId",
                table: "PlayerSet",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingRuleSet_OrganizationId",
                table: "PricingRuleSet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategorySet_OrganizationId",
                table: "ProductCategorySet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSet_CategoryId",
                table: "ProductSet",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSet_OrganizationId",
                table: "ProductSet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistorySet_MatchId",
                table: "RatingHistorySet",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingHistorySet_PlayerId",
                table: "RatingHistorySet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSet_OrganizationId_StartAt",
                table: "ReservationSet",
                columns: new[] { "OrganizationId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSet_PlayerId",
                table: "ReservationSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSet_SessionId",
                table: "ReservationSet",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSet_TableId",
                table: "ReservationSet",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonSet_LeagueId",
                table: "SeasonSet",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonSet_RuleSetId",
                table: "SeasonSet",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTeamSet_SeasonId",
                table: "SeasonTeamSet",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonTeamSet_TeamId",
                table: "SeasonTeamSet",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPlayerSet_CustomerMembershipId",
                table: "SessionPlayerSet",
                column: "CustomerMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPlayerSet_PlayerId",
                table: "SessionPlayerSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPlayerSet_SessionId",
                table: "SessionPlayerSet",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMemberSet_OrganizationId_UserId",
                table: "StaffMemberSet",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMemberSet_UserId",
                table: "StaffMemberSet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StageGroupSet_StageId",
                table: "StageGroupSet",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_StageStandingSet_EntryId",
                table: "StageStandingSet",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_StageStandingSet_GroupId",
                table: "StageStandingSet",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StageStandingSet_StageId",
                table: "StageStandingSet",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessionSet_ClosedByStaffId",
                table: "TableSessionSet",
                column: "ClosedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessionSet_OpenedByStaffId",
                table: "TableSessionSet",
                column: "OpenedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessionSet_OrganizationId_OpenedAt",
                table: "TableSessionSet",
                columns: new[] { "OrganizationId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TableSessionSet_ReservationId",
                table: "TableSessionSet",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_TableSessionSet_TableId",
                table: "TableSessionSet",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixtureSet_AwayTeamId",
                table: "TeamFixtureSet",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixtureSet_HomeTeamId",
                table: "TeamFixtureSet",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixtureSet_OrganizationId",
                table: "TeamFixtureSet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixtureSet_RefereeId",
                table: "TeamFixtureSet",
                column: "RefereeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamFixtureSet_SeasonId",
                table: "TeamFixtureSet",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberSet_PlayerId",
                table: "TeamMemberSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMemberSet_TeamId_PlayerId",
                table: "TeamMemberSet",
                columns: new[] { "TeamId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamSet_ClubId",
                table: "TeamSet",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSet_HomeOrganizationId",
                table: "TeamSet",
                column: "HomeOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSet_SeasonId",
                table: "TeamSet",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEntrySet_ClubId",
                table: "TournamentEntrySet",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEntrySet_PlayerId",
                table: "TournamentEntrySet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEntrySet_TournamentId",
                table: "TournamentEntrySet",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentSet_DefaultRuleSetId",
                table: "TournamentSet",
                column: "DefaultRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentSet_OrganizationId",
                table: "TournamentSet",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStageSet_RuleSetId",
                table: "TournamentStageSet",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStageSet_TournamentId",
                table: "TournamentStageSet",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSet_Email",
                table: "UserSet",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSet_Phone",
                table: "UserSet",
                column: "Phone",
                unique: true,
                filter: "\"Phone\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_BilliardTableSet_TableSessionSet_CurrentSessionId",
                table: "BilliardTableSet",
                column: "CurrentSessionId",
                principalTable: "TableSessionSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InningSet_MatchesSet_MatchId",
                table: "InningSet",
                column: "MatchId",
                principalTable: "MatchesSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchesSet_TableSessionSet_SessionId",
                table: "MatchesSet",
                column: "SessionId",
                principalTable: "TableSessionSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItemSet_SessionPlayerSet_SessionPlayerId",
                table: "OrderItemSet",
                column: "SessionPlayerId",
                principalTable: "SessionPlayerSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItemSet_TableSessionSet_SessionId",
                table: "OrderItemSet",
                column: "SessionId",
                principalTable: "TableSessionSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSet_SessionPlayerSet_SessionPlayerId",
                table: "PaymentSet",
                column: "SessionPlayerId",
                principalTable: "SessionPlayerSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSet_TableSessionSet_SessionId",
                table: "PaymentSet",
                column: "SessionId",
                principalTable: "TableSessionSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationSet_TableSessionSet_SessionId",
                table: "ReservationSet",
                column: "SessionId",
                principalTable: "TableSessionSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BilliardTableSet_OrganizationSet_OrganizationId",
                table: "BilliardTableSet");

            migrationBuilder.DropForeignKey(
                name: "FK_PricingRuleSet_OrganizationSet_OrganizationId",
                table: "PricingRuleSet");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationSet_OrganizationSet_OrganizationId",
                table: "ReservationSet");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMemberSet_OrganizationSet_OrganizationId",
                table: "StaffMemberSet");

            migrationBuilder.DropForeignKey(
                name: "FK_TableSessionSet_OrganizationSet_OrganizationId",
                table: "TableSessionSet");

            migrationBuilder.DropForeignKey(
                name: "FK_BilliardTableSet_PricingRuleSet_PricingRuleId",
                table: "BilliardTableSet");

            migrationBuilder.DropForeignKey(
                name: "FK_BilliardTableSet_TableSessionSet_CurrentSessionId",
                table: "BilliardTableSet");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationSet_TableSessionSet_SessionId",
                table: "ReservationSet");

            migrationBuilder.DropTable(
                name: "AuditLogSet");

            migrationBuilder.DropTable(
                name: "ClubMembershipSet");

            migrationBuilder.DropTable(
                name: "InningSet");

            migrationBuilder.DropTable(
                name: "MatchEventSet");

            migrationBuilder.DropTable(
                name: "MatchParticipantSet");

            migrationBuilder.DropTable(
                name: "MatchSetSet");

            migrationBuilder.DropTable(
                name: "MatchStatSet");

            migrationBuilder.DropTable(
                name: "OrderItemSet");

            migrationBuilder.DropTable(
                name: "PaymentSet");

            migrationBuilder.DropTable(
                name: "PlayerOrganizationStatsSet");

            migrationBuilder.DropTable(
                name: "PlayerStatsSet");

            migrationBuilder.DropTable(
                name: "RatingHistorySet");

            migrationBuilder.DropTable(
                name: "SeasonTeamSet");

            migrationBuilder.DropTable(
                name: "StageStandingSet");

            migrationBuilder.DropTable(
                name: "TeamMemberSet");

            migrationBuilder.DropTable(
                name: "ProductSet");

            migrationBuilder.DropTable(
                name: "CashRegisterShiftSet");

            migrationBuilder.DropTable(
                name: "SessionPlayerSet");

            migrationBuilder.DropTable(
                name: "MatchesSet");

            migrationBuilder.DropTable(
                name: "TournamentEntrySet");

            migrationBuilder.DropTable(
                name: "ProductCategorySet");

            migrationBuilder.DropTable(
                name: "CustomerMembershipSet");

            migrationBuilder.DropTable(
                name: "DeviceSet");

            migrationBuilder.DropTable(
                name: "StageGroupSet");

            migrationBuilder.DropTable(
                name: "TeamFixtureSet");

            migrationBuilder.DropTable(
                name: "TournamentStageSet");

            migrationBuilder.DropTable(
                name: "TeamSet");

            migrationBuilder.DropTable(
                name: "TournamentSet");

            migrationBuilder.DropTable(
                name: "ClubSet");

            migrationBuilder.DropTable(
                name: "SeasonSet");

            migrationBuilder.DropTable(
                name: "LeagueSet");

            migrationBuilder.DropTable(
                name: "OrganizationSet");

            migrationBuilder.DropTable(
                name: "RuleSetSet");

            migrationBuilder.DropTable(
                name: "PricingRuleSet");

            migrationBuilder.DropTable(
                name: "TableSessionSet");

            migrationBuilder.DropTable(
                name: "ReservationSet");

            migrationBuilder.DropTable(
                name: "StaffMemberSet");

            migrationBuilder.DropTable(
                name: "BilliardTableSet");

            migrationBuilder.DropTable(
                name: "PlayerSet");

            migrationBuilder.DropTable(
                name: "UserSet");
        }
    }
}
