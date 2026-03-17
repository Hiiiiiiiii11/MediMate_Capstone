using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMembershipAndSubscriptionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsultantLimit",
                table: "MembershipPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OcrLimit",
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

            migrationBuilder.AddColumn<int>(
                name: "RemainingOcrCount",
                table: "FamilySubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsultantLimit",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "OcrLimit",
                table: "MembershipPackages");

            migrationBuilder.DropColumn(
                name: "RemainingConsultantCount",
                table: "FamilySubscriptions");

            migrationBuilder.DropColumn(
                name: "RemainingOcrCount",
                table: "FamilySubscriptions");
        }
    }
}
