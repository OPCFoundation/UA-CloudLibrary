using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class RenameAvailableModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_ModelUri1_ModelPublicationDate",
                table: "RequiredModelInfo");

            migrationBuilder.RenameColumn(
                name: "ModelUri1",
                table: "RequiredModelInfo",
                newName: "AvailableModelModelUri");

            migrationBuilder.RenameColumn(
                name: "ModelPublicationDate",
                table: "RequiredModelInfo",
                newName: "AvailableModelPublicationDate");

            migrationBuilder.RenameIndex(
                name: "IX_RequiredModelInfo_ModelUri1_ModelPublicationDate",
                table: "RequiredModelInfo",
                newName: "IX_RequiredModelInfo_AvailableModelModelUri_AvailableModelPubl~");

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_AvailableModelModelUri_Available~",
                table: "RequiredModelInfo",
                columns: new[] { "AvailableModelModelUri", "AvailableModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_AvailableModelModelUri_Available~",
                table: "RequiredModelInfo");

            migrationBuilder.RenameColumn(
                name: "AvailableModelPublicationDate",
                table: "RequiredModelInfo",
                newName: "ModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "AvailableModelModelUri",
                table: "RequiredModelInfo",
                newName: "ModelUri1");

            migrationBuilder.RenameIndex(
                name: "IX_RequiredModelInfo_AvailableModelModelUri_AvailableModelPubl~",
                table: "RequiredModelInfo",
                newName: "IX_RequiredModelInfo_ModelUri1_ModelPublicationDate");

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_ModelUri1_ModelPublicationDate",
                table: "RequiredModelInfo",
                columns: new[] { "ModelUri1", "ModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });
        }
    }
}
