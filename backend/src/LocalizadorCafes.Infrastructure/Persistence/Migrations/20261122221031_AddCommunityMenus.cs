using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalizadorCafes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityMenus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddedByUserId",
                table: "menu_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OsmId",
                table: "cafes",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_AddedByUserId",
                table: "menu_items",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_cafes_OsmId",
                table: "cafes",
                column: "OsmId",
                unique: true,
                filter: "\"OsmId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_menu_items_AspNetUsers_AddedByUserId",
                table: "menu_items",
                column: "AddedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_menu_items_AspNetUsers_AddedByUserId",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "IX_menu_items_AddedByUserId",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "IX_cafes_OsmId",
                table: "cafes");

            migrationBuilder.DropColumn(
                name: "AddedByUserId",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "OsmId",
                table: "cafes");
        }
    }
}
