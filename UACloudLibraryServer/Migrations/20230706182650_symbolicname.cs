using Microsoft.EntityFrameworkCore.Migrations;

namespace Opc.Ua.Cloud.Library
{
    public partial class SymbolicName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SymbolicName",
                table: "UaEnumField",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SymbolicName",
                table: "StructureField",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SymbolicName",
                table: "UaEnumField");

            migrationBuilder.DropColumn(
                name: "SymbolicName",
                table: "StructureField");
        }
    }
}
