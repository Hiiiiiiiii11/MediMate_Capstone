using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class FixAppointmentConflict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationSessions_Appointments_AppointmentId1",
                table: "ConsultationSessions");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationSessions_AppointmentId",
                table: "ConsultationSessions");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationSessions_AppointmentId1",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "AppointmentId1",
                table: "ConsultationSessions");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_AppointmentId",
                table: "ConsultationSessions",
                column: "AppointmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConsultationSessions_AppointmentId",
                table: "ConsultationSessions");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId1",
                table: "ConsultationSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_AppointmentId",
                table: "ConsultationSessions",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_AppointmentId1",
                table: "ConsultationSessions",
                column: "AppointmentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationSessions_Appointments_AppointmentId1",
                table: "ConsultationSessions",
                column: "AppointmentId1",
                principalTable: "Appointments",
                principalColumn: "AppointmentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
