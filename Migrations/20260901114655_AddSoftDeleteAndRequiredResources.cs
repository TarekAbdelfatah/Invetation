using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndRequiredResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InnovationIdeas_ApplicantUserId",
                table: "InnovationIdeas");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InnovationIdeas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InnovationIdeas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiredResources",
                table: "InnovationIdeas",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ApplicantUserId",
                table: "InnovationIdeas",
                column: "ApplicantUserId",
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InnovationIdeas_ApplicantUserId",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "RequiredResources",
                table: "InnovationIdeas");

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIdeas_ApplicantUserId",
                table: "InnovationIdeas",
                column: "ApplicantUserId");
        }
    }
}
