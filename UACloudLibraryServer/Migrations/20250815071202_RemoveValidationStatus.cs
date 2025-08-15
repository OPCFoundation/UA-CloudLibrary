using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class RemoveValidationStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationStatus",
                table: "NamespaceMeta");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ValidationStatus",
                table: "NamespaceMeta",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
