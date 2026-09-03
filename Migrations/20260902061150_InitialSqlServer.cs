using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CriterionScorings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Percent = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterionScorings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpectedImpacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsOther = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpectedImpacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdeaStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InnovationDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnovationDomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TargetAudiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsOther = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetAudiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsOther = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technologies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NetworkUser = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DeptId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admins_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InnovationCommittees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnovationCommittees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InnovationCommittees_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InnovationIdeas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ProblemStatement = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProposedSolution = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExpectedBenefits = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExpectedImpactOther = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetAudienceOther = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsesEmergingTech = table.Column<bool>(type: "bit", nullable: false),
                    TechnologyOther = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InnovationDomainId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedImpactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetAudienceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicantDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuditEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuditAssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnovationIdeas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InnovationIdeas_Departments_ApplicantDepartmentId",
                        column: x => x.ApplicantDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InnovationIdeas_Departments_AssignedDepartmentId",
                        column: x => x.AssignedDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InnovationIdeas_ExpectedImpacts_ExpectedImpactId",
                        column: x => x.ExpectedImpactId,
                        principalTable: "ExpectedImpacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InnovationIdeas_IdeaStatuses_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "IdeaStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InnovationIdeas_InnovationDomains_InnovationDomainId",
                        column: x => x.InnovationDomainId,
                        principalTable: "InnovationDomains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InnovationIdeas_TargetAudiences_TargetAudienceId",
                        column: x => x.TargetAudienceId,
                        principalTable: "TargetAudiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InnovationIdeas_Users_ApplicantUserId",
                        column: x => x.ApplicantUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeDelegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationCommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HeadUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DelegateMemberUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeDelegations_InnovationCommittees_InnovationCommitteeId",
                        column: x => x.InnovationCommitteeId,
                        principalTable: "InnovationCommittees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitteeDelegations_Users_DelegateMemberUserId",
                        column: x => x.DelegateMemberUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommitteeDelegations_Users_HeadUserId",
                        column: x => x.HeadUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationCommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsHead = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeMembers_InnovationCommittees_InnovationCommitteeId",
                        column: x => x.InnovationCommitteeId,
                        principalTable: "InnovationCommittees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitteeMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentHeaders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessorDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentHeaders_Departments_AssessorDepartmentId",
                        column: x => x.AssessorDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentHeaders_InnovationIdeas_InnovationIdeaId",
                        column: x => x.InnovationIdeaId,
                        principalTable: "InnovationIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentHeaders_Users_AssessorUserId",
                        column: x => x.AssessorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditActionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DecisionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuditorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditActionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditActionItems_Departments_TargetDepartmentId",
                        column: x => x.TargetDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditActionItems_InnovationIdeas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "InnovationIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditActionItems_Users_AuditorId",
                        column: x => x.AuditorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeVotes_InnovationIdeas_InnovationIdeaId",
                        column: x => x.InnovationIdeaId,
                        principalTable: "InnovationIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitteeVotes_Users_MemberUserId",
                        column: x => x.MemberUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionProgresses_ExecutionStages_ExecutionStageId",
                        column: x => x.ExecutionStageId,
                        principalTable: "ExecutionStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionProgresses_InnovationIdeas_InnovationIdeaId",
                        column: x => x.InnovationIdeaId,
                        principalTable: "InnovationIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionProgresses_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IdeaAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdeaAttachments_InnovationIdeas_InnovationIdeaId",
                        column: x => x.InnovationIdeaId,
                        principalTable: "InnovationIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdeaAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdeaStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdeaStatusHistories_IdeaStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "IdeaStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdeaStatusHistories_IdeaStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "IdeaStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdeaStatusHistories_InnovationIdeas_InnovationIdeaId",
                        column: x => x.InnovationIdeaId,
                        principalTable: "InnovationIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdeaStatusHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PartnerAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartnerDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerAssignments_Departments_PartnerDepartmentId",
                        column: x => x.PartnerDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerAssignments_InnovationIdeas_InnovationIdeaId",
                        column: x => x.InnovationIdeaId,
                        principalTable: "InnovationIdeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartnerAssignments_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentDetails_AssessmentCriteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "AssessmentCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentDetails_AssessmentHeaders_AssessmentHeaderId",
                        column: x => x.AssessmentHeaderId,
                        principalTable: "AssessmentHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_NetworkUser",
                table: "Admins",
                column: "NetworkUser",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_RoleId",
                table: "Admins",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriteria_Code",
                table: "AssessmentCriteria",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentDetails_AssessmentHeaderId",
                table: "AssessmentDetails",
                column: "AssessmentHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentDetails_CriterionId",
                table: "AssessmentDetails",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentHeaders_AssessorDepartmentId",
                table: "AssessmentHeaders",
                column: "AssessorDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentHeaders_AssessorUserId",
                table: "AssessmentHeaders",
                column: "AssessorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentHeaders_InnovationIdeaId",
                table: "AssessmentHeaders",
                column: "InnovationIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentHeaders_InnovationIdeaId_Source",
                table: "AssessmentHeaders",
                columns: new[] { "InnovationIdeaId", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditActionItems_AuditorId",
                table: "AuditActionItems",
                column: "AuditorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditActionItems_IdeaId",
                table: "AuditActionItems",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditActionItems_TargetDepartmentId",
                table: "AuditActionItems",
                column: "TargetDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDelegations_DelegateMemberUserId",
                table: "CommitteeDelegations",
                column: "DelegateMemberUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDelegations_HeadUserId",
                table: "CommitteeDelegations",
                column: "HeadUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDelegations_InnovationCommitteeId",
                table: "CommitteeDelegations",
                column: "InnovationCommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDelegations_InnovationCommitteeId_StartAt_EndAt",
                table: "CommitteeDelegations",
                columns: new[] { "InnovationCommitteeId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_InnovationCommitteeId",
                table: "CommitteeMembers",
                column: "InnovationCommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_UserId",
                table: "CommitteeMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeVotes_InnovationIdeaId",
                table: "CommitteeVotes",
                column: "InnovationIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeVotes_InnovationIdeaId_MemberUserId",
                table: "CommitteeVotes",
                columns: new[] { "InnovationIdeaId", "MemberUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeVotes_MemberUserId",
                table: "CommitteeVotes",
                column: "MemberUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CriterionScorings_Score",
                table: "CriterionScorings",
                column: "Score",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionProgresses_ChangedByUserId",
                table: "ExecutionProgresses",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionProgresses_ExecutionStageId",
                table: "ExecutionProgresses",
                column: "ExecutionStageId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionProgresses_InnovationIdeaId",
                table: "ExecutionProgresses",
                column: "InnovationIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionProgresses_InnovationIdeaId_ChangedAt",
                table: "ExecutionProgresses",
                columns: new[] { "InnovationIdeaId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionStages_Code",
                table: "ExecutionStages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionStages_Order",
                table: "ExecutionStages",
                column: "Order",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpectedImpacts_Code",
                table: "ExpectedImpacts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAttachments_InnovationIdeaId",
                table: "IdeaAttachments",
                column: "InnovationIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAttachments_UploadedByUserId",
                table: "IdeaAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaStatuses_Code",
                table: "IdeaStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdeaStatusHistories_ChangedByUserId",
                table: "IdeaStatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaStatusHistories_FromStatusId",
                table: "IdeaStatusHistories",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaStatusHistories_InnovationIdeaId",
                table: "IdeaStatusHistories",
                column: "InnovationIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaStatusHistories_ToStatusId",
                table: "IdeaStatusHistories",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationCommittees_CreatedByUserId",
                table: "InnovationCommittees",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationCommittees_IsActive",
                table: "InnovationCommittees",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationDomains_Code",
                table: "InnovationDomains",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ApplicantDepartmentId",
                table: "InnovationIdeas",
                column: "ApplicantDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ApplicantUserId",
                table: "InnovationIdeas",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_AssignedDepartmentId",
                table: "InnovationIdeas",
                column: "AssignedDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_CurrentStatusId",
                table: "InnovationIdeas",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ExpectedImpactId",
                table: "InnovationIdeas",
                column: "ExpectedImpactId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_InnovationDomainId",
                table: "InnovationIdeas",
                column: "InnovationDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ReferenceNumber",
                table: "InnovationIdeas",
                column: "ReferenceNumber",
                unique: true,
                filter: "\"IsDraft\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_TargetAudienceId",
                table: "InnovationIdeas",
                column: "TargetAudienceId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAssignments_InnovationIdeaId",
                table: "PartnerAssignments",
                column: "InnovationIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAssignments_InnovationIdeaId_PartnerDepartmentId",
                table: "PartnerAssignments",
                columns: new[] { "InnovationIdeaId", "PartnerDepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAssignments_PartnerDepartmentId",
                table: "PartnerAssignments",
                column: "PartnerDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerAssignments_RequestedByUserId",
                table: "PartnerAssignments",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TargetAudiences_Code",
                table: "TargetAudiences",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technologies_Code",
                table: "Technologies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "AssessmentDetails");

            migrationBuilder.DropTable(
                name: "AuditActionItems");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CommitteeDelegations");

            migrationBuilder.DropTable(
                name: "CommitteeMembers");

            migrationBuilder.DropTable(
                name: "CommitteeVotes");

            migrationBuilder.DropTable(
                name: "CriterionScorings");

            migrationBuilder.DropTable(
                name: "ExecutionProgresses");

            migrationBuilder.DropTable(
                name: "IdeaAttachments");

            migrationBuilder.DropTable(
                name: "IdeaStatusHistories");

            migrationBuilder.DropTable(
                name: "PartnerAssignments");

            migrationBuilder.DropTable(
                name: "Technologies");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "AssessmentCriteria");

            migrationBuilder.DropTable(
                name: "AssessmentHeaders");

            migrationBuilder.DropTable(
                name: "InnovationCommittees");

            migrationBuilder.DropTable(
                name: "ExecutionStages");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "InnovationIdeas");

            migrationBuilder.DropTable(
                name: "ExpectedImpacts");

            migrationBuilder.DropTable(
                name: "IdeaStatuses");

            migrationBuilder.DropTable(
                name: "InnovationDomains");

            migrationBuilder.DropTable(
                name: "TargetAudiences");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
