using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RomManagerShared.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNoIntroTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoIntroRoms");

            migrationBuilder.DropTable(
                name: "NoIntroGames");

            migrationBuilder.DropTable(
                name: "NoIntroDataFiles");

            migrationBuilder.DropTable(
                name: "NoIntroHeaders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoIntroHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    HeaderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoIntroHeaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NoIntroDataFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HeaderId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoIntroDataFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoIntroDataFiles_NoIntroHeaders_HeaderId",
                        column: x => x.HeaderId,
                        principalTable: "NoIntroHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NoIntroGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    GameId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NoIntroDataFileId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoIntroGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoIntroGames_NoIntroDataFiles_NoIntroDataFileId",
                        column: x => x.NoIntroDataFileId,
                        principalTable: "NoIntroDataFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NoIntroRoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CRC = table.Column<string>(type: "TEXT", nullable: true),
                    MD5 = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    NoIntroGameId = table.Column<int>(type: "INTEGER", nullable: true),
                    SHA1 = table.Column<string>(type: "TEXT", nullable: true),
                    SHA256 = table.Column<string>(type: "TEXT", nullable: true),
                    Serial = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoIntroRoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoIntroRoms_NoIntroGames_NoIntroGameId",
                        column: x => x.NoIntroGameId,
                        principalTable: "NoIntroGames",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoIntroDataFiles_HeaderId",
                table: "NoIntroDataFiles",
                column: "HeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_NoIntroGames_NoIntroDataFileId",
                table: "NoIntroGames",
                column: "NoIntroDataFileId");

            migrationBuilder.CreateIndex(
                name: "IX_NoIntroRoms_NoIntroGameId",
                table: "NoIntroRoms",
                column: "NoIntroGameId");
        }
    }
}
