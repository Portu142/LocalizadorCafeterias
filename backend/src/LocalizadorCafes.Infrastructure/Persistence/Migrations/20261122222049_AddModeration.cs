using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalizadorCafes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "menu_items",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "cafe_photos",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_Status",
                table: "menu_items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cafe_photos_Status",
                table: "cafe_photos",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_menu_items_Status",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "IX_cafe_photos_Status",
                table: "cafe_photos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "cafe_photos");
        }
    }
}
