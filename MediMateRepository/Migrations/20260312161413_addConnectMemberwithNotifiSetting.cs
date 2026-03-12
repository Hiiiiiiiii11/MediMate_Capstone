using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addConnectMemberwithNotifiSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NotificationSettingSettingId",
                table: "Members",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_NotificationSettingSettingId",
                table: "Members",
                column: "NotificationSettingSettingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_NotificationSettings_NotificationSettingSettingId",
                table: "Members",
                column: "NotificationSettingSettingId",
                principalTable: "NotificationSettings",
                principalColumn: "SettingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_NotificationSettings_NotificationSettingSettingId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_NotificationSettingSettingId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "NotificationSettingSettingId",
                table: "Members");
        }
    }
}
