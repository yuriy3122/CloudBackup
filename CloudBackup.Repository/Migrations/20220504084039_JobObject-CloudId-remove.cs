using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBackup.Repository.Migrations
{
    public partial class JobObjectCloudIdremove : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudId",
                table: "JobObjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudId",
                table: "JobObjects",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
