using Ibtikar.Data;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data
{
    // Ibtikar audit feature & admin table schema upgrade.
    // Applied idempotently on startup.
    public static class AuditSchemaUpgrader
    {
        public static void EnsureAuditSchema(IbtikarDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AssignedDepartmentId\" uuid NULL; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AuditEmployeeId\" uuid NULL; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AuditAssignedAt\" timestamp with time zone NULL; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"RequiredResources\" text NULL; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"IsDeleted\" boolean NOT NULL DEFAULT FALSE; " +
                "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"DeletedAt\" timestamp with time zone NULL;");

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

            db.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS \"Admins\" (" +
                "\"Id\" SERIAL PRIMARY KEY, " +
                "\"NetworkUser\" varchar(150) NOT NULL UNIQUE, " +
                "\"DeptId\" integer NULL, " +
                "\"RoleId\" uuid NOT NULL, " +
                "\"IsActive\" boolean NOT NULL DEFAULT TRUE, " +
                "\"CreatedAt\" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP);");

            db.Database.ExecuteSqlRaw(
                "INSERT INTO \"Roles\" (\"Id\", \"Code\", \"Name\", \"Description\", \"IsActive\", \"CreatedAt\") " +
                "SELECT gen_random_uuid(), 'AuditEmployee', 'موظف تدقيق', 'Audit Employee', true, CURRENT_TIMESTAMP " +
                "WHERE NOT EXISTS (SELECT 1 FROM \"Roles\" WHERE \"Code\" = 'AuditEmployee'); " +
                "INSERT INTO \"Roles\" (\"Id\", \"Code\", \"Name\", \"Description\", \"IsActive\", \"CreatedAt\") " +
                "SELECT gen_random_uuid(), 'SpecializedDepartment', 'الإدارة المختصة', 'Specialized Department', true, CURRENT_TIMESTAMP " +
                "WHERE NOT EXISTS (SELECT 1 FROM \"Roles\" WHERE \"Code\" = 'SpecializedDepartment'); " +
                "INSERT INTO \"Roles\" (\"Id\", \"Code\", \"Name\", \"Description\", \"IsActive\", \"CreatedAt\") " +
                "SELECT gen_random_uuid(), 'InnovationCommitteeMember', 'عضو لجنة الابتكار', 'Innovation Committee Member', true, CURRENT_TIMESTAMP " +
                "WHERE NOT EXISTS (SELECT 1 FROM \"Roles\" WHERE \"Code\" = 'InnovationCommitteeMember'); " +
                "INSERT INTO \"Roles\" (\"Id\", \"Code\", \"Name\", \"Description\", \"IsActive\", \"CreatedAt\") " +
                "SELECT gen_random_uuid(), 'admin', 'مدير النظام', 'Admin', true, CURRENT_TIMESTAMP " +
                "WHERE NOT EXISTS (SELECT 1 FROM \"Roles\" WHERE \"Code\" = 'admin');");
        }
    }
}
