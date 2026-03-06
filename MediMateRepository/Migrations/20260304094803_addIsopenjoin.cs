using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class addIsopenjoin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpenJoin",
                table: "Families",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOpenJoin",
                table: "Families");
        }
    }
}
