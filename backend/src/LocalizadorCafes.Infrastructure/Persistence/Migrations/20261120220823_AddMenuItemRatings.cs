using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalizadorCafes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_reviews_menu_rating",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "MenuRating",
                table: "reviews");

            migrationBuilder.CreateTable(
                name: "menu_item_ratings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_item_ratings", x => x.Id);
                    table.CheckConstraint("CK_menu_item_ratings_score", "\"Score\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_menu_item_ratings_menu_items_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "menu_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_menu_item_ratings_reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_ratings_MenuItemId",
                table: "menu_item_ratings",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_ratings_ReviewId_MenuItemId",
                table: "menu_item_ratings",
                columns: new[] { "ReviewId", "MenuItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MenuRating",
                table: "reviews",
                type: "integer",
                nullable: true);

            // Se recupera una puntuación única a partir de la media de los platos.
            migrationBuilder.Sql(
                "UPDATE reviews r SET \"MenuRating\" = COALESCE((SELECT ROUND(AVG(mir.\"Score\"))::int FROM menu_item_ratings mir WHERE mir.\"ReviewId\" = r.\"Id\"), 5);");

            migrationBuilder.AlterColumn<int>(
                name: "MenuRating",
                table: "reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_reviews_menu_rating",
                table: "reviews",
                sql: "\"MenuRating\" BETWEEN 1 AND 5");

            migrationBuilder.DropTable(
                name: "menu_item_ratings");
        }
    }
}
