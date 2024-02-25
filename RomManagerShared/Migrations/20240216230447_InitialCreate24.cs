using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RomManagerShared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WiiUWikiBrewTitleDTOs_ProductCode",
                table: "WiiUWikiBrewTitleDTOs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WiiUWikiBrewTitleDTOs_ProductCode",
                table: "WiiUWikiBrewTitleDTOs",
                column: "ProductCode",
                unique: true);
        }
    }
}
