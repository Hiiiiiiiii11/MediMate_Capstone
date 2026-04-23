using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class ApplyClinicDoctorsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorPayouts_DoctorPayoutRates_RateId",
                table: "DoctorPayouts");

            migrationBuilder.DropTable(
                name: "DoctorContracts");

            migrationBuilder.DropIndex(
                name: "IX_DoctorPayouts_RateId",
                table: "DoctorPayouts");

            migrationBuilder.DropColumn(
                name: "ConsultantLimit",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "RemainingConsultantCount",
                table: "FamilySubscriptions");

            migrationBuilder.DropColumn(
                name: "RateId",
                table: "DoctorPayouts");

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "DoctorPayouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInTime",
                table: "Appointments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "Appointments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Appointments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Clinics",
                columns: table => new
                {
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    License = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.ClinicId);
                    table.ForeignKey(
                        name: "FK_Clinics_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageType = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionUsageLogs_FamilySubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "FamilySubscriptions",
                        principalColumn: "SubscriptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicContracts",
                columns: table => new
                {
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicContracts", x => x.ContractId);
                    table.ForeignKey(
                        name: "FK_ClinicContracts_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "ClinicId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicDoctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Specialty = table.Column<string>(type: "text", nullable: true),
                    ConsultationFee = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicDoctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicDoctors_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "ClinicId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClinicDoctors_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPayouts_ClinicId",
                table: "DoctorPayouts",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId",
                table: "Appointments",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicContracts_ClinicId",
                table: "ClinicContracts",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDoctors_ClinicId",
                table: "ClinicDoctors",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDoctors_DoctorId",
                table: "ClinicDoctors",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_AdminId",
                table: "Clinics",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionUsageLogs_SubscriptionId",
                table: "SubscriptionUsageLogs",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Clinics_ClinicId",
                table: "Appointments",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "ClinicId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorPayouts_Clinics_ClinicId",
                table: "DoctorPayouts",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "ClinicId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Clinics_ClinicId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorPayouts_Clinics_ClinicId",
                table: "DoctorPayouts");

            migrationBuilder.DropTable(
                name: "ClinicContracts");

            migrationBuilder.DropTable(
                name: "ClinicDoctors");

            migrationBuilder.DropTable(
                name: "SubscriptionUsageLogs");

            migrationBuilder.DropTable(
                name: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_DoctorPayouts_ClinicId",
                table: "DoctorPayouts");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClinicId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DoctorPayouts");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Appointments");

            migrationBuilder.AddColumn<int>(
                name: "ConsultantLimit",
                table: "MembershipPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RemainingConsultantCount",
                table: "FamilySubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RateId",
                table: "DoctorPayouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DoctorContracts",
                columns: table => new
                {
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorContracts", x => x.ContractId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPayouts_RateId",
                table: "DoctorPayouts",
                column: "RateId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorPayouts_DoctorPayoutRates_RateId",
                table: "DoctorPayouts",
                column: "RateId",
                principalTable: "DoctorPayoutRates",
                principalColumn: "RateId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
