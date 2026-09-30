using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class TournamentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CupSet",
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
                    table.PrimaryKey("PK_CupSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CupSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CupParticipantSet",
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
                    table.PrimaryKey("PK_CupParticipantSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CupParticipantSet_CupSet_CupId",
                        column: x => x.CupId,
                        principalTable: "CupSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CupParticipantSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CupRuleBlockSet",
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
                    table.PrimaryKey("PK_CupRuleBlockSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CupRuleBlockSet_CupSet_CupId",
                        column: x => x.CupId,
                        principalTable: "CupSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CupMatchSet",
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
                    table.PrimaryKey("PK_CupMatchSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CupMatchSet_BilliardTableSet_TableId",
                        column: x => x.TableId,
                        principalTable: "BilliardTableSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CupMatchSet_CupParticipantSet_ParticipantAId",
                        column: x => x.ParticipantAId,
                        principalTable: "CupParticipantSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CupMatchSet_CupParticipantSet_ParticipantBId",
                        column: x => x.ParticipantBId,
                        principalTable: "CupParticipantSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CupMatchSet_CupSet_CupId",
                        column: x => x.CupId,
                        principalTable: "CupSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CupMatchSet_CupId_Round_Number",
                table: "CupMatchSet",
                columns: new[] { "CupId", "Round", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CupMatchSet_ParticipantAId",
                table: "CupMatchSet",
                column: "ParticipantAId");

            migrationBuilder.CreateIndex(
                name: "IX_CupMatchSet_ParticipantBId",
                table: "CupMatchSet",
                column: "ParticipantBId");

            migrationBuilder.CreateIndex(
                name: "IX_CupMatchSet_TableId",
                table: "CupMatchSet",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_CupParticipantSet_CupId_PlayerId",
                table: "CupParticipantSet",
                columns: new[] { "CupId", "PlayerId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CupParticipantSet_PlayerId",
                table: "CupParticipantSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CupRuleBlockSet_CupId",
                table: "CupRuleBlockSet",
                column: "CupId");

            migrationBuilder.CreateIndex(
                name: "IX_CupSet_OrganizationId",
                table: "CupSet",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CupMatchSet");

            migrationBuilder.DropTable(
                name: "CupRuleBlockSet");

            migrationBuilder.DropTable(
                name: "CupParticipantSet");

            migrationBuilder.DropTable(
                name: "CupSet");
        }
    }
}
