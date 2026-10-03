using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHandicap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHandicap",
                table: "MatchStat",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Player1Target",
                table: "MatchStat",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Player2Target",
                table: "MatchStat",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHandicap",
                table: "MatchStat");

            migrationBuilder.DropColumn(
                name: "Player1Target",
                table: "MatchStat");

            migrationBuilder.DropColumn(
                name: "Player2Target",
                table: "MatchStat");
        }
    }
}
