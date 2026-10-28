using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RomManagerShared.Migrations
{
    /// <inheritdoc />
    public partial class RomMetadataOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RomMetadataOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Console = table.Column<string>(type: "TEXT", nullable: false),
                    RomKey = table.Column<string>(type: "TEXT", nullable: false),
                    RomType = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    Developer = table.Column<string>(type: "TEXT", nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    CoverPath = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomMetadataOverrides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RomMetadataOverrides_Console_RomKey_RomType",
                table: "RomMetadataOverrides",
                columns: new[] { "Console", "RomKey", "RomType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RomMetadataOverrides");
        }
    }
}
