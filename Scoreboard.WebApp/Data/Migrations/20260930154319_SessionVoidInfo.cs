using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SessionVoidInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VoidedAt",
                table: "TableSessionSet",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoidedByStaffId",
                table: "TableSessionSet",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "TableSessionSet");

            migrationBuilder.DropColumn(
                name: "VoidedByStaffId",
                table: "TableSessionSet");
        }
    }
}
