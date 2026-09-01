using Ibtikar.Data;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data
{
    // Ibtikar audit feature schema upgrade.
    // The remote database predates EF migrations, so new columns/table are
    // applied idempotently here on startup rather than via a migration file.
    // All DDL is static and contains no request-derived values.
    public static class AuditSchemaUpgrader
    {
        public static void EnsureAuditSchema(IbtikarDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AssignedDepartmentId\" uuid NULL; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AuditEmployeeId\" uuid NULL; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AuditAssignedAt\" timestamp with time zone NULL; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"IsDeleted\" boolean NOT NULL DEFAULT FALSE; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"DeletedAt\" timestamp with time zone NULL; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"RequiredResources\" text NULL;");

            db.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS \"AuditActionItems\" (" +
                "\"Id\" uuid NOT NULL, " +
                "\"IdeaId\" uuid NOT NULL, " +
                "\"Decision\" text NOT NULL, " +
                "\"DecisionText\" text NULL, " +
                "\"TargetDepartmentId\" uuid NULL, " +
                "\"AuditorId\" uuid NOT NULL, " +
                "\"AuditDate\" timestamp with time zone NOT NULL, " +
                "CONSTRAINT \"PK_AuditActionItems\" PRIMARY KEY (\"Id\"), " +
                "CONSTRAINT \"FK_AuditActionItems_InnovationIdeas_IdeaId\" FOREIGN KEY (\"IdeaId\") REFERENCES \"InnovationIdeas\" (\"Id\") ON DELETE CASCADE);");

            db.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS \"IX_AuditActionItems_IdeaId\" ON \"AuditActionItems\" (\"IdeaId\");");
        }
    }
}
