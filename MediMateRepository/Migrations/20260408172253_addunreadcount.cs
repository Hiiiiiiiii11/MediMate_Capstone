using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addunreadcount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnreadCountDoctor",
                table: "ConsultationSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnreadCountMember",
                table: "ConsultationSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnreadCountDoctor",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "UnreadCountMember",
                table: "ConsultationSessions");
        }
    }
}
