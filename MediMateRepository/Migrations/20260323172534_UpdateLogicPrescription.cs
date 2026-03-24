using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLogicPrescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationSchedules_PrescriptionMedicines_PrescriptionMedic~",
                table: "MedicationSchedules");

            migrationBuilder.DropIndex(
                name: "IX_MedicationSchedules_PrescriptionMedicineId",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "PrescriptionMedicineId",
                table: "MedicationSchedules");

            migrationBuilder.AddColumn<Guid>(
                name: "PrescriptionId",
                table: "MedicationSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_PrescriptionId",
                table: "MedicationSchedules",
                column: "PrescriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationSchedules_Prescriptions_PrescriptionId",
                table: "MedicationSchedules",
                column: "PrescriptionId",
                principalTable: "Prescriptions",
                principalColumn: "PrescriptionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationSchedules_Prescriptions_PrescriptionId",
                table: "MedicationSchedules");

            migrationBuilder.DropIndex(
                name: "IX_MedicationSchedules_PrescriptionId",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "PrescriptionId",
                table: "MedicationSchedules");

            migrationBuilder.AddColumn<Guid>(
                name: "PrescriptionMedicineId",
                table: "MedicationSchedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_PrescriptionMedicineId",
                table: "MedicationSchedules",
                column: "PrescriptionMedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationSchedules_PrescriptionMedicines_PrescriptionMedic~",
                table: "MedicationSchedules",
                column: "PrescriptionMedicineId",
                principalTable: "PrescriptionMedicines",
                principalColumn: "PrescriptionMedicineId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
