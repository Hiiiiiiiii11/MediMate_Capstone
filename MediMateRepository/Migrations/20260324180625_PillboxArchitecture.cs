using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class PillboxArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationSchedules_Prescriptions_PrescriptionId",
                table: "MedicationSchedules");

            migrationBuilder.DropIndex(
                name: "IX_MedicationSchedules_PrescriptionId",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "CreateAt",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "Dosage",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "IsAiGenerated",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "MedicineName",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "PrescriptionId",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MedicationSchedules");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "MedicationSchedules",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "SpecificTimes",
                table: "MedicationSchedules",
                newName: "ScheduleName");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeOfDay",
                table: "MedicationSchedules",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "MedicationScheduleDetails",
                columns: table => new
                {
                    ScheduleDetailId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionMedicineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dosage = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationScheduleDetails", x => x.ScheduleDetailId);
                    table.ForeignKey(
                        name: "FK_MedicationScheduleDetails_MedicationSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "MedicationSchedules",
                        principalColumn: "ScheduleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicationScheduleDetails_PrescriptionMedicines_Prescriptio~",
                        column: x => x.PrescriptionMedicineId,
                        principalTable: "PrescriptionMedicines",
                        principalColumn: "PrescriptionMedicineId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationScheduleDetails_PrescriptionMedicineId",
                table: "MedicationScheduleDetails",
                column: "PrescriptionMedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationScheduleDetails_ScheduleId",
                table: "MedicationScheduleDetails",
                column: "ScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicationScheduleDetails");

            migrationBuilder.DropColumn(
                name: "TimeOfDay",
                table: "MedicationSchedules");

            migrationBuilder.RenameColumn(
                name: "ScheduleName",
                table: "MedicationSchedules",
                newName: "SpecificTimes");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "MedicationSchedules",
                newName: "StartDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateAt",
                table: "MedicationSchedules",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Dosage",
                table: "MedicationSchedules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "MedicationSchedules",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                table: "MedicationSchedules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "MedicationSchedules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAiGenerated",
                table: "MedicationSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MedicineName",
                table: "MedicationSchedules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PrescriptionId",
                table: "MedicationSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MedicationSchedules",
                type: "timestamp without time zone",
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
    }
}
