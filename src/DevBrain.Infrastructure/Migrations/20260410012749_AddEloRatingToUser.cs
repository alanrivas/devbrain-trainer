using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevBrain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEloRatingToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EloRating",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 1000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EloRating",
                table: "users");
        }
    }
}
