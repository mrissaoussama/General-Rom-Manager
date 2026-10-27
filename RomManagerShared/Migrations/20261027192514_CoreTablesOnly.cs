using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RomManagerShared.Migrations
{
    /// <inheritdoc />
    public partial class CoreTablesOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RomHashes_Roms_RomId",
                table: "RomHashes");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "RomDescription");

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
                name: "Roms");

            migrationBuilder.DropIndex(
                name: "IX_RomHashes_RomId",
                table: "RomHashes");

            migrationBuilder.DropColumn(
                name: "RomId",
                table: "RomHashes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RomId",
                table: "RomHashes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Roms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Banner = table.Column<string>(type: "TEXT", nullable: true),
                    Developer = table.Column<string>(type: "TEXT", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Images = table.Column<string>(type: "TEXT", nullable: true),
                    IsDemo = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFolderFormat = table.Column<bool>(type: "INTEGER", nullable: false),
                    Languages = table.Column<string>(type: "TEXT", nullable: true),
                    MinimumFirmware = table.Column<string>(type: "TEXT", nullable: true),
                    NumberOfPlayers = table.Column<int>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    Regions = table.Column<string>(type: "TEXT", nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RomType = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    Thumbnail = table.Column<string>(type: "TEXT", nullable: true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedGameId = table.Column<int>(type: "INTEGER", nullable: true),
                    MinimumGameUpdate = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUDLC_RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUDLC_RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPGame_Category = table.Column<string>(type: "TEXT", nullable: true),
                    PSPGame_DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPGame_DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPGame_ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Update_RelatedGameId = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPUpdate_Category = table.Column<string>(type: "TEXT", nullable: true),
                    PSPUpdate_DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPUpdate_DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    PSPUpdate_ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Roms_Roms_RelatedGameId",
                        column: x => x.RelatedGameId,
                        principalTable: "Roms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Roms_Roms_Update_RelatedGameId",
                        column: x => x.Update_RelatedGameId,
                        principalTable: "Roms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SwitchJsonRomDTOs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Banner = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Developer = table.Column<string>(type: "TEXT", nullable: true),
                    Genres = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Images = table.Column<string>(type: "TEXT", nullable: true),
                    IsDemo = table.Column<bool>(type: "INTEGER", nullable: true),
                    Languages = table.Column<string>(type: "TEXT", nullable: true),
                    NsuID = table.Column<long>(type: "INTEGER", nullable: true),
                    NumberOfPlayers = table.Column<int>(type: "INTEGER", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    RatingContent = table.Column<string>(type: "TEXT", nullable: true),
                    Region = table.Column<string>(type: "TEXT", nullable: true),
                    ReleaseDate = table.Column<int>(type: "INTEGER", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: true),
                    TitleName = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: true)
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
                    ProductCode = table.Column<string>(type: "TEXT", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    TitleID = table.Column<string>(type: "TEXT", nullable: true),
                    UID = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true)
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
                    CompanyCode = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: true),
                    Region = table.Column<string>(type: "TEXT", nullable: true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: true),
                    TitleType = table.Column<string>(type: "TEXT", nullable: true),
                    Versions = table.Column<string>(type: "TEXT", nullable: true)
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
                    Developer = table.Column<string>(type: "TEXT", nullable: false),
                    Languages = table.Column<string>(type: "TEXT", nullable: false),
                    Players = table.Column<int>(type: "INTEGER", nullable: false),
                    Publisher = table.Column<string>(type: "TEXT", nullable: false),
                    Region = table.Column<string>(type: "TEXT", nullable: false),
                    ReleaseDate = table.Column<int>(type: "INTEGER", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    Synopsis = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    TitleID = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WiiXmlRomDTOs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<int>(type: "INTEGER", nullable: false),
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
                    RomId = table.Column<int>(type: "INTEGER", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "RomTitle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Language = table.Column<int>(type: "INTEGER", nullable: false),
                    RomId = table.Column<int>(type: "INTEGER", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_RomHashes_RomId",
                table: "RomHashes",
                column: "RomId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_RomId",
                table: "Ratings",
                column: "RomId");

            migrationBuilder.CreateIndex(
                name: "IX_RomDescription_RomId",
                table: "RomDescription",
                column: "RomId");

            migrationBuilder.CreateIndex(
                name: "IX_Roms_GameId",
                table: "Roms",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Roms_RelatedGameId",
                table: "Roms",
                column: "RelatedGameId");

            migrationBuilder.CreateIndex(
                name: "IX_Roms_Update_RelatedGameId",
                table: "Roms",
                column: "Update_RelatedGameId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_RomHashes_Roms_RomId",
                table: "RomHashes",
                column: "RomId",
                principalTable: "Roms",
                principalColumn: "Id");
        }
    }
}
