using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class AddInnovationIdea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InnovationIdeas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ProblemStatement = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ProposedSolution = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ExpectedBenefits = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    InnovationDomainId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedImpactId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetAudienceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDraft = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ApplicantDepartmentId",
                table: "InnovationIdeas",
                column: "ApplicantDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ApplicantUserId",
                table: "InnovationIdeas",
                column: "ApplicantUserId");

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
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_TargetAudienceId",
                table: "InnovationIdeas",
                column: "TargetAudienceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InnovationIdeas");
        }
    }
}
