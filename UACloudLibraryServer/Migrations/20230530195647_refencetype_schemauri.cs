using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class refencetype_schemauri : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Variables_BaseTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes");

            migrationBuilder.AddColumn<string>(
                name: "XmlSchemaUri",
                table: "NodeSets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceTypeModelUri",
                table: "Nodes_OtherReferencingNodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceTypeNodeId",
                table: "Nodes_OtherReferencingNodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferenceTypePublicationDate",
                table: "Nodes_OtherReferencingNodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceTypeModelUri",
                table: "Nodes_OtherReferencedNodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceTypeNodeId",
                table: "Nodes_OtherReferencedNodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferenceTypePublicationDate",
                table: "Nodes_OtherReferencedNodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencingNodes_ReferenceTypeNodeId_ReferenceTy~",
                table: "Nodes_OtherReferencingNodes",
                columns: new[] { "ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencedNodes_ReferenceTypeNodeId_ReferenceTyp~",
                table: "Nodes_OtherReferencedNodes",
                columns: new[] { "ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferenceTypeNodeId_Refere~",
                table: "Nodes_OtherReferencedNodes",
                columns: new[] { "ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferenceTypeNodeId_Refer~",
                table: "Nodes_OtherReferencingNodes",
                columns: new[] { "ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_DataTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" },
                principalTable: "DataTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_DataTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" },
                principalTable: "DataTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferenceTypeNodeId_Refere~",
                table: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferenceTypeNodeId_Refer~",
                table: "Nodes_OtherReferencingNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Variables_DataTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_DataTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_OtherReferencingNodes_ReferenceTypeNodeId_ReferenceTy~",
                table: "Nodes_OtherReferencingNodes");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_OtherReferencedNodes_ReferenceTypeNodeId_ReferenceTyp~",
                table: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropColumn(
                name: "XmlSchemaUri",
                table: "NodeSets");

            migrationBuilder.DropColumn(
                name: "ReferenceTypeModelUri",
                table: "Nodes_OtherReferencingNodes");

            migrationBuilder.DropColumn(
                name: "ReferenceTypeNodeId",
                table: "Nodes_OtherReferencingNodes");

            migrationBuilder.DropColumn(
                name: "ReferenceTypePublicationDate",
                table: "Nodes_OtherReferencingNodes");

            migrationBuilder.DropColumn(
                name: "ReferenceTypeModelUri",
                table: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropColumn(
                name: "ReferenceTypeNodeId",
                table: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropColumn(
                name: "ReferenceTypePublicationDate",
                table: "Nodes_OtherReferencedNodes");

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_BaseTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" },
                principalTable: "BaseTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" },
                principalTable: "BaseTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
