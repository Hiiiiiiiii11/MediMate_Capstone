using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgoraRecordingResourceId",
                table: "ConsultationSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgoraSid",
                table: "ConsultationSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecordingDuration",
                table: "ConsultationSessions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgoraRecordingResourceId",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "AgoraSid",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "RecordingDuration",
                table: "ConsultationSessions");
        }
    }
}
