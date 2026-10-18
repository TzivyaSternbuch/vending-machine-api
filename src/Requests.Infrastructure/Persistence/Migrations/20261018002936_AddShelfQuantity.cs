using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Requests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShelfQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Shelves",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shelves_Quantity",
                table: "Shelves",
                sql: "\"Quantity\" >= 0 AND \"Quantity\" <= \"Capacity\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Shelves_Quantity",
                table: "Shelves");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Shelves");
        }
    }
}
