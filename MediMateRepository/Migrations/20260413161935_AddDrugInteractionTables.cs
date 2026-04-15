using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class AddDrugInteractionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Drugs",
                columns: table => new
                {
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugBankId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drugs", x => x.DrugId);
                });

            migrationBuilder.CreateTable(
                name: "DrugInteractions",
                columns: table => new
                {
                    InteractionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugId = table.Column<Guid>(type: "uuid", nullable: false),
                    InteractingDrugBankId = table.Column<string>(type: "text", nullable: false),
                    InteractingDrugName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugInteractions", x => x.InteractionId);
                    table.ForeignKey(
                        name: "FK_DrugInteractions_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drugs",
                        principalColumn: "DrugId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugInteractions_DrugId",
                table: "DrugInteractions",
                column: "DrugId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugInteractions");

            migrationBuilder.DropTable(
                name: "Drugs");
        }
    }
}
