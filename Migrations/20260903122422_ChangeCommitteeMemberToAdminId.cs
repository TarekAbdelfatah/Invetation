using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ibtikar.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCommitteeMemberToAdminId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommitteeMembers_InnovationCommittees_InnovationCommitteeId",
                table: "CommitteeMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_CommitteeMembers_Users_UserId",
                table: "CommitteeMembers");

            migrationBuilder.DropIndex(
                name: "IX_CommitteeMembers_UserId",
                table: "CommitteeMembers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CommitteeMembers");

            migrationBuilder.AlterColumn<Guid>(
                name: "InnovationCommitteeId",
                table: "CommitteeMembers",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "AdminId",
                table: "CommitteeMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_AdminId",
                table: "CommitteeMembers",
                column: "AdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommitteeMembers_Admins_AdminId",
                table: "CommitteeMembers",
                column: "AdminId",
                principalTable: "Admins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CommitteeMembers_InnovationCommittees_InnovationCommitteeId",
                table: "CommitteeMembers",
                column: "InnovationCommitteeId",
                principalTable: "InnovationCommittees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommitteeMembers_Admins_AdminId",
                table: "CommitteeMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_CommitteeMembers_InnovationCommittees_InnovationCommitteeId",
                table: "CommitteeMembers");

            migrationBuilder.DropIndex(
                name: "IX_CommitteeMembers_AdminId",
                table: "CommitteeMembers");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "CommitteeMembers");

            migrationBuilder.AlterColumn<Guid>(
                name: "InnovationCommitteeId",
                table: "CommitteeMembers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "CommitteeMembers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_UserId",
                table: "CommitteeMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommitteeMembers_InnovationCommittees_InnovationCommitteeId",
                table: "CommitteeMembers",
                column: "InnovationCommitteeId",
                principalTable: "InnovationCommittees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommitteeMembers_Users_UserId",
                table: "CommitteeMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
