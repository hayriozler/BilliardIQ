using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerShortcutNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShortcutNumber",
                table: "PlayerSet",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_CreatedInOrganizationId_ShortcutNumber",
                table: "PlayerSet",
                columns: new[] { "CreatedInOrganizationId", "ShortcutNumber" },
                unique: true,
                filter: "\"ShortcutNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerSet_CreatedInOrganizationId_ShortcutNumber",
                table: "PlayerSet");

            migrationBuilder.DropColumn(
                name: "ShortcutNumber",
                table: "PlayerSet");
        }
    }
}
