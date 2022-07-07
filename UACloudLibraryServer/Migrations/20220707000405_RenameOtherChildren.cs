using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class RenameOtherChildren : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Methods_BaseTypes_TypeDefinitionNodeId_TypeDefinitionNodeSe~",
                table: "Methods");

            migrationBuilder.DropTable(
                name: "ChildAndReference");

            migrationBuilder.DropColumn(
                name: "Namespace",
                table: "Nodes");

            migrationBuilder.AddColumn<string>(
                name: "ArrayDimensions",
                table: "VariableTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataTypeNodeId",
                table: "VariableTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataTypeNodeSetModelUri",
                table: "VariableTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataTypeNodeSetPublicationDate",
                table: "VariableTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "VariableTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueRank",
                table: "VariableTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EURangeModelingRule",
                table: "Variables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EURangeNodeId",
                table: "Variables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngUnitModelingRule",
                table: "Variables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArrayDimensions",
                table: "StructureField",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MaxStringLength",
                table: "StructureField",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueRank",
                table: "StructureField",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOptionSet",
                table: "DataTypes",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Nodes_OtherReferencedNodes",
                columns: table => new {
                    OwnerNodeId = table.Column<string>(type: "text", nullable: false),
                    OwnerModelUri = table.Column<string>(type: "text", nullable: false),
                    OwnerPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferencedNodeId = table.Column<string>(type: "text", nullable: true),
                    ReferencedModelUri = table.Column<string>(type: "text", nullable: true),
                    ReferencedPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Nodes_OtherReferencedNodes", x => new { x.OwnerNodeId, x.OwnerModelUri, x.OwnerPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencedNodes_Nodes_OwnerNodeId_OwnerModelUri_~",
                        columns: x => new { x.OwnerNodeId, x.OwnerModelUri, x.OwnerPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferencedNodeId_Reference~",
                        columns: x => new { x.ReferencedNodeId, x.ReferencedModelUri, x.ReferencedPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_VariableTypes_DataTypeNodeId_DataTypeNodeSetModelUri_DataTy~",
                table: "VariableTypes",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencedNodes_ReferencedNodeId_ReferencedModel~",
                table: "Nodes_OtherReferencedNodes",
                columns: new[] { "ReferencedNodeId", "ReferencedModelUri", "ReferencedPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetM~",
                table: "Methods",
                columns: new[] { "TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate" },
                principalTable: "Methods",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" },
                principalTable: "BaseTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Methods_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetM~",
                table: "Methods");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes");

            migrationBuilder.DropTable(
                name: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropIndex(
                name: "IX_VariableTypes_DataTypeNodeId_DataTypeNodeSetModelUri_DataTy~",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "ArrayDimensions",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeId",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeSetModelUri",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeSetPublicationDate",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "ValueRank",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "EURangeModelingRule",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "EURangeNodeId",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "EngUnitModelingRule",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "ArrayDimensions",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "MaxStringLength",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "ValueRank",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "IsOptionSet",
                table: "DataTypes");

            migrationBuilder.AddColumn<string>(
                name: "Namespace",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChildAndReference",
                columns: table => new {
                    NodeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    NodeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChildNodeId = table.Column<string>(type: "text", nullable: true),
                    ChildNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    ChildNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_ChildAndReference", x => new { x.NodeModelNodeId, x.NodeModelNodeSetModelUri, x.NodeModelNodeSetPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_ChildAndReference_Nodes_ChildNodeId_ChildNodeSetModelUri_Ch~",
                        columns: x => new { x.ChildNodeId, x.ChildNodeSetModelUri, x.ChildNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_ChildAndReference_Nodes_NodeModelNodeId_NodeModelNodeSetMod~",
                        columns: x => new { x.NodeModelNodeId, x.NodeModelNodeSetModelUri, x.NodeModelNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildAndReference_ChildNodeId_ChildNodeSetModelUri_ChildNod~",
                table: "ChildAndReference",
                columns: new[] { "ChildNodeId", "ChildNodeSetModelUri", "ChildNodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_BaseTypes_TypeDefinitionNodeId_TypeDefinitionNodeSe~",
                table: "Methods",
                columns: new[] { "TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate" },
                principalTable: "BaseTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
        }
    }
}
