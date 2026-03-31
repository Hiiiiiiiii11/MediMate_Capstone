using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class ConsultantSessionV2_AddJoinTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConsultationSessionsConsultanSessionId",
                table: "Ratings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DoctorJoined",
                table: "ConsultationSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ConsultationSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UserJoined",
                table: "ConsultationSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_ConsultationSessionsConsultanSessionId",
                table: "Ratings",
                column: "ConsultationSessionsConsultanSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_ConsultationSessions_ConsultationSessionsConsultanS~",
                table: "Ratings",
                column: "ConsultationSessionsConsultanSessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "ConsultanSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_ConsultationSessions_ConsultationSessionsConsultanS~",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_ConsultationSessionsConsultanSessionId",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "ConsultationSessionsConsultanSessionId",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "DoctorJoined",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "UserJoined",
                table: "ConsultationSessions");
        }
    }
}
