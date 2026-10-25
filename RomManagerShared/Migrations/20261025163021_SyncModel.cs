using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RomManagerShared.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Discriminator",
                table: "Roms",
                newName: "RomType");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Roms",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "RelatedGameId",
                table: "Roms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Update_RelatedGameId",
                table: "Roms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roms_RelatedGameId",
                table: "Roms",
                column: "RelatedGameId");

            migrationBuilder.CreateIndex(
                name: "IX_Roms_Update_RelatedGameId",
                table: "Roms",
                column: "Update_RelatedGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Roms_Roms_RelatedGameId",
                table: "Roms",
                column: "RelatedGameId",
                principalTable: "Roms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Roms_Roms_Update_RelatedGameId",
                table: "Roms",
                column: "Update_RelatedGameId",
                principalTable: "Roms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roms_Roms_RelatedGameId",
                table: "Roms");

            migrationBuilder.DropForeignKey(
                name: "FK_Roms_Roms_Update_RelatedGameId",
                table: "Roms");

            migrationBuilder.DropIndex(
                name: "IX_Roms_RelatedGameId",
                table: "Roms");

            migrationBuilder.DropIndex(
                name: "IX_Roms_Update_RelatedGameId",
                table: "Roms");

            migrationBuilder.DropColumn(
                name: "RelatedGameId",
                table: "Roms");

            migrationBuilder.DropColumn(
                name: "Update_RelatedGameId",
                table: "Roms");

            migrationBuilder.RenameColumn(
                name: "RomType",
                table: "Roms",
                newName: "Discriminator");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Roms",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);
        }
    }
}
