using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class fixtableConsultantionSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionsByDoctor_ConsultationSessions_SessionId",
                table: "PrescriptionsByDoctor");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_ConsultationSessions_SessionId",
                table: "Ratings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConsultationSessions",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "DoctorNotes",
                table: "ConsultationSessions");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "Ratings",
                newName: "ConsultationSessionConsultanSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Ratings_SessionId",
                table: "Ratings",
                newName: "IX_Ratings_ConsultationSessionConsultanSessionId");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "ConsultationSessions",
                newName: "AppointmentId1");

            migrationBuilder.AddColumn<Guid>(
                name: "ConsultanSessionId",
                table: "Ratings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConsultanSessionId",
                table: "ConsultationSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DoctorNote",
                table: "ConsultationSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecordUrl",
                table: "ConsultationSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConsultationSessions",
                table: "ConsultationSessions",
                column: "ConsultanSessionId");

            migrationBuilder.CreateTable(
                name: "ChatDoctorMessages",
                columns: table => new
                {
                    ChatDoctorMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultanSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    SendAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConsultantSessionConsultanSessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatDoctorMessages", x => x.ChatDoctorMessageId);
                    table.ForeignKey(
                        name: "FK_ChatDoctorMessages_ConsultationSessions_ConsultantSessionCo~",
                        column: x => x.ConsultantSessionConsultanSessionId,
                        principalTable: "ConsultationSessions",
                        principalColumn: "ConsultanSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatDoctorMessages_Doctors_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId");
                    table.ForeignKey(
                        name: "FK_ChatDoctorMessages_Members_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Members",
                        principalColumn: "MemberId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_ConsultanSessionId",
                table: "Ratings",
                column: "ConsultanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_DoctorId",
                table: "Ratings",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_MemberId",
                table: "Ratings",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_AppointmentId1",
                table: "ConsultationSessions",
                column: "AppointmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_DoctorId",
                table: "ConsultationSessions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSessions_MemberId",
                table: "ConsultationSessions",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatDoctorMessages_ConsultantSessionConsultanSessionId",
                table: "ChatDoctorMessages",
                column: "ConsultantSessionConsultanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatDoctorMessages_SenderId",
                table: "ChatDoctorMessages",
                column: "SenderId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationSessions_Appointments_AppointmentId1",
                table: "ConsultationSessions",
                column: "AppointmentId1",
                principalTable: "Appointments",
                principalColumn: "AppointmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationSessions_Doctors_DoctorId",
                table: "ConsultationSessions",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "DoctorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationSessions_Members_MemberId",
                table: "ConsultationSessions",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionsByDoctor_ConsultationSessions_SessionId",
                table: "PrescriptionsByDoctor",
                column: "SessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "ConsultanSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_ConsultationSessions_ConsultanSessionId",
                table: "Ratings",
                column: "ConsultanSessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "ConsultanSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_ConsultationSessions_ConsultationSessionConsultanSe~",
                table: "Ratings",
                column: "ConsultationSessionConsultanSessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "ConsultanSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Doctors_DoctorId",
                table: "Ratings",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "DoctorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Members_MemberId",
                table: "Ratings",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationSessions_Appointments_AppointmentId1",
                table: "ConsultationSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationSessions_Doctors_DoctorId",
                table: "ConsultationSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationSessions_Members_MemberId",
                table: "ConsultationSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionsByDoctor_ConsultationSessions_SessionId",
                table: "PrescriptionsByDoctor");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_ConsultationSessions_ConsultanSessionId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_ConsultationSessions_ConsultationSessionConsultanSe~",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Doctors_DoctorId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Members_MemberId",
                table: "Ratings");

            migrationBuilder.DropTable(
                name: "ChatDoctorMessages");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_ConsultanSessionId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_DoctorId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_MemberId",
                table: "Ratings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConsultationSessions",
                table: "ConsultationSessions");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationSessions_AppointmentId1",
                table: "ConsultationSessions");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationSessions_DoctorId",
                table: "ConsultationSessions");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationSessions_MemberId",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "ConsultanSessionId",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "ConsultanSessionId",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "DoctorNote",
                table: "ConsultationSessions");

            migrationBuilder.DropColumn(
                name: "RecordUrl",
                table: "ConsultationSessions");

            migrationBuilder.RenameColumn(
                name: "ConsultationSessionConsultanSessionId",
                table: "Ratings",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Ratings_ConsultationSessionConsultanSessionId",
                table: "Ratings",
                newName: "IX_Ratings_SessionId");

            migrationBuilder.RenameColumn(
                name: "AppointmentId1",
                table: "ConsultationSessions",
                newName: "SessionId");

            migrationBuilder.AddColumn<string>(
                name: "DoctorNotes",
                table: "ConsultationSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConsultationSessions",
                table: "ConsultationSessions",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionsByDoctor_ConsultationSessions_SessionId",
                table: "PrescriptionsByDoctor",
                column: "SessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_ConsultationSessions_SessionId",
                table: "Ratings",
                column: "SessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
