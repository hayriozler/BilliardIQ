using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropOrganizationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organization_Code",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Organization");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Organization",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_Code",
                table: "Organization",
                column: "Code",
                unique: true);
        }
    }
}
