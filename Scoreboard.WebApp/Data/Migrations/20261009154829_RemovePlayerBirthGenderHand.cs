using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlayerBirthGenderHand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "Handedness",
                table: "Player");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "Player",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Player",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Handedness",
                table: "Player",
                type: "integer",
                nullable: true);
        }
    }
}
