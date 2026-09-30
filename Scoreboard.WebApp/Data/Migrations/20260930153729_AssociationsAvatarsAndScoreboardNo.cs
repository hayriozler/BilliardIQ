using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssociationsAvatarsAndScoreboardNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvatarId",
                table: "TeamSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssociationId",
                table: "PlayerSet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreboardNo",
                table: "BilliardTableSet",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssociationSet",
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
                    table.PrimaryKey("PK_AssociationSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationSet_OrganizationSet_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Free-text associations typed so far become entries of the new per-salon list, and players are linked to them.
            migrationBuilder.Sql(@"
                INSERT INTO ""AssociationSet"" (""OrganizationId"", ""Name"", ""CreatedAt"", ""UpdatedAt"")
                SELECT DISTINCT p.""CreatedInOrganizationId"", btrim(p.""AssociationName""), now(), now()
                FROM ""PlayerSet"" p
                WHERE p.""CreatedInOrganizationId"" IS NOT NULL
                  AND p.""AssociationName"" IS NOT NULL AND btrim(p.""AssociationName"") <> '';

                UPDATE ""PlayerSet"" p
                SET ""AssociationId"" = a.""Id""
                FROM ""AssociationSet"" a
                WHERE a.""OrganizationId"" = p.""CreatedInOrganizationId""
                  AND a.""Name"" = btrim(p.""AssociationName"");");

            migrationBuilder.DropColumn(
                name: "AssociationName",
                table: "PlayerSet");

            // Tables that existed before this migration keep working with their current number as scoreboard number.
            migrationBuilder.Sql(@"UPDATE ""BilliardTableSet"" SET ""ScoreboardNo"" = ""Number"" WHERE ""DeletedAt"" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_AssociationId",
                table: "PlayerSet",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_BilliardTableSet_OrganizationId_ScoreboardNo",
                table: "BilliardTableSet",
                columns: new[] { "OrganizationId", "ScoreboardNo" },
                unique: true,
                filter: "\"ScoreboardNo\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationSet_OrganizationId_Name",
                table: "AssociationSet",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSet_AssociationSet_AssociationId",
                table: "PlayerSet",
                column: "AssociationId",
                principalTable: "AssociationSet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSet_AssociationSet_AssociationId",
                table: "PlayerSet");

            migrationBuilder.DropTable(
                name: "AssociationSet");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSet_AssociationId",
                table: "PlayerSet");

            migrationBuilder.DropIndex(
                name: "IX_BilliardTableSet_OrganizationId_ScoreboardNo",
                table: "BilliardTableSet");

            migrationBuilder.DropColumn(
                name: "AvatarId",
                table: "TeamSet");

            migrationBuilder.DropColumn(
                name: "AssociationId",
                table: "PlayerSet");

            migrationBuilder.DropColumn(
                name: "ScoreboardNo",
                table: "BilliardTableSet");

            migrationBuilder.AddColumn<string>(
                name: "AssociationName",
                table: "PlayerSet",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }
    }
}
