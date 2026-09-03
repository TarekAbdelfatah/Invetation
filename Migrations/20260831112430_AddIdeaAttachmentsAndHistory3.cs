using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaAttachmentsAndHistory3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdeaAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InnovationIdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatusId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAttachments_InnovationIdeaId",
                table: "IdeaAttachments",
                column: "InnovationIdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaAttachments_UploadedByUserId",
                table: "IdeaAttachments",
                column: "UploadedByUserId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdeaAttachments");

            migrationBuilder.DropTable(
                name: "IdeaStatusHistories");
        }
    }
}
