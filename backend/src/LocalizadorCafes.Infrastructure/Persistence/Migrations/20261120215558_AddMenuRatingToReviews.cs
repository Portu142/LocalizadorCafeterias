using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalizadorCafes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuRatingToReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MenuRating",
                table: "reviews",
                type: "integer",
                nullable: true);

            // Las reseñas existentes heredan la puntuación general como
            // aproximación inicial de la puntuación del menú.
            migrationBuilder.Sql("UPDATE reviews SET \"MenuRating\" = \"Rating\";");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_reviews_menu_rating",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "MenuRating",
                table: "reviews");
        }
    }
}
