using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerTableChangeOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"EntityChange\"; DELETE FROM \"ClientSync\";");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EntityChange",
                table: "EntityChange");

            migrationBuilder.DropIndex(
                name: "IX_EntityChange_OrganizationId_Seq",
                table: "EntityChange");

            migrationBuilder.DropColumn(
                name: "AckedSeq",
                table: "ClientSync");

            migrationBuilder.DropColumn(
                name: "PendingSeq",
                table: "ClientSync");

            migrationBuilder.RenameColumn(
                name: "TableNo",
                table: "ClientSync",
                newName: "TableId");

            migrationBuilder.AddColumn<int>(
                name: "TableId",
                table: "EntityChange",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PendingKeys",
                table: "ClientSync",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EntityChange",
                table: "EntityChange",
                columns: new[] { "OrganizationId", "TableId", "EntityName", "EntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EntityChange",
                table: "EntityChange");

            migrationBuilder.DropColumn(
                name: "TableId",
                table: "EntityChange");

            migrationBuilder.DropColumn(
                name: "PendingKeys",
                table: "ClientSync");

            migrationBuilder.RenameColumn(
                name: "TableId",
                table: "ClientSync",
                newName: "TableNo");

            migrationBuilder.AddColumn<long>(
                name: "AckedSeq",
                table: "ClientSync",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PendingSeq",
                table: "ClientSync",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EntityChange",
                table: "EntityChange",
                columns: new[] { "OrganizationId", "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityChange_OrganizationId_Seq",
                table: "EntityChange",
                columns: new[] { "OrganizationId", "Seq" });
        }
    }
}
