using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationClientIdAndMatchStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "OrganizationSet",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DeviceId",
                table: "MatchStatSet",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "BucketMinutes",
                table: "MatchStatSet",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "MatchStatSet",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "MatchStatSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "MatchStatSet",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TableId",
                table: "MatchStatSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TableNo",
                table: "MatchStatSet",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatchStatBucketSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchStatId = table.Column<int>(type: "integer", nullable: false),
                    PlayerSlot = table.Column<int>(type: "integer", nullable: false),
                    BucketIndex = table.Column<int>(type: "integer", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchStatBucketSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchStatBucketSet_MatchStatSet_MatchStatId",
                        column: x => x.MatchStatId,
                        principalTable: "MatchStatSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSet_ClientId",
                table: "OrganizationSet",
                column: "ClientId",
                unique: true,
                filter: "\"ClientId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatSet_OrganizationId_PlayedAt",
                table: "MatchStatSet",
                columns: new[] { "OrganizationId", "PlayedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatSet_TableId",
                table: "MatchStatSet",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatBucketSet_MatchStatId_PlayerSlot_BucketIndex",
                table: "MatchStatBucketSet",
                columns: new[] { "MatchStatId", "PlayerSlot", "BucketIndex" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchStatSet_BilliardTableSet_TableId",
                table: "MatchStatSet",
                column: "TableId",
                principalTable: "BilliardTableSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchStatSet_OrganizationSet_OrganizationId",
                table: "MatchStatSet",
                column: "OrganizationId",
                principalTable: "OrganizationSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchStatSet_BilliardTableSet_TableId",
                table: "MatchStatSet");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchStatSet_OrganizationSet_OrganizationId",
                table: "MatchStatSet");

            migrationBuilder.DropTable(
                name: "MatchStatBucketSet");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationSet_ClientId",
                table: "OrganizationSet");

            migrationBuilder.DropIndex(
                name: "IX_MatchStatSet_OrganizationId_PlayedAt",
                table: "MatchStatSet");

            migrationBuilder.DropIndex(
                name: "IX_MatchStatSet_TableId",
                table: "MatchStatSet");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "OrganizationSet");

            migrationBuilder.DropColumn(
                name: "BucketMinutes",
                table: "MatchStatSet");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "MatchStatSet");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "MatchStatSet");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "MatchStatSet");

            migrationBuilder.DropColumn(
                name: "TableId",
                table: "MatchStatSet");

            migrationBuilder.DropColumn(
                name: "TableNo",
                table: "MatchStatSet");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceId",
                table: "MatchStatSet",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
