using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scoreboard.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<int>(type: "integer", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhotoPath = table.Column<string>(type: "text", nullable: true),
                    AvatarId = table.Column<int>(type: "integer", nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    BaseCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaseCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSet_ClubSet_ClubId",
                        column: x => x.ClubId,
                        principalTable: "ClubSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoreboardClientSet",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClubId = table.Column<int>(type: "integer", nullable: true),
                    TableNumber = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreboardClientSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreboardClientSet_ClubSet_ClubId",
                        column: x => x.ClubId,
                        principalTable: "ClubSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClubId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSet_ClubSet_ClubId",
                        column: x => x.ClubId,
                        principalTable: "ClubSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchStatSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                        name: "FK_MatchStatSet_ScoreboardClientSet_ClientId",
                        column: x => x.ClientId,
                        principalTable: "ScoreboardClientSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamPlayerSet",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamPlayerSet", x => new { x.TeamId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_TeamPlayerSet_PlayerSet_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamPlayerSet_TeamSet_TeamId",
                        column: x => x.TeamId,
                        principalTable: "TeamSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubSet_Code",
                table: "ClubSet",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatSet_ClientId",
                table: "MatchStatSet",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_ClubId",
                table: "PlayerSet",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreboardClientSet_ClubId",
                table: "ScoreboardClientSet",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPlayerSet_PlayerId",
                table: "TeamPlayerSet",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSet_ClubId",
                table: "TeamSet",
                column: "ClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchStatSet");

            migrationBuilder.DropTable(
                name: "TeamPlayerSet");

            migrationBuilder.DropTable(
                name: "ScoreboardClientSet");

            migrationBuilder.DropTable(
                name: "PlayerSet");

            migrationBuilder.DropTable(
                name: "TeamSet");

            migrationBuilder.DropTable(
                name: "ClubSet");
        }
    }
}
