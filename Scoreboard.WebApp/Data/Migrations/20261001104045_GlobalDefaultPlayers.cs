using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class GlobalDefaultPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GlobalDefaultPlayers_Prepare
            // Player 1 and Player 2 become two global rows with Id 1 and 2. Real players that sit on those ids move to new ids
            // (every table that points at them follows), the per-venue system players are dropped, and the two rows are created.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    fk record;
    old_id integer;
    new_id integer;
    seq text := pg_get_serial_sequence('""PlayerSet""', 'Id');
BEGIN
    FOR fk IN SELECT c.conname, c.conrelid::regclass::text AS child
              FROM pg_constraint c WHERE c.contype = 'f' AND c.confrelid = '""PlayerSet""'::regclass
    LOOP
        EXECUTE format('ALTER TABLE %s ALTER CONSTRAINT %I DEFERRABLE INITIALLY DEFERRED', fk.child, fk.conname);
    END LOOP;

    FOR old_id IN SELECT ""Id"" FROM ""PlayerSet"" WHERE ""Id"" IN (1, 2) AND NOT ""IsSystem"" ORDER BY ""Id"" LOOP
        new_id := nextval(seq);
        UPDATE ""PlayerSet"" SET ""Id"" = new_id WHERE ""Id"" = old_id;
        FOR fk IN SELECT c.conrelid::regclass::text AS child, a.attname AS col
                  FROM pg_constraint c JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = ANY (c.conkey)
                  WHERE c.contype = 'f' AND c.confrelid = '""PlayerSet""'::regclass
        LOOP
            EXECUTE format('UPDATE %s SET %I = %s WHERE %I = %s', fk.child, fk.col, new_id, fk.col, old_id);
        END LOOP;
    END LOOP;

    SET CONSTRAINTS ALL IMMEDIATE;

    FOR fk IN SELECT c.conname, c.conrelid::regclass::text AS child
              FROM pg_constraint c WHERE c.contype = 'f' AND c.confrelid = '""PlayerSet""'::regclass
    LOOP
        EXECUTE format('ALTER TABLE %s ALTER CONSTRAINT %I NOT DEFERRABLE', fk.child, fk.conname);
    END LOOP;
END $$;

-- results recorded with a venue's own default players now point at the global ones (slot = id)
UPDATE ""MatchStatSet"" m SET ""Player1ExternalId"" = p.""SystemSlot"" FROM ""PlayerSet"" p WHERE m.""Player1ExternalId"" = p.""Id"" AND p.""IsSystem"" AND p.""Id"" > 2;
UPDATE ""MatchStatSet"" m SET ""Player2ExternalId"" = p.""SystemSlot"" FROM ""PlayerSet"" p WHERE m.""Player2ExternalId"" = p.""Id"" AND p.""IsSystem"" AND p.""Id"" > 2;
DELETE FROM ""PlayerSet"" WHERE ""IsSystem"" AND ""Id"" > 2;
UPDATE ""PlayerSet"" SET ""CreatedInOrganizationId"" = NULL WHERE ""IsSystem"" AND ""Id"" IN (1, 2);

INSERT INTO ""PlayerSet"" (""Id"", ""FirstName"", ""LastName"", ""DisplayName"", ""IsGuest"", ""IsPublicProfile"", ""Level"", ""IsSystem"", ""SystemSlot"", ""CreatedAt"", ""UpdatedAt"")
VALUES (1, 'Oyuncu 1', '', 'Oyuncu 1', TRUE, FALSE, 2, TRUE, 1, now(), now()),
       (2, 'Oyuncu 2', '', 'Oyuncu 2', TRUE, FALSE, 2, TRUE, 2, now(), now())
ON CONFLICT (""Id"") DO NOTHING;

SELECT setval(pg_get_serial_sequence('""PlayerSet""', 'Id'), GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""PlayerSet""), 2));
");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSet_CreatedInOrganizationId_SystemSlot",
                table: "PlayerSet");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_SystemSlot",
                table: "PlayerSet",
                column: "SystemSlot",
                unique: true,
                filter: "\"SystemSlot\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlayerSet_ReservedIds",
                table: "PlayerSet",
                sql: "\"IsSystem\" OR \"Id\" > 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerSet_SystemSlot",
                table: "PlayerSet");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlayerSet_ReservedIds",
                table: "PlayerSet");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSet_CreatedInOrganizationId_SystemSlot",
                table: "PlayerSet",
                columns: new[] { "CreatedInOrganizationId", "SystemSlot" },
                unique: true,
                filter: "\"SystemSlot\" IS NOT NULL");
        }
    }
}
