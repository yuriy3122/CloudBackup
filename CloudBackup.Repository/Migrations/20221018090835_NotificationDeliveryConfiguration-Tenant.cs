using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudBackup.Repository.Migrations
{
    public partial class NotificationDeliveryConfigurationTenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "NotificationDeliveryConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryConfigurations_TenantId",
                table: "NotificationDeliveryConfigurations",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationDeliveryConfigurations_Tenants_TenantId",
                table: "NotificationDeliveryConfigurations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationDeliveryConfigurations_Tenants_TenantId",
                table: "NotificationDeliveryConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveryConfigurations_TenantId",
                table: "NotificationDeliveryConfigurations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "NotificationDeliveryConfigurations");
        }
    }
}
