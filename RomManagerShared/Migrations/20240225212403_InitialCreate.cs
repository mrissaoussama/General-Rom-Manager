using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RomManagerShared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoIntroHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HeaderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoIntroHeaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Regions = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    Thumbnail = table.Column<string>(type: "TEXT", nullable: true),
                    Languages = table.Column<string>(type: "TEXT", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    Developer = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Banner = table.Column<string>(type: "TEXT", nullable: true),
                    Images = table.Column<string>(type: "TEXT", nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: true),
                    MinimumFirmware = table.Column<string>(type: "TEXT", nullable: true),
                    NumberOfPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDemo = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFolderFormat = table.Column<bool>(type: "INTEGER", nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    MinimumGameUpdate = table.Column<string>(type: "TEXT", nullable: true),
                    DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUDLC_RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUDLC_RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPGame_DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPGame_DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPGame_ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPGame_Category = table.Column<string>(type: "TEXT", nullable: true),
                    PSPUpdate_DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPUpdate_DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPUpdate_ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPUpdate_Category = table.Column<string>(type: "TEXT", nullable: true),
                    SwitchUpdate_RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    SwitchUpdate_RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUUpdate_RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUUpdate_RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roms_Roms_GameId",
                        column: x => x.GameId,
                        principalTable: "Roms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SwitchJsonRomDTOs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TitleName = table.Column<string>(type: "TEXT", nullable: true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: true),
                    Region = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    RatingContent = table.Column<string>(type: "TEXT", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    Languages = table.Column<string>(type: "TEXT", nullable: true),
                    Developer = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsDemo = table.Column<bool>(type: "INTEGER", nullable: true),
                    NumberOfPlayers = table.Column<int>(type: "INTEGER", nullable: true),
                    ReleaseDate = table.Column<int>(type: "INTEGER", nullable: true),
                    Banner = table.Column<string>(type: "TEXT", nullable: true),
                    NsuID = table.Column<long>(type: "INTEGER", nullable: true),
                    Images = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwitchJsonRomDTOs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThreeDSJsonDTOs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    UID = table.Column<string>(type: "TEXT", nullable: true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreeDSJsonDTOs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WiiUWikiBrewTitleDTOs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Versions = table.Column<string>(type: "TEXT", nullable: true),
                    Region = table.Column<string>(type: "TEXT", nullable: true),
                    CompanyCode = table.Column<string>(type: "TEXT", nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: true),
                    TitleType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiiUWikiBrewTitleDTOs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WiiXmlRomDTOs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: false),
                    Region = table.Column<string>(type: "TEXT", nullable: false),
                    Languages = table.Column<string>(type: "TEXT", nullable: false),
                    Developer = table.Column<string>(type: "TEXT", nullable: false),
                    Publisher = table.Column<string>(type: "TEXT", nullable: false),
                    ReleaseDate = table.Column<int>(type: "INTEGER", nullable: true),
                    Players = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Synopsis = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiiXmlRomDTOs", x => x.Id);
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
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    RatingContents = table.Column<string>(type: "TEXT", nullable: false),
                    RomId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ratings_Roms_RomId",
                        column: x => x.RomId,
                        principalTable: "Roms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RomDescription",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Language = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    RomId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomDescription_Roms_RomId",
                        column: x => x.RomId,
                        principalTable: "Roms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RomHashes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Extension = table.Column<string>(type: "TEXT", nullable: true),
                    Filename = table.Column<string>(type: "TEXT", nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    RomId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomHashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomHashes_Roms_RomId",
                        column: x => x.RomId,
                        principalTable: "Roms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RomTitle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Language = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    RomId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomTitle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomTitle_Roms_RomId",
                        column: x => x.RomId,
                        principalTable: "Roms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NoIntroGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    GameId = table.Column<string>(type: "TEXT", nullable: true),
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
                name: "RomHashProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    RomHashId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomHashProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomHashProperties_RomHashes_RomHashId",
                        column: x => x.RomHashId,
                        principalTable: "RomHashes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NoIntroRoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Size = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    CRC = table.Column<string>(type: "TEXT", nullable: true),
                    MD5 = table.Column<string>(type: "TEXT", nullable: true),
                    SHA1 = table.Column<string>(type: "TEXT", nullable: true),
                    SHA256 = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Serial = table.Column<string>(type: "TEXT", nullable: true),
                    NoIntroGameId = table.Column<int>(type: "INTEGER", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_RomId",
                table: "Ratings",
                column: "RomId");

            migrationBuilder.CreateIndex(
                name: "IX_RomDescription_RomId",
                table: "RomDescription",
                column: "RomId");

            migrationBuilder.CreateIndex(
                name: "IX_RomHashes_RomId",
                table: "RomHashes",
                column: "RomId");

            migrationBuilder.CreateIndex(
                name: "IX_RomHashProperties_RomHashId",
                table: "RomHashProperties",
                column: "RomHashId");

            migrationBuilder.CreateIndex(
                name: "IX_Roms_GameId",
                table: "Roms",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RomTitle_RomId",
                table: "RomTitle",
                column: "RomId");

            migrationBuilder.CreateIndex(
                name: "IX_SwitchJsonRomDTOs_TitleID",
                table: "SwitchJsonRomDTOs",
                column: "TitleID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreeDSJsonDTOs_TitleID",
                table: "ThreeDSJsonDTOs",
                column: "TitleID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WiiXmlRomDTOs_TitleID",
                table: "WiiXmlRomDTOs",
                column: "TitleID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoIntroRoms");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "RomDescription");

            migrationBuilder.DropTable(
                name: "RomHashProperties");

            migrationBuilder.DropTable(
                name: "RomTitle");

            migrationBuilder.DropTable(
                name: "SwitchJsonRomDTOs");

            migrationBuilder.DropTable(
                name: "ThreeDSJsonDTOs");

            migrationBuilder.DropTable(
                name: "WiiUWikiBrewTitleDTOs");

            migrationBuilder.DropTable(
                name: "WiiXmlRomDTOs");

            migrationBuilder.DropTable(
                name: "NoIntroGames");

            migrationBuilder.DropTable(
                name: "RomHashes");

            migrationBuilder.DropTable(
                name: "NoIntroDataFiles");

            migrationBuilder.DropTable(
                name: "Roms");

            migrationBuilder.DropTable(
                name: "NoIntroHeaders");
        }
    }
}
