using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaOtherFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpectedImpactOther",
                table: "InnovationIdeas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetAudienceOther",
                table: "InnovationIdeas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnologyOther",
                table: "InnovationIdeas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsesEmergingTech",
                table: "InnovationIdeas",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedImpactOther",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "TargetAudienceOther",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "TechnologyOther",
                table: "InnovationIdeas");

            migrationBuilder.DropColumn(
                name: "UsesEmergingTech",
                table: "InnovationIdeas");
        }
    }
}
