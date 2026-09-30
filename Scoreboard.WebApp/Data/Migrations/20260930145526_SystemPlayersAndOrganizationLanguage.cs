using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SystemPlayersAndOrganizationLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "PlayerSet",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SystemSlot",
                table: "PlayerSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "OrganizationSet",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "tr");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_CreatedInOrganizationId_SystemSlot",
                table: "PlayerSet",
                columns: new[] { "CreatedInOrganizationId", "SystemSlot" },
                unique: true,
                filter: "\"SystemSlot\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerSet_CreatedInOrganizationId_SystemSlot",
                table: "PlayerSet");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "PlayerSet");

            migrationBuilder.DropColumn(
                name: "SystemSlot",
                table: "PlayerSet");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "OrganizationSet");
        }
    }
}
