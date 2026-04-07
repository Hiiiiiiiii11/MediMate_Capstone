using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addAtributeConsultSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GuardianJoined",
                table: "ConsultationSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "GuardianUserId",
                table: "ConsultationSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_GuardianUserId",
                table: "ConsultationSessions",
                column: "GuardianUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationSessions_Users_GuardianUserId",
                table: "ConsultationSessions",
                column: "GuardianUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationSessions_Users_GuardianUserId",
                table: "ConsultationSessions");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationSessions_GuardianUserId",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "GuardianJoined",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "GuardianUserId",
                table: "ConsultationSessions");
        }
    }
}
