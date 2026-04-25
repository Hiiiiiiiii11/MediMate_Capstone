using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class Addrelationappointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentsAppointmentId",
                table: "ConsultationSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_AppointmentsAppointmentId",
                table: "ConsultationSessions",
                column: "AppointmentsAppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationSessions_Appointments_AppointmentsAppointmentId",
                table: "ConsultationSessions",
                column: "AppointmentsAppointmentId",
                principalTable: "Appointments",
                principalColumn: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationSessions_Appointments_AppointmentsAppointmentId",
                table: "ConsultationSessions");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationSessions_AppointmentsAppointmentId",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "AppointmentsAppointmentId",
                table: "ConsultationSessions");
        }
    }
}
