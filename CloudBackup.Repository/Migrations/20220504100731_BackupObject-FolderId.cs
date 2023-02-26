using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBackup.Repository.Migrations
{
    public partial class BackupObjectFolderId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FolderId",
                table: "BackupObjects",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "BackupObjects");
        }
    }
}
