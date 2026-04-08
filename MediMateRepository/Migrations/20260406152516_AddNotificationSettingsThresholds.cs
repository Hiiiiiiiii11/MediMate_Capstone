using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSettingsThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxDosesPerDay",
                table: "NotificationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinimumHoursGap",
                table: "NotificationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MissedDosesThreshold",
                table: "NotificationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxDosesPerDay",
                table: "NotificationSettings");

            migrationBuilder.DropColumn(
                name: "MinimumHoursGap",
                table: "NotificationSettings");

            migrationBuilder.DropColumn(
                name: "MissedDosesThreshold",
                table: "NotificationSettings");
        }
    }
}
