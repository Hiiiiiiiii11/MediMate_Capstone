using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChatSenderFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatDoctorMessages_Doctors_SenderId",
                table: "ChatDoctorMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatDoctorMessages_Members_SenderId",
                table: "ChatDoctorMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatDoctorMessages_SenderId",
                table: "ChatDoctorMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ChatDoctorMessages_SenderId",
                table: "ChatDoctorMessages",
                column: "SenderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatDoctorMessages_Doctors_SenderId",
                table: "ChatDoctorMessages",
                column: "SenderId",
                principalTable: "Doctors",
                principalColumn: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatDoctorMessages_Members_SenderId",
                table: "ChatDoctorMessages",
                column: "SenderId",
                principalTable: "Members",
                principalColumn: "MemberId");
        }
    }
}
