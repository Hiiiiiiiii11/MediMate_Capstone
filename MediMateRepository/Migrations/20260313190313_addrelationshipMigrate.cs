using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addrelationshipMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_DoctorAvailabilities_AvailabilityId",
                table: "Appointments");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_DoctorAvailabilities_AvailabilityId",
                table: "Appointments",
                column: "AvailabilityId",
                principalTable: "DoctorAvailabilities",
                principalColumn: "DoctorAvailabilityId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_DoctorAvailabilities_AvailabilityId",
                table: "Appointments");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_DoctorAvailabilities_AvailabilityId",
                table: "Appointments",
                column: "AvailabilityId",
                principalTable: "DoctorAvailabilities",
                principalColumn: "DoctorAvailabilityId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
