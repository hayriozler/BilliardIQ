using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class EntityChangeLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "EntityChangeSeq");

            migrationBuilder.CreateTable(
                name: "EntityChange",
                columns: table => new
                {
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    Seq = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('\"EntityChangeSeq\"')"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityChange", x => new { x.OrganizationId, x.EntityName, x.EntityId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityChange_OrganizationId_Seq",
                table: "EntityChange",
                columns: new[] { "OrganizationId", "Seq" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityChange");

            migrationBuilder.DropSequence(
                name: "EntityChangeSeq");
        }
    }
}
