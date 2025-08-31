using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LovelyFish.API.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddIsNewArrivalToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNewArrival",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNewArrival",
                table: "Products");
        }
    }
}
