using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClientSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientSync",
                columns: table => new
                {
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    TableNo = table.Column<int>(type: "integer", nullable: false),
                    InstanceId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AckedSeq = table.Column<long>(type: "bigint", nullable: false),
                    PendingSeq = table.Column<long>(type: "bigint", nullable: false),
                    PendingFull = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastFullSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSync", x => new { x.OrganizationId, x.TableNo });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientSync");
        }
    }
}
