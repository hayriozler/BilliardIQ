using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalMatch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    PlayedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    OpponentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Venue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    OpponentScore = table.Column<int>(type: "integer", nullable: false),
                    Innings = table.Column<int>(type: "integer", nullable: false),
                    HighRun = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalMatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalMatch_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Player",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMatch_OrganizationId_PlayerId_PlayedOn",
                table: "ExternalMatch",
                columns: new[] { "OrganizationId", "PlayerId", "PlayedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalMatch_PlayerId",
                table: "ExternalMatch",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalMatch");
        }
    }
}
