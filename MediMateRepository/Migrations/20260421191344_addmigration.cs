using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_DoctorBankAccounts_DoctorBankAccountBankAccountId",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_DoctorBankAccountBankAccountId",
                table: "Doctors");

            migrationBuilder.DropIndex(
                name: "IX_DoctorBankAccounts_DoctorId",
                table: "DoctorBankAccounts");

            migrationBuilder.DropColumn(
                name: "DoctorBankAccountBankAccountId",
                table: "Doctors");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorBankAccounts_DoctorId",
                table: "DoctorBankAccounts",
                column: "DoctorId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DoctorBankAccounts_DoctorId",
                table: "DoctorBankAccounts");

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorBankAccountBankAccountId",
                table: "Doctors",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_DoctorBankAccountBankAccountId",
                table: "Doctors",
                column: "DoctorBankAccountBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorBankAccounts_DoctorId",
                table: "DoctorBankAccounts",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_DoctorBankAccounts_DoctorBankAccountBankAccountId",
                table: "Doctors",
                column: "DoctorBankAccountBankAccountId",
                principalTable: "DoctorBankAccounts",
                principalColumn: "BankAccountId");
        }
    }
}
