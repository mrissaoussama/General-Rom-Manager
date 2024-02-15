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
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UID = table.Column<string>(type: "TEXT", nullable: false),
                    TitleID = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: false),
                    Publisher = table.Column<string>(type: "TEXT", nullable: false)
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
                name: "DLC",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumGameUpdate = table.Column<string>(type: "TEXT", nullable: false),
                    DLCId = table.Column<int>(type: "INTEGER", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUDLC_RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUDLC_RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
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
                    IsFolderFormat = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DLC", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DLC_DLC_DLCId",
                        column: x => x.DLCId,
                        principalTable: "DLC",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DLCUpdate",
                columns: table => new
                {
                    DLCsId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DLCUpdate", x => new { x.DLCsId, x.UpdatesId });
                    table.ForeignKey(
                        name: "FK_DLCUpdate_DLC_DLCsId",
                        column: x => x.DLCsId,
                        principalTable: "DLC",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DLCId = table.Column<int>(type: "INTEGER", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateId = table.Column<int>(type: "INTEGER", nullable: true),
                    DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
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
                    IsFolderFormat = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Game_DLC_DLCId",
                        column: x => x.DLCId,
                        principalTable: "DLC",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Game_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Update",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    UpdateId = table.Column<int>(type: "INTEGER", nullable: true),
                    DiskNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    DiskTotal = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentalLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUUpdate_RelatedGameTitleID = table.Column<string>(type: "TEXT", nullable: true),
                    WiiUUpdate_RelatedGameTitleName = table.Column<string>(type: "TEXT", nullable: true),
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
                    IsFolderFormat = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Update", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Update_Game_Id",
                        column: x => x.Id,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Update_Update_UpdateId",
                        column: x => x.UpdateId,
                        principalTable: "Update",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Rating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    RatingContents = table.Column<string>(type: "TEXT", nullable: false),
                    DLCId = table.Column<int>(type: "INTEGER", nullable: true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rating", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rating_DLC_DLCId",
                        column: x => x.DLCId,
                        principalTable: "DLC",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Rating_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Rating_Update_UpdateId",
                        column: x => x.UpdateId,
                        principalTable: "Update",
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
                    DLCId = table.Column<int>(type: "INTEGER", nullable: true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomDescription_DLC_DLCId",
                        column: x => x.DLCId,
                        principalTable: "DLC",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RomDescription_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RomDescription_Update_UpdateId",
                        column: x => x.UpdateId,
                        principalTable: "Update",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RomHash",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Extension = table.Column<string>(type: "TEXT", nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    DLCId = table.Column<int>(type: "INTEGER", nullable: true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomHash", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomHash_DLC_DLCId",
                        column: x => x.DLCId,
                        principalTable: "DLC",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RomHash_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RomHash_Update_UpdateId",
                        column: x => x.UpdateId,
                        principalTable: "Update",
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
                    DLCId = table.Column<int>(type: "INTEGER", nullable: true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RomTitle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomTitle_DLC_DLCId",
                        column: x => x.DLCId,
                        principalTable: "DLC",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RomTitle_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RomTitle_Update_UpdateId",
                        column: x => x.UpdateId,
                        principalTable: "Update",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RomHashProperty",
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
                    table.PrimaryKey("PK_RomHashProperty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RomHashProperty_RomHash_RomHashId",
                        column: x => x.RomHashId,
                        principalTable: "RomHash",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DLC_DLCId",
                table: "DLC",
                column: "DLCId");

            migrationBuilder.CreateIndex(
                name: "IX_DLCUpdate_UpdatesId",
                table: "DLCUpdate",
                column: "UpdatesId");

            migrationBuilder.CreateIndex(
                name: "IX_Game_DLCId",
                table: "Game",
                column: "DLCId");

            migrationBuilder.CreateIndex(
                name: "IX_Game_GameId",
                table: "Game",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Game_UpdateId",
                table: "Game",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_Rating_DLCId",
                table: "Rating",
                column: "DLCId");

            migrationBuilder.CreateIndex(
                name: "IX_Rating_GameId",
                table: "Rating",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Rating_UpdateId",
                table: "Rating",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_RomDescription_DLCId",
                table: "RomDescription",
                column: "DLCId");

            migrationBuilder.CreateIndex(
                name: "IX_RomDescription_GameId",
                table: "RomDescription",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RomDescription_UpdateId",
                table: "RomDescription",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_RomHash_DLCId",
                table: "RomHash",
                column: "DLCId");

            migrationBuilder.CreateIndex(
                name: "IX_RomHash_GameId",
                table: "RomHash",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RomHash_UpdateId",
                table: "RomHash",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_RomHashProperty_RomHashId",
                table: "RomHashProperty",
                column: "RomHashId");

            migrationBuilder.CreateIndex(
                name: "IX_RomTitle_DLCId",
                table: "RomTitle",
                column: "DLCId");

            migrationBuilder.CreateIndex(
                name: "IX_RomTitle_GameId",
                table: "RomTitle",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RomTitle_UpdateId",
                table: "RomTitle",
                column: "UpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_Update_UpdateId",
                table: "Update",
                column: "UpdateId");

            migrationBuilder.AddForeignKey(
                name: "FK_DLC_Game_Id",
                table: "DLC",
                column: "Id",
                principalTable: "Game",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DLCUpdate_Update_UpdatesId",
                table: "DLCUpdate",
                column: "UpdatesId",
                principalTable: "Update",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Game_Update_UpdateId",
                table: "Game",
                column: "UpdateId",
                principalTable: "Update",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DLC_Game_Id",
                table: "DLC");

            migrationBuilder.DropForeignKey(
                name: "FK_Update_Game_Id",
                table: "Update");

            migrationBuilder.DropTable(
                name: "DLCUpdate");

            migrationBuilder.DropTable(
                name: "Rating");

            migrationBuilder.DropTable(
                name: "RomDescription");

            migrationBuilder.DropTable(
                name: "RomHashProperty");

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
                name: "RomHash");

            migrationBuilder.DropTable(
                name: "Game");

            migrationBuilder.DropTable(
                name: "DLC");

            migrationBuilder.DropTable(
                name: "Update");
        }
    }
}
