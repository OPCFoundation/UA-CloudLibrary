using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class removereferenceid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Nodes_OtherReferencingNodes");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Nodes_OtherReferencedNodes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Nodes_OtherReferencingNodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Nodes_OtherReferencedNodes",
                type: "text",
                nullable: true);
        }
    }
}
