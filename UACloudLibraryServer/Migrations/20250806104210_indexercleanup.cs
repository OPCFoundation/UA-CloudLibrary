using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class IndexerCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseTypes_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUr~",
                table: "BaseTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Methods_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetM~",
                table: "Methods");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_ObjectTypes_TypeDefinitionNodeId_TypeDefinitionNode~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_StructureField_BaseTypes_DataTypeNodeId_DataTypeNodeSetMode~",
                table: "StructureField");

            migrationBuilder.DropForeignKey(
                name: "FK_Variables_DataTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_Variables_VariableTypes_TypeDefinitionNodeId_TypeDefinition~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_DataTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes");

            migrationBuilder.DropIndex(
                name: "IX_VariableTypes_DataTypeNodeId_DataTypeNodeSetModelUri_DataTy~",
                table: "VariableTypes");

            migrationBuilder.DropIndex(
                name: "IX_Variables_DataTypeNodeId_DataTypeNodeSetModelUri_DataTypeNo~",
                table: "Variables");

            migrationBuilder.DropIndex(
                name: "IX_Variables_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUr~",
                table: "Variables");

            migrationBuilder.DropIndex(
                name: "IX_StructureField_DataTypeNodeId_DataTypeNodeSetModelUri_DataT~",
                table: "StructureField");

            migrationBuilder.DropIndex(
                name: "IX_Objects_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUri_~",
                table: "Objects");

            migrationBuilder.DropIndex(
                name: "IX_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUri_~",
                table: "Methods");

            migrationBuilder.DropIndex(
                name: "IX_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUri_SuperTyp~",
                table: "BaseTypes");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeId",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeSetPublicationDate",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeId",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeSetModelUri",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeSetPublicationDate",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "TypeDefinitionNodeSetPublicationDate",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeId",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "DataTypeNodeSetPublicationDate",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "TypeDefinitionNodeId",
                table: "Objects");

            migrationBuilder.DropColumn(
                name: "TypeDefinitionNodeSetPublicationDate",
                table: "Objects");

            migrationBuilder.DropColumn(
                name: "TypeDefinitionNodeId",
                table: "Methods");

            migrationBuilder.DropColumn(
                name: "TypeDefinitionNodeSetPublicationDate",
                table: "Methods");

            migrationBuilder.DropColumn(
                name: "SuperTypeNodeId",
                table: "BaseTypes");

            migrationBuilder.DropColumn(
                name: "SuperTypeNodeSetPublicationDate",
                table: "BaseTypes");

            migrationBuilder.RenameColumn(
                name: "DataTypeNodeSetModelUri",
                table: "VariableTypes",
                newName: "DataType");

            migrationBuilder.RenameColumn(
                name: "TypeDefinitionNodeSetModelUri",
                table: "Variables",
                newName: "TypeDefinition");

            migrationBuilder.RenameColumn(
                name: "TypeDefinitionNodeId",
                table: "Variables",
                newName: "DataType");

            migrationBuilder.RenameColumn(
                name: "DataTypeNodeSetModelUri",
                table: "StructureField",
                newName: "DataType");

            migrationBuilder.RenameColumn(
                name: "TypeDefinitionNodeSetModelUri",
                table: "Objects",
                newName: "TypeDefinition");

            migrationBuilder.RenameColumn(
                name: "TypeDefinitionNodeSetModelUri",
                table: "Methods",
                newName: "TypeDefinition");

            migrationBuilder.RenameColumn(
                name: "SuperTypeNodeSetModelUri",
                table: "BaseTypes",
                newName: "SuperType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataType",
                table: "VariableTypes",
                newName: "DataTypeNodeSetModelUri");

            migrationBuilder.RenameColumn(
                name: "TypeDefinition",
                table: "Variables",
                newName: "TypeDefinitionNodeSetModelUri");

            migrationBuilder.RenameColumn(
                name: "DataType",
                table: "Variables",
                newName: "TypeDefinitionNodeId");

            migrationBuilder.RenameColumn(
                name: "DataType",
                table: "StructureField",
                newName: "DataTypeNodeSetModelUri");

            migrationBuilder.RenameColumn(
                name: "TypeDefinition",
                table: "Objects",
                newName: "TypeDefinitionNodeSetModelUri");

            migrationBuilder.RenameColumn(
                name: "TypeDefinition",
                table: "Methods",
                newName: "TypeDefinitionNodeSetModelUri");

            migrationBuilder.RenameColumn(
                name: "SuperType",
                table: "BaseTypes",
                newName: "SuperTypeNodeSetModelUri");

            migrationBuilder.AddColumn<string>(
                name: "DataTypeNodeId",
                table: "VariableTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataTypeNodeSetPublicationDate",
                table: "VariableTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataTypeNodeId",
                table: "Variables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataTypeNodeSetModelUri",
                table: "Variables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataTypeNodeSetPublicationDate",
                table: "Variables",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TypeDefinitionNodeSetPublicationDate",
                table: "Variables",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataTypeNodeId",
                table: "StructureField",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataTypeNodeSetPublicationDate",
                table: "StructureField",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeDefinitionNodeId",
                table: "Objects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TypeDefinitionNodeSetPublicationDate",
                table: "Objects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeDefinitionNodeId",
                table: "Methods",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TypeDefinitionNodeSetPublicationDate",
                table: "Methods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuperTypeNodeId",
                table: "BaseTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuperTypeNodeSetPublicationDate",
                table: "BaseTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariableTypes_DataTypeNodeId_DataTypeNodeSetModelUri_DataTy~",
                table: "VariableTypes",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Variables_DataTypeNodeId_DataTypeNodeSetModelUri_DataTypeNo~",
                table: "Variables",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Variables_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUr~",
                table: "Variables",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_StructureField_DataTypeNodeId_DataTypeNodeSetModelUri_DataT~",
                table: "StructureField",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Objects_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUri_~",
                table: "Objects",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUri_~",
                table: "Methods",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUri_SuperTyp~",
                table: "BaseTypes",
                columns: ["SuperTypeNodeId", "SuperTypeNodeSetModelUri", "SuperTypeNodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseTypes_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUr~",
                table: "BaseTypes",
                columns: ["SuperTypeNodeId", "SuperTypeNodeSetModelUri", "SuperTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetM~",
                table: "Methods",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"],
                principalTable: "Methods",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_ObjectTypes_TypeDefinitionNodeId_TypeDefinitionNode~",
                table: "Objects",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"],
                principalTable: "ObjectTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StructureField_BaseTypes_DataTypeNodeId_DataTypeNodeSetMode~",
                table: "StructureField",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_DataTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "DataTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_VariableTypes_TypeDefinitionNodeId_TypeDefinition~",
                table: "Variables",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"],
                principalTable: "VariableTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_DataTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "DataTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);
        }
    }
}
