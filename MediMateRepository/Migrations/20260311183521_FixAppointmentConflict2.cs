using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class FixAppointmentConflict2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatDoctorMessages_ConsultationSessions_ConsultantSessionCo~",
                table: "ChatDoctorMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorAvailabilities_Doctors_DoctorId1",
                table: "DoctorAvailabilities");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionsByDoctor_ConsultationSessions_SessionId",
                table: "PrescriptionsByDoctor");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_ConsultationSessions_ConsultationSessionConsultanSe~",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Doctors_DoctorId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Members_MemberId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_ConsultationSessionConsultanSessionId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_DoctorAvailabilities_DoctorId1",
                table: "DoctorAvailabilities");

            migrationBuilder.DropIndex(
                name: "IX_ChatDoctorMessages_ConsultantSessionConsultanSessionId",
                table: "ChatDoctorMessages");

            migrationBuilder.DropColumn(
                name: "ConsultationSessionConsultanSessionId",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "DoctorId1",
                table: "DoctorAvailabilities");

            migrationBuilder.DropColumn(
                name: "ConsultantSessionConsultanSessionId",
                table: "ChatDoctorMessages");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "PrescriptionsByDoctor",
                newName: "ConsultanSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_PrescriptionsByDoctor_SessionId",
                table: "PrescriptionsByDoctor",
                newName: "IX_PrescriptionsByDoctor_ConsultanSessionId");

            migrationBuilder.CreateTable(
                name: "DoctorAvailabilityExceptions",
                columns: table => new
                {
                    ExceptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    IsAvailableOverride = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilityExceptions", x => x.ExceptionId);
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilityExceptions_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionsByDoctor_DoctorId",
                table: "PrescriptionsByDoctor",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionsByDoctor_MemberId",
                table: "PrescriptionsByDoctor",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatDoctorMessages_ConsultanSessionId",
                table: "ChatDoctorMessages",
                column: "ConsultanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilityExceptions_DoctorId",
                table: "DoctorAvailabilityExceptions",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatDoctorMessages_ConsultationSessions_ConsultanSessionId",
                table: "ChatDoctorMessages",
                column: "ConsultanSessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "ConsultanSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionsByDoctor_ConsultationSessions_ConsultanSession~",
                table: "PrescriptionsByDoctor",
                column: "ConsultanSessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "ConsultanSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionsByDoctor_Doctors_DoctorId",
                table: "PrescriptionsByDoctor",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "DoctorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionsByDoctor_Members_MemberId",
                table: "PrescriptionsByDoctor",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Doctors_DoctorId",
                table: "Ratings",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Members_MemberId",
                table: "Ratings",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatDoctorMessages_ConsultationSessions_ConsultanSessionId",
                table: "ChatDoctorMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionsByDoctor_ConsultationSessions_ConsultanSession~",
                table: "PrescriptionsByDoctor");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionsByDoctor_Doctors_DoctorId",
                table: "PrescriptionsByDoctor");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionsByDoctor_Members_MemberId",
                table: "PrescriptionsByDoctor");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Doctors_DoctorId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Members_MemberId",
                table: "Ratings");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilityExceptions");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionsByDoctor_DoctorId",
                table: "PrescriptionsByDoctor");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionsByDoctor_MemberId",
                table: "PrescriptionsByDoctor");

            migrationBuilder.DropIndex(
                name: "IX_ChatDoctorMessages_ConsultanSessionId",
                table: "ChatDoctorMessages");

            migrationBuilder.RenameColumn(
                name: "ConsultanSessionId",
                table: "PrescriptionsByDoctor",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_PrescriptionsByDoctor_ConsultanSessionId",
                table: "PrescriptionsByDoctor",
                newName: "IX_PrescriptionsByDoctor_SessionId");

            migrationBuilder.AddColumn<Guid>(
                name: "ConsultationSessionConsultanSessionId",
                table: "Ratings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DoctorId1",
                table: "DoctorAvailabilities",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ConsultantSessionConsultanSessionId",
                table: "ChatDoctorMessages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_ConsultationSessionConsultanSessionId",
                table: "Ratings",
                column: "ConsultationSessionConsultanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilities_DoctorId1",
                table: "DoctorAvailabilities",
                column: "DoctorId1");

            migrationBuilder.CreateIndex(
                name: "IX_ChatDoctorMessages_ConsultantSessionConsultanSessionId",
                table: "ChatDoctorMessages",
                column: "ConsultantSessionConsultanSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatDoctorMessages_ConsultationSessions_ConsultantSessionCo~",
                table: "ChatDoctorMessages",
                column: "ConsultantSessionConsultanSessionId",
                principalTable: "ConsultationSessions",
                principalColumn: "ConsultanSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorAvailabilities_Doctors_DoctorId1",
                table: "DoctorAvailabilities",
                column: "DoctorId1",
                principalTable: "Doctors",
                principalColumn: "DoctorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionsByDoctor_ConsultationSessions_SessionId",
                table: "PrescriptionsByDoctor",
                column: "SessionId",
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
    }
}
