using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class StoreNodesetValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Values",
                table: "DbFiles",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Values",
                table: "DbFiles");
        }
    }
}
