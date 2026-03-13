using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addnewTable1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorBankAccounts",
                columns: table => new
                {
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: false),
                    AccountNumber = table.Column<string>(type: "text", nullable: false),
                    AccountHolder = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorBankAccounts", x => x.BankAccountId);
                    table.ForeignKey(
                        name: "FK_DoctorBankAccounts_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorDocuments",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewBy = table.Column<string>(type: "text", nullable: false),
                    ReviewAt = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorDocuments", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_DoctorDocuments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AvailabilityId",
                table: "Appointments",
                column: "AvailabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorBankAccounts_DoctorId",
                table: "DoctorBankAccounts",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorDocuments_DoctorId",
                table: "DoctorDocuments",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_DoctorAvailabilities_AvailabilityId",
                table: "Appointments",
                column: "AvailabilityId",
                principalTable: "DoctorAvailabilities",
                principalColumn: "DoctorAvailabilityId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_DoctorAvailabilities_AvailabilityId",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "DoctorBankAccounts");

            migrationBuilder.DropTable(
                name: "DoctorDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_AvailabilityId",
                table: "Appointments");
        }
    }
}
