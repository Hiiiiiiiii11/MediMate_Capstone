using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediMateRepository.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Ratings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Ratings");
        }
    }
}
