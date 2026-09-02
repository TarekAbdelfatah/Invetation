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
            if (db.Database.IsSqlServer())
            {
                db.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[InnovationIdeas]') AND name = N'AssignedDepartmentId')
                        ALTER TABLE [InnovationIdeas] ADD [AssignedDepartmentId] uniqueidentifier NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[InnovationIdeas]') AND name = N'AuditEmployeeId')
                        ALTER TABLE [InnovationIdeas] ADD [AuditEmployeeId] uniqueidentifier NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[InnovationIdeas]') AND name = N'AuditAssignedAt')
                        ALTER TABLE [InnovationIdeas] ADD [AuditAssignedAt] datetime2 NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[AuditActionItems]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [AuditActionItems] (
                            [Id] uniqueidentifier NOT NULL,
                            [IdeaId] uniqueidentifier NOT NULL,
                            [Decision] nvarchar(max) NOT NULL,
                            [DecisionText] nvarchar(max) NULL,
                            [TargetDepartmentId] uniqueidentifier NULL,
                            [AuditorId] uniqueidentifier NOT NULL,
                            [AuditDate] datetime2 NOT NULL,
                            CONSTRAINT [PK_AuditActionItems] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_AuditActionItems_InnovationIdeas_IdeaId] FOREIGN KEY ([IdeaId]) REFERENCES [InnovationIdeas] ([Id]) ON DELETE CASCADE
                        );
                        CREATE INDEX [IX_AuditActionItems_IdeaId] ON [AuditActionItems] ([IdeaId]);
                    END

                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[Admins]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [Admins] (
                            [Id] int IDENTITY(1,1) NOT NULL,
                            [NetworkUser] nvarchar(150) NOT NULL,
                            [DeptId] int NULL,
                            [RoleId] uniqueidentifier NOT NULL,
                            [IsActive] bit NOT NULL DEFAULT 1,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_Admins] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_Admins_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
                        );
                        CREATE UNIQUE INDEX [IX_Admins_NetworkUser] ON [Admins] ([NetworkUser]);
                    END

                    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Code] = N'AuditEmployee')
                        INSERT INTO [Roles] ([Id], [Code], [Name], [Description], [IsActive], [CreatedAt]) VALUES (NEWID(), N'AuditEmployee', N'موظف تدقيق', N'Audit Employee', 1, GETUTCDATE());

                    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Code] = N'SpecializedDepartment')
                        INSERT INTO [Roles] ([Id], [Code], [Name], [Description], [IsActive], [CreatedAt]) VALUES (NEWID(), N'SpecializedDepartment', N'الإدارة المختصة', N'Specialized Department', 1, GETUTCDATE());

                    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Code] = N'InnovationCommitteeMember')
                        INSERT INTO [Roles] ([Id], [Code], [Name], [Description], [IsActive], [CreatedAt]) VALUES (NEWID(), N'InnovationCommitteeMember', N'عضو لجنة الابتكار', N'Innovation Committee Member', 1, GETUTCDATE());

                    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Code] = N'admin')
                        INSERT INTO [Roles] ([Id], [Code], [Name], [Description], [IsActive], [CreatedAt]) VALUES (NEWID(), N'admin', N'مدير النظام', N'Admin', 1, GETUTCDATE());
                ");
            }
            else
            {
                db.Database.ExecuteSqlRaw(
                    "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AssignedDepartmentId\" uuid NULL; " +
                    "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AuditEmployeeId\" uuid NULL; " +
                    "ALTER TABLE \"InnovationIdeas\" ADD COLUMN IF NOT EXISTS \"AuditAssignedAt\" timestamp with time zone NULL;");

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
}
