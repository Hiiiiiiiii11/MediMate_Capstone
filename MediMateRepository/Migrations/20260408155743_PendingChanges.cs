using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DoctorBankAccountBankAccountId",
                table: "Doctors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_DoctorBankAccountBankAccountId",
                table: "Doctors",
                column: "DoctorBankAccountBankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_DoctorBankAccounts_DoctorBankAccountBankAccountId",
                table: "Doctors",
                column: "DoctorBankAccountBankAccountId",
                principalTable: "DoctorBankAccounts",
                principalColumn: "BankAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_DoctorBankAccounts_DoctorBankAccountBankAccountId",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_DoctorBankAccountBankAccountId",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "DoctorBankAccountBankAccountId",
                table: "Doctors");
        }
    }
}
