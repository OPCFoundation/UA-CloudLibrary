using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class ReferencingNodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nodes_OtherReferencingNodes",
                columns: table => new {
                    OwnerNodeId = table.Column<string>(type: "text", nullable: false),
                    OwnerModelUri = table.Column<string>(type: "text", nullable: false),
                    OwnerPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferencingNodeId = table.Column<string>(type: "text", nullable: true),
                    ReferencingModelUri = table.Column<string>(type: "text", nullable: true),
                    ReferencingPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Nodes_OtherReferencingNodes", x => new { x.OwnerNodeId, x.OwnerModelUri, x.OwnerPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencingNodes_Nodes_OwnerNodeId_OwnerModelUri~",
                        columns: x => new { x.OwnerNodeId, x.OwnerModelUri, x.OwnerPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferencingNodeId_Referen~",
                        columns: x => new { x.ReferencingNodeId, x.ReferencingModelUri, x.ReferencingPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencingNodes_ReferencingNodeId_ReferencingMo~",
                table: "Nodes_OtherReferencingNodes",
                columns: new[] { "ReferencingNodeId", "ReferencingModelUri", "ReferencingPublicationDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nodes_OtherReferencingNodes");
        }
    }
}
