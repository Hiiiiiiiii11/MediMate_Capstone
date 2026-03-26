using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addFamilyAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationSettings_Members_MemberId",
                table: "NotificationSettings");

            migrationBuilder.RenameColumn(
                name: "MemberId",
                table: "NotificationSettings",
                newName: "FamilyId");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationSettings_MemberId",
                table: "NotificationSettings",
                newName: "IX_NotificationSettings_FamilyId");

            migrationBuilder.RenameColumn(
                name: "SentdAt",
                table: "MedicationReminders",
                newName: "SentAt");

            migrationBuilder.AddColumn<string>(
                name: "FamilyAvatarUrl",
                table: "Families",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationSettings_Families_FamilyId",
                table: "NotificationSettings",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "FamilyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationSettings_Families_FamilyId",
                table: "NotificationSettings");

            migrationBuilder.DropColumn(
                name: "FamilyAvatarUrl",
                table: "Families");

            migrationBuilder.RenameColumn(
                name: "FamilyId",
                table: "NotificationSettings",
                newName: "MemberId");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationSettings_FamilyId",
                table: "NotificationSettings",
                newName: "IX_NotificationSettings_MemberId");

            migrationBuilder.RenameColumn(
                name: "SentAt",
                table: "MedicationReminders",
                newName: "SentdAt");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationSettings_Members_MemberId",
                table: "NotificationSettings",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
