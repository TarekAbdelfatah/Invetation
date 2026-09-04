using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class MakeAssessorDepartmentIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [AssessmentHeaders] ALTER COLUMN [AssessorDepartmentId] uniqueidentifier NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [AssessmentHeaders] ALTER COLUMN [AssessorDepartmentId] uniqueidentifier NOT NULL");
        }
    }
}
