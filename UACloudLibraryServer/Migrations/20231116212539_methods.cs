using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Opc.Ua.Cloud.Library
{
    public partial class BasetypeModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NodeSetMethodsModelUri",
                table: "Methods",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NodeSetMethodsPublicationDate",
                table: "Methods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Methods_NodeSetMethodsModelUri_NodeSetMethodsPublicationDate",
                table: "Methods",
                columns: ["NodeSetMethodsModelUri", "NodeSetMethodsPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_NodeSets_NodeSetMethodsModelUri_NodeSetMethodsPubli~",
                table: "Methods",
                columns: ["NodeSetMethodsModelUri", "NodeSetMethodsPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Methods_NodeSets_NodeSetMethodsModelUri_NodeSetMethodsPubli~",
                table: "Methods");

            migrationBuilder.DropIndex(
                name: "IX_Methods_NodeSetMethodsModelUri_NodeSetMethodsPublicationDate",
                table: "Methods");

            migrationBuilder.DropColumn(
                name: "NodeSetMethodsModelUri",
                table: "Methods");

            migrationBuilder.DropColumn(
                name: "NodeSetMethodsPublicationDate",
                table: "Methods");
        }
    }
}
