using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class instrumentrange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InstrumentRangeAccessLevel",
                table: "Variables",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstrumentRangeModellingRule",
                table: "Variables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstrumentRangeNodeId",
                table: "Variables",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstrumentRangeAccessLevel",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "InstrumentRangeModellingRule",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "InstrumentRangeNodeId",
                table: "Variables");
        }
    }
}
