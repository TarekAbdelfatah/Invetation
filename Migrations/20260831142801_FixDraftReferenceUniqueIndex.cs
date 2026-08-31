using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class FixDraftReferenceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InnovationIdeas_ReferenceNumber",
                table: "InnovationIdeas");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ReferenceNumber",
                table: "InnovationIdeas",
                column: "ReferenceNumber",
                unique: true,
                filter: "\"IsDraft\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InnovationIdeas_ReferenceNumber",
                table: "InnovationIdeas");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ReferenceNumber",
                table: "InnovationIdeas",
                column: "ReferenceNumber",
                unique: true);
        }
    }
}
