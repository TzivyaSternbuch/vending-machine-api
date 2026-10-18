using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Requests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndProductTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Code = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ProductTypes",
                columns: table => new
                {
                    Code = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryCode = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.Code);
                    table.ForeignKey(
                        name: "FK_ProductTypes_ProductCategories_CategoryCode",
                        column: x => x.CategoryCode,
                        principalTable: "ProductCategories",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { 1, "drink" },
                    { 2, "snack" },
                    { 3, "candy" }
                });

            migrationBuilder.InsertData(
                table: "ProductTypes",
                columns: new[] { "Code", "CategoryCode", "Name" },
                values: new object[,]
                {
                    { 1, 1, "water" },
                    { 2, 1, "cola" },
                    { 3, 1, "juice" },
                    { 4, 2, "chips" },
                    { 5, 2, "pretzels" },
                    { 6, 3, "chocolate" },
                    { 7, 3, "gum" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Name",
                table: "ProductCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_CategoryCode",
                table: "ProductTypes",
                column: "CategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_Name",
                table: "ProductTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductTypes");

            migrationBuilder.DropTable(
                name: "ProductCategories");
        }
    }
}
