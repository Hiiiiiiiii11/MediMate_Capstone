using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addAtributeFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NotificationSettingSettingId",
                table: "Families",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Families_NotificationSettingSettingId",
                table: "Families",
                column: "NotificationSettingSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Families_NotificationSettings_NotificationSettingSettingId",
                table: "Families",
                column: "NotificationSettingSettingId",
                principalTable: "NotificationSettings",
                principalColumn: "SettingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Families_NotificationSettings_NotificationSettingSettingId",
                table: "Families");

            migrationBuilder.DropIndex(
                name: "IX_Families_NotificationSettingSettingId",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "NotificationSettingSettingId",
                table: "Families");
        }
    }
}
