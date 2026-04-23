using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class ApplyClinicDoctorsModel1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorPayoutRates");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "Payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConsultationId",
                table: "DoctorPayouts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "DoctorPayouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AppointmentId",
                table: "Payments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPayouts_AppointmentId",
                table: "DoctorPayouts",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorPayouts_Appointments_AppointmentId",
                table: "DoctorPayouts",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "AppointmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Appointments_AppointmentId",
                table: "Payments",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "AppointmentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorPayouts_Appointments_AppointmentId",
                table: "DoctorPayouts");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Appointments_AppointmentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_AppointmentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_DoctorPayouts_AppointmentId",
                table: "DoctorPayouts");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "DoctorPayouts");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConsultationId",
                table: "DoctorPayouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DoctorPayoutRates",
                columns: table => new
                {
                    RateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountPerSession = table.Column<decimal>(type: "numeric", nullable: false),
                    ConsultationType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPayoutRates", x => x.RateId);
                });
        }
    }
}
