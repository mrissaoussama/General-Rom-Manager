using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RomManagerShared.Migrations
{
    /// <inheritdoc />
    public partial class LibraryFileDataFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataFingerprint",
                table: "LibraryFiles",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataFingerprint",
                table: "LibraryFiles");
        }
    }
}
