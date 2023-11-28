using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class eventnotifier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "EventNotifier",
                table: "Objects",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeIdIdentifier",
                table: "Nodes",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventNotifier",
                table: "Objects");

            migrationBuilder.DropColumn(
                name: "NodeIdIdentifier",
                table: "Nodes");
        }
    }
}
