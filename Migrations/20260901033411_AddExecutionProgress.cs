using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommitteeVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "InnovationCommittees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "CommitteeDelegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InnovationCommitteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    HeadUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateMemberUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeDelegations_InnovationCommittees_InnovationCommitt~",
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InnovationCommitteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsHead = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_InnovationCommittees_CreatedByUserId",
                table: "InnovationCommittees",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationCommittees_IsActive",
                table: "InnovationCommittees",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitteeDelegations");

            migrationBuilder.DropTable(
                name: "CommitteeMembers");

            migrationBuilder.DropTable(
                name: "CommitteeVotes");

            migrationBuilder.DropTable(
                name: "InnovationCommittees");
        }
    }
}
