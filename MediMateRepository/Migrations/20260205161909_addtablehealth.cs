using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addtablehealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthProfiles",
                columns: table => new
                {
                    HealthProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    BloodType = table.Column<string>(type: "text", nullable: false),
                    Height = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    InsuranceNumber = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthProfiles", x => x.HealthProfileId);
                    table.ForeignKey(
                        name: "FK_HealthProfiles_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthConditions",
                columns: table => new
                {
                    ConditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConditionName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DiagnosedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthConditions", x => x.ConditionId);
                    table.ForeignKey(
                        name: "FK_HealthConditions_HealthProfiles_HealthProfileId",
                        column: x => x.HealthProfileId,
                        principalTable: "HealthProfiles",
                        principalColumn: "HealthProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthConditions_HealthProfileId",
                table: "HealthConditions",
                column: "HealthProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthProfiles_MemberId",
                table: "HealthProfiles",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthConditions");

            migrationBuilder.DropTable(
                name: "HealthProfiles");
        }
    }
}
