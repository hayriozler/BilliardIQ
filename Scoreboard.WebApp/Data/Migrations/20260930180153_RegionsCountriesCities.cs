using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegionsCountriesCities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "PlayerSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "PlayerSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "PlayerSet",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CountrySet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountrySet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CountrySet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegionSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CitySet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitySet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CitySet_CountrySet_CountryId",
                        column: x => x.CountryId,
                        principalTable: "CountrySet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CitySet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_CityId",
                table: "PlayerSet",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_CountryId",
                table: "PlayerSet",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_RegionId",
                table: "PlayerSet",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_CitySet_CountryId",
                table: "CitySet",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_CitySet_OrganizationId_CountryId_Name",
                table: "CitySet",
                columns: new[] { "OrganizationId", "CountryId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CountrySet_OrganizationId_Name",
                table: "CountrySet",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegionSet_OrganizationId_Name",
                table: "RegionSet",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSet_CitySet_CityId",
                table: "PlayerSet",
                column: "CityId",
                principalTable: "CitySet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSet_CountrySet_CountryId",
                table: "PlayerSet",
                column: "CountryId",
                principalTable: "CountrySet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSet_RegionSet_RegionId",
                table: "PlayerSet",
                column: "RegionId",
                principalTable: "RegionSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSet_CitySet_CityId",
                table: "PlayerSet");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSet_CountrySet_CountryId",
                table: "PlayerSet");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSet_RegionSet_RegionId",
                table: "PlayerSet");

            migrationBuilder.DropTable(
                name: "CitySet");

            migrationBuilder.DropTable(
                name: "RegionSet");

            migrationBuilder.DropTable(
                name: "CountrySet");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSet_CityId",
                table: "PlayerSet");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSet_CountryId",
                table: "PlayerSet");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSet_RegionId",
                table: "PlayerSet");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "PlayerSet");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "PlayerSet");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "PlayerSet");
        }
    }
}
