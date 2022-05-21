using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class AddReferenceTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferenceTypes",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodeSetModelModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeSetModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_ReferenceTypes", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_ReferenceTypes_Nodes_NodeId_NodeSetModelUri_NodeSetPublicat~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReferenceTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPu~",
                        columns: x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_ReferenceTypes_NodeSets_NodeSetModelUri_NodeSetPublicationD~",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTypes_NodeSetModelModelUri_NodeSetModelPublication~",
                table: "ReferenceTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTypes_NodeSetModelUri_NodeSetPublicationDate",
                table: "ReferenceTypes",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferenceTypes");
        }
    }
}
