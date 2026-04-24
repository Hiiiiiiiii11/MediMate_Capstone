using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addbankinfoforuserandclinic3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropColumn(
            //     name: "CreatedAt",
            //     table: "DoctorPayouts");

            // Manually add missing bank columns
            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Clinics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Clinics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountHolder",
                table: "Clinics",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.AddColumn<DateTime>(
            //     name: "CreatedAt",
            //     table: "DoctorPayouts",
            //     type: "timestamp without time zone",
            //     nullable: false,
            //     defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.DropColumn(name: "BankName", table: "Clinics");
            migrationBuilder.DropColumn(name: "BankAccountNumber", table: "Clinics");
            migrationBuilder.DropColumn(name: "BankAccountHolder", table: "Clinics");
        }
    }
}
