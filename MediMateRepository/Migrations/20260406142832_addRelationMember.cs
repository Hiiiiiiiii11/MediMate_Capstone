using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addRelationMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId1",
                table: "Members",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_FamilyId1",
                table: "Members",
                column: "FamilyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Families_FamilyId1",
                table: "Members",
                column: "FamilyId1",
                principalTable: "Families",
                principalColumn: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Families_FamilyId1",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_FamilyId1",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "FamilyId1",
                table: "Members");
        }
    }
}
