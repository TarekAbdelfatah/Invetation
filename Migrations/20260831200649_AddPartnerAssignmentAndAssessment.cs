using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerAssignmentAndAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDepartmentId",
                table: "InnovationIdeas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuditAssignedAt",
                table: "InnovationIdeas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuditEmployeeId",
                table: "InnovationIdeas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentHeaders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessorDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsDraft = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalScore = table.Column<decimal>(type: "numeric", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DecisionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TargetDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuditorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "PartnerAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentHeaderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                name: "IX_InnovationIdeas_AssignedDepartmentId",
                table: "InnovationIdeas",
                column: "AssignedDepartmentId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_InnovationIdeas_Departments_AssignedDepartmentId",
                table: "InnovationIdeas",
                column: "AssignedDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InnovationIdeas_Departments_AssignedDepartmentId",
                table: "InnovationIdeas");

            migrationBuilder.DropTable(
                name: "AssessmentDetails");

            migrationBuilder.DropTable(
                name: "AuditActionItems");

            migrationBuilder.DropTable(
                name: "PartnerAssignments");

            migrationBuilder.DropTable(
                name: "AssessmentHeaders");

            migrationBuilder.DropIndex(
                name: "IX_InnovationIdeas_AssignedDepartmentId",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "AssignedDepartmentId",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "AuditAssignedAt",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "AuditEmployeeId",
                table: "InnovationIdeas");
        }
    }
}
