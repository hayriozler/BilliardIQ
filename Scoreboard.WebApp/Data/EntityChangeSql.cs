namespace Scoreboard.WebApp.Data;

public static class EntityChangeSql
{
    public static FormattableString Upsert(int organizationId, string entity, int id, bool deleted) =>
        $"""
        INSERT INTO "EntityChange" ("OrganizationId", "TableId", "EntityName", "EntityId", "IsDeleted", "ChangedAt", "Seq")
        SELECT {organizationId}, t."Id", {entity}, {id}, {deleted}, now(), nextval('"EntityChangeSeq"')
        FROM "BilliardTable" t
        WHERE t."OrganizationId" = {organizationId} AND t."ScoreboardNo" IS NOT NULL AND t."DeletedAt" IS NULL
        ON CONFLICT ("OrganizationId", "TableId", "EntityName", "EntityId")
        DO UPDATE SET "IsDeleted" = EXCLUDED."IsDeleted", "ChangedAt" = EXCLUDED."ChangedAt", "Seq" = EXCLUDED."Seq"
        """;

    public static FormattableString Remove(int organizationId, int tableId, string[] entities, int[] ids, long[] seqs) =>
        $"""
        DELETE FROM "EntityChange" e
        USING unnest({entities}::text[], {ids}::integer[], {seqs}::bigint[]) AS k(entity, id, seq)
        WHERE e."OrganizationId" = {organizationId} AND e."TableId" = {tableId}
          AND e."EntityName" = k.entity AND e."EntityId" = k.id AND e."Seq" = k.seq
        """;
}
