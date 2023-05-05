using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class ModellingRule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModelingRule",
                table: "Variables",
                newName: "ModellingRule");

            migrationBuilder.RenameColumn(
                name: "EngUnitModelingRule",
                table: "Variables",
                newName: "EngUnitModellingRule");

            migrationBuilder.RenameColumn(
                name: "EURangeModelingRule",
                table: "Variables",
                newName: "EURangeModellingRule");

            migrationBuilder.RenameColumn(
                name: "ModelingRule",
                table: "Objects",
                newName: "ModellingRule");

            migrationBuilder.RenameColumn(
                name: "ModelingRule",
                table: "Methods",
                newName: "ModellingRule");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModellingRule",
                table: "Variables",
                newName: "ModelingRule");

            migrationBuilder.RenameColumn(
                name: "EngUnitModellingRule",
                table: "Variables",
                newName: "EngUnitModelingRule");

            migrationBuilder.RenameColumn(
                name: "EURangeModellingRule",
                table: "Variables",
                newName: "EURangeModelingRule");

            migrationBuilder.RenameColumn(
                name: "ModellingRule",
                table: "Objects",
                newName: "ModelingRule");

            migrationBuilder.RenameColumn(
                name: "ModellingRule",
                table: "Methods",
                newName: "ModelingRule");
        }
    }
}
