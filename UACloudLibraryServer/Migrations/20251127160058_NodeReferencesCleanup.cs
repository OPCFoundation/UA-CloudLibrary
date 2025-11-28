using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class NodeReferencesCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MethodModelNodeModel");

            migrationBuilder.DropTable(
                name: "NodeModelObjectTypeModel");

            migrationBuilder.DropTable(
                name: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropTable(
                name: "Nodes_OtherReferencingNodes");

            migrationBuilder.AddColumn<string>(
                name: "MethodModelNodeId",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MethodModelNodeSetModelUri",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MethodModelNodeSetPublicationDate",
                table: "Nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectTypeModelNodeId",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectTypeModelNodeSetModelUri",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ObjectTypeModelNodeSetPublicationDate",
                table: "Nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerModelUri",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerNodeId",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnerPublicationDate",
                table: "Nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceTypeModelUri",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceTypeNodeId",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferenceTypePublicationDate",
                table: "Nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencedModelUri",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencedNodeId",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferencedPublicationDate",
                table: "Nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencingModelUri",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencingNodeId",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferencingPublicationDate",
                table: "Nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_MethodModelNodeId_MethodModelNodeSetModelUri_MethodMo~",
                table: "Nodes",
                columns: ["MethodModelNodeId", "MethodModelNodeSetModelUri", "MethodModelNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_ObjectTypeModelNodeId_ObjectTypeModelNodeSetModelUri_~",
                table: "Nodes",
                columns: ["ObjectTypeModelNodeId", "ObjectTypeModelNodeSetModelUri", "ObjectTypeModelNodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_Methods_MethodModelNodeId_MethodModelNodeSetModelUri_~",
                table: "Nodes",
                columns: ["MethodModelNodeId", "MethodModelNodeSetModelUri", "MethodModelNodeSetPublicationDate"],
                principalTable: "Methods",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_ObjectTypes_ObjectTypeModelNodeId_ObjectTypeModelNode~",
                table: "Nodes",
                columns: ["ObjectTypeModelNodeId", "ObjectTypeModelNodeSetModelUri", "ObjectTypeModelNodeSetPublicationDate"],
                principalTable: "ObjectTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_Methods_MethodModelNodeId_MethodModelNodeSetModelUri_~",
                table: "Nodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_ObjectTypes_ObjectTypeModelNodeId_ObjectTypeModelNode~",
                table: "Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_MethodModelNodeId_MethodModelNodeSetModelUri_MethodMo~",
                table: "Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_ObjectTypeModelNodeId_ObjectTypeModelNodeSetModelUri_~",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MethodModelNodeId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MethodModelNodeSetModelUri",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MethodModelNodeSetPublicationDate",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ObjectTypeModelNodeId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ObjectTypeModelNodeSetModelUri",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ObjectTypeModelNodeSetPublicationDate",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "OwnerModelUri",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "OwnerNodeId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "OwnerPublicationDate",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferenceTypeModelUri",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferenceTypeNodeId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferenceTypePublicationDate",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferencedModelUri",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferencedNodeId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferencedPublicationDate",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferencingModelUri",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferencingNodeId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ReferencingPublicationDate",
                table: "Nodes");

            migrationBuilder.CreateTable(
                name: "MethodModelNodeModel",
                columns: table => new {
                    MethodsNodeId = table.Column<string>(type: "text", nullable: false),
                    MethodsNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    MethodsNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodesWithMethodsNodeId = table.Column<string>(type: "text", nullable: false),
                    NodesWithMethodsNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodesWithMethodsNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_MethodModelNodeModel", x => new { x.MethodsNodeId, x.MethodsNodeSetModelUri, x.MethodsNodeSetPublicationDate, x.NodesWithMethodsNodeId, x.NodesWithMethodsNodeSetModelUri, x.NodesWithMethodsNodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_MethodModelNodeModel_Methods_MethodsNodeId_MethodsNodeSetMo~",
                        columns: x => new { x.MethodsNodeId, x.MethodsNodeSetModelUri, x.MethodsNodeSetPublicationDate },
                        principalTable: "Methods",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MethodModelNodeModel_Nodes_NodesWithMethodsNodeId_NodesWith~",
                        columns: x => new { x.NodesWithMethodsNodeId, x.NodesWithMethodsNodeSetModelUri, x.NodesWithMethodsNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeModelObjectTypeModel",
                columns: table => new {
                    EventsNodeId = table.Column<string>(type: "text", nullable: false),
                    EventsNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    EventsNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodesWithEventsNodeId = table.Column<string>(type: "text", nullable: false),
                    NodesWithEventsNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodesWithEventsNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_NodeModelObjectTypeModel", x => new { x.EventsNodeId, x.EventsNodeSetModelUri, x.EventsNodeSetPublicationDate, x.NodesWithEventsNodeId, x.NodesWithEventsNodeSetModelUri, x.NodesWithEventsNodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_NodeModelObjectTypeModel_Nodes_NodesWithEventsNodeId_NodesW~",
                        columns: x => new { x.NodesWithEventsNodeId, x.NodesWithEventsNodeSetModelUri, x.NodesWithEventsNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeModelObjectTypeModel_ObjectTypes_EventsNodeId_EventsNod~",
                        columns: x => new { x.EventsNodeId, x.EventsNodeSetModelUri, x.EventsNodeSetPublicationDate },
                        principalTable: "ObjectTypes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nodes_OtherReferencedNodes",
                columns: table => new {
                    OwnerNodeId = table.Column<string>(type: "text", nullable: false),
                    OwnerModelUri = table.Column<string>(type: "text", nullable: false),
                    OwnerPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferencedNodeId = table.Column<string>(type: "text", nullable: true),
                    ReferenceTypeNodeId = table.Column<string>(type: "text", nullable: true),
                    ReferencedModelUri = table.Column<string>(type: "text", nullable: true),
                    ReferenceTypeModelUri = table.Column<string>(type: "text", nullable: true),
                    ReferencedPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferenceTypePublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Nodes_OtherReferencedNodes", x => new { x.OwnerNodeId, x.OwnerModelUri, x.OwnerPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencedNodes_Nodes_OwnerNodeId_OwnerModelUri_~",
                        columns: x => new { x.OwnerNodeId, x.OwnerModelUri, x.OwnerPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferenceTypeNodeId_Refere~",
                        columns: x => new { x.ReferenceTypeNodeId, x.ReferenceTypeModelUri, x.ReferenceTypePublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferencedNodeId_Reference~",
                        columns: x => new { x.ReferencedNodeId, x.ReferencedModelUri, x.ReferencedPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nodes_OtherReferencingNodes",
                columns: table => new {
                    OwnerNodeId = table.Column<string>(type: "text", nullable: false),
                    OwnerModelUri = table.Column<string>(type: "text", nullable: false),
                    OwnerPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceTypeNodeId = table.Column<string>(type: "text", nullable: true),
                    ReferencingNodeId = table.Column<string>(type: "text", nullable: true),
                    ReferenceTypeModelUri = table.Column<string>(type: "text", nullable: true),
                    ReferencingModelUri = table.Column<string>(type: "text", nullable: true),
                    ReferenceTypePublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferencingPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Nodes_OtherReferencingNodes", x => new { x.OwnerNodeId, x.OwnerModelUri, x.OwnerPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencingNodes_Nodes_OwnerNodeId_OwnerModelUri~",
                        columns: x => new { x.OwnerNodeId, x.OwnerModelUri, x.OwnerPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferenceTypeNodeId_Refer~",
                        columns: x => new { x.ReferenceTypeNodeId, x.ReferenceTypeModelUri, x.ReferenceTypePublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferencingNodeId_Referen~",
                        columns: x => new { x.ReferencingNodeId, x.ReferencingModelUri, x.ReferencingPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MethodModelNodeModel_NodesWithMethodsNodeId_NodesWithMethod~",
                table: "MethodModelNodeModel",
                columns: ["NodesWithMethodsNodeId", "NodesWithMethodsNodeSetModelUri", "NodesWithMethodsNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_NodeModelObjectTypeModel_NodesWithEventsNodeId_NodesWithEve~",
                table: "NodeModelObjectTypeModel",
                columns: ["NodesWithEventsNodeId", "NodesWithEventsNodeSetModelUri", "NodesWithEventsNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencedNodes_ReferencedNodeId_ReferencedModel~",
                table: "Nodes_OtherReferencedNodes",
                columns: ["ReferencedNodeId", "ReferencedModelUri", "ReferencedPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencedNodes_ReferenceTypeNodeId_ReferenceTyp~",
                table: "Nodes_OtherReferencedNodes",
                columns: ["ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencingNodes_ReferenceTypeNodeId_ReferenceTy~",
                table: "Nodes_OtherReferencingNodes",
                columns: ["ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencingNodes_ReferencingNodeId_ReferencingMo~",
                table: "Nodes_OtherReferencingNodes",
                columns: ["ReferencingNodeId", "ReferencingModelUri", "ReferencingPublicationDate"]);
        }
    }
}
