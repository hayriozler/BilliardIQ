using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "OrganizationSet",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);
            // Salons created before the country was asked are treated as Turkish; fix individual salons afterwards.
            migrationBuilder.Sql(@"UPDATE ""OrganizationSet"" SET ""CountryCode"" = 'TR' WHERE ""CountryCode"" IS NULL;");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "OrganizationSet");
        }
    }
}
