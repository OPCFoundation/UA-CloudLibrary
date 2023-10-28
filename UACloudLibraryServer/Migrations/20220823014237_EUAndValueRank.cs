using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class EUAndValueRank : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPublica~",
                table: "DataTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_DataVariables_Nodes_NodeModelNodeId_NodeModelNodeSetModelUr~",
                table: "DataVariables");

            migrationBuilder.DropForeignKey(
                name: "FK_DataVariables_NodeSets_NodeSetModelModelUri_NodeSetModelPub~",
                table: "DataVariables");

            migrationBuilder.DropForeignKey(
                name: "FK_Interfaces_Nodes_NodeModelNodeId1_NodeModelNodeSetModelUri1~",
                table: "Interfaces");

            migrationBuilder.DropForeignKey(
                name: "FK_Interfaces_NodeSets_NodeSetModelModelUri1_NodeSetModelPubli~",
                table: "Interfaces");

            migrationBuilder.DropForeignKey(
                name: "FK_Methods_BaseTypes_TypeDefinitionNodeId_TypeDefinitionNodeSe~",
                table: "Methods");

            migrationBuilder.DropForeignKey(
                name: "FK_Methods_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNode~",
                table: "Methods");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNode~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_NodeSets_NodeSetModelModelUri_NodeSetModelPublicati~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_ObjectTypes_Nodes_NodeModelNodeId_NodeModelNodeSetModelUri_~",
                table: "ObjectTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_ObjectTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPubli~",
                table: "ObjectTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_NodeSets_NodeSetModelModelUri_NodeSetModelPublic~",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPu~",
                table: "ReferenceTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_NodeSetModelModelUri_NodeSetMode~",
                table: "RequiredModelInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_Variables_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNo~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPub~",
                table: "VariableTypes");

            migrationBuilder.DropTable(
                name: "ChildAndReference");

            migrationBuilder.DropIndex(
                name: "IX_VariableTypes_NodeSetModelModelUri_NodeSetModelPublicationD~",
                table: "VariableTypes");

            migrationBuilder.DropIndex(
                name: "IX_Variables_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetP~",
                table: "Variables");

            migrationBuilder.DropIndex(
                name: "IX_Properties_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_ObjectTypes_NodeModelNodeId_NodeModelNodeSetModelUri_NodeMo~",
                table: "ObjectTypes");

            migrationBuilder.DropIndex(
                name: "IX_Interfaces_NodeModelNodeId1_NodeModelNodeSetModelUri1_NodeM~",
                table: "Interfaces");

            migrationBuilder.DropIndex(
                name: "IX_DataVariables_NodeModelNodeId_NodeModelNodeSetModelUri_Node~",
                table: "DataVariables");

            migrationBuilder.DropIndex(
                name: "IX_DataVariables_NodeSetModelModelUri_NodeSetModelPublicationD~",
                table: "DataVariables");

            migrationBuilder.DropColumn(
                name: "ParentNodeSetPublicationDate",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "NodeModelNodeId",
                table: "ObjectTypes");

            migrationBuilder.DropColumn(
                name: "NodeModelNodeSetModelUri",
                table: "ObjectTypes");

            migrationBuilder.DropColumn(
                name: "NodeModelNodeSetPublicationDate",
                table: "ObjectTypes");

            migrationBuilder.DropColumn(
                name: "NodeModelNodeId1",
                table: "Interfaces");

            migrationBuilder.DropColumn(
                name: "NodeModelNodeSetModelUri1",
                table: "Interfaces");

            migrationBuilder.DropColumn(
                name: "NodeModelNodeSetPublicationDate1",
                table: "Interfaces");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate",
                table: "VariableTypes",
                newName: "NodeSetVariableTypesPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri",
                table: "VariableTypes",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "UserAccessLevel",
                table: "Variables",
                newName: "EngUnitAccessLevel");

            migrationBuilder.RenameColumn(
                name: "ParentNodeSetModelUri",
                table: "Variables",
                newName: "EngUnitModelingRule");

            migrationBuilder.RenameColumn(
                name: "ParentNodeId",
                table: "Variables",
                newName: "EURangeNodeId");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate",
                table: "RequiredModelInfo",
                newName: "DependentPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri",
                table: "RequiredModelInfo",
                newName: "DependentModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate",
                table: "ReferenceTypes",
                newName: "NodeSetReferenceTypesPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri",
                table: "ReferenceTypes",
                newName: "NodeSetReferenceTypesModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_ReferenceTypes_NodeSetModelModelUri_NodeSetModelPublication~",
                table: "ReferenceTypes",
                newName: "IX_ReferenceTypes_NodeSetReferenceTypesModelUri_NodeSetReferen~");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate",
                table: "Properties",
                newName: "ParentPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri",
                table: "Properties",
                newName: "ParentNodeId");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate",
                table: "ObjectTypes",
                newName: "NodeSetObjectTypesPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri",
                table: "ObjectTypes",
                newName: "NodeSetObjectTypesModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_ObjectTypes_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "ObjectTypes",
                newName: "IX_ObjectTypes_NodeSetObjectTypesModelUri_NodeSetObjectTypesPu~");

            migrationBuilder.RenameColumn(
                name: "ParentNodeSetPublicationDate",
                table: "Objects",
                newName: "ParentPublicationDate");

            migrationBuilder.RenameColumn(
                name: "ParentNodeSetModelUri",
                table: "Objects",
                newName: "ParentModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate",
                table: "Objects",
                newName: "NodeSetObjectsPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri",
                table: "Objects",
                newName: "NodeSetObjectsModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_Objects_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetPub~",
                table: "Objects",
                newName: "IX_Objects_ParentNodeId_ParentModelUri_ParentPublicationDate");

            migrationBuilder.RenameIndex(
                name: "IX_Objects_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "Objects",
                newName: "IX_Objects_NodeSetObjectsModelUri_NodeSetObjectsPublicationDate");

            migrationBuilder.RenameColumn(
                name: "Namespace",
                table: "Nodes",
                newName: "ReleaseStatus");

            migrationBuilder.RenameColumn(
                name: "ParentNodeSetPublicationDate",
                table: "Methods",
                newName: "ParentPublicationDate");

            migrationBuilder.RenameColumn(
                name: "ParentNodeSetModelUri",
                table: "Methods",
                newName: "ParentModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_Methods_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetPub~",
                table: "Methods",
                newName: "IX_Methods_ParentNodeId_ParentModelUri_ParentPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate1",
                table: "Interfaces",
                newName: "NodeSetInterfacesPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri1",
                table: "Interfaces",
                newName: "NodeSetInterfacesModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_Interfaces_NodeSetModelModelUri1_NodeSetModelPublicationDat~",
                table: "Interfaces",
                newName: "IX_Interfaces_NodeSetInterfacesModelUri_NodeSetInterfacesPubli~");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate",
                table: "DataVariables",
                newName: "ParentPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri",
                table: "DataVariables",
                newName: "ParentNodeId");

            migrationBuilder.RenameColumn(
                name: "NodeModelNodeSetPublicationDate",
                table: "DataVariables",
                newName: "NodeSetDataVariablesPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeModelNodeSetModelUri",
                table: "DataVariables",
                newName: "ParentModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeModelNodeId",
                table: "DataVariables",
                newName: "NodeSetDataVariablesModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelPublicationDate",
                table: "DataTypes",
                newName: "NodeSetDataTypesPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetModelModelUri",
                table: "DataTypes",
                newName: "NodeSetDataTypesModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_DataTypes_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "DataTypes",
                newName: "IX_DataTypes_NodeSetDataTypesModelUri_NodeSetDataTypesPublicat~");

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
                name: "NodeSetVariableTypesModelUri",
                table: "VariableTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValueRank",
                table: "VariableTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EURangeAccessLevel",
                table: "Variables",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EURangeModelingRule",
                table: "Variables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinimumSamplingInterval",
                table: "Variables",
                type: "double precision",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "UaEnumField_DisplayName",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "UaEnumField_Description",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "StructureField_Description",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArrayDimensions",
                table: "StructureField",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FieldOrder",
                table: "StructureField",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "ReferenceTypes_InverseName",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeSetPropertiesModelUri",
                table: "Properties",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NodeSetPropertiesPublicationDate",
                table: "Properties",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentModelUri",
                table: "Properties",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Nodes_DisplayName",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Nodes_Description",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeSetUnknownNodesModelUri",
                table: "Nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NodeSetUnknownNodesPublicationDate",
                table: "Nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOptionSet",
                table: "DataTypes",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataVariableModelNodeModel",
                columns: table => new {
                    DataVariablesNodeId = table.Column<string>(type: "text", nullable: false),
                    DataVariablesNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    DataVariablesNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodesWithDataVariablesNodeId = table.Column<string>(type: "text", nullable: false),
                    NodesWithDataVariablesNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodesWithDataVariablesNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DataVariableModelNodeModel", x => new { x.DataVariablesNodeId, x.DataVariablesNodeSetModelUri, x.DataVariablesNodeSetPublicationDate, x.NodesWithDataVariablesNodeId, x.NodesWithDataVariablesNodeSetModelUri, x.NodesWithDataVariablesNodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_DataVariableModelNodeModel_DataVariables_DataVariablesNodeI~",
                        columns: x => new { x.DataVariablesNodeId, x.DataVariablesNodeSetModelUri, x.DataVariablesNodeSetPublicationDate },
                        principalTable: "DataVariables",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataVariableModelNodeModel_Nodes_NodesWithDataVariablesNode~",
                        columns: x => new { x.NodesWithDataVariablesNodeId, x.NodesWithDataVariablesNodeSetModelUri, x.NodesWithDataVariablesNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterfaceModelNodeModel",
                columns: table => new {
                    InterfacesNodeId = table.Column<string>(type: "text", nullable: false),
                    InterfacesNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    InterfacesNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodesWithInterfaceNodeId = table.Column<string>(type: "text", nullable: false),
                    NodesWithInterfaceNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodesWithInterfaceNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_InterfaceModelNodeModel", x => new { x.InterfacesNodeId, x.InterfacesNodeSetModelUri, x.InterfacesNodeSetPublicationDate, x.NodesWithInterfaceNodeId, x.NodesWithInterfaceNodeSetModelUri, x.NodesWithInterfaceNodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_InterfaceModelNodeModel_Interfaces_InterfacesNodeId_Interfa~",
                        columns: x => new { x.InterfacesNodeId, x.InterfacesNodeSetModelUri, x.InterfacesNodeSetPublicationDate },
                        principalTable: "Interfaces",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterfaceModelNodeModel_Nodes_NodesWithInterfaceNodeId_Node~",
                        columns: x => new { x.NodesWithInterfaceNodeId, x.NodesWithInterfaceNodeSetModelUri, x.NodesWithInterfaceNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

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
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MethodModelNodeModel_Nodes_NodesWithMethodsNodeId_NodesWith~",
                        columns: x => new { x.NodesWithMethodsNodeId, x.NodesWithMethodsNodeSetModelUri, x.NodesWithMethodsNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeModelObjectModel",
                columns: table => new {
                    NodesWithObjectsNodeId = table.Column<string>(type: "text", nullable: false),
                    NodesWithObjectsNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodesWithObjectsNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ObjectsNodeId = table.Column<string>(type: "text", nullable: false),
                    ObjectsNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    ObjectsNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_NodeModelObjectModel", x => new { x.NodesWithObjectsNodeId, x.NodesWithObjectsNodeSetModelUri, x.NodesWithObjectsNodeSetPublicationDate, x.ObjectsNodeId, x.ObjectsNodeSetModelUri, x.ObjectsNodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_NodeModelObjectModel_Nodes_NodesWithObjectsNodeId_NodesWith~",
                        columns: x => new { x.NodesWithObjectsNodeId, x.NodesWithObjectsNodeSetModelUri, x.NodesWithObjectsNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeModelObjectModel_Objects_ObjectsNodeId_ObjectsNodeSetMo~",
                        columns: x => new { x.ObjectsNodeId, x.ObjectsNodeSetModelUri, x.ObjectsNodeSetPublicationDate },
                        principalTable: "Objects",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
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
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeModelObjectTypeModel_ObjectTypes_EventsNodeId_EventsNod~",
                        columns: x => new { x.EventsNodeId, x.EventsNodeSetModelUri, x.EventsNodeSetPublicationDate },
                        principalTable: "ObjectTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeModelVariableModel",
                columns: table => new {
                    NodesWithPropertiesNodeId = table.Column<string>(type: "text", nullable: false),
                    NodesWithPropertiesNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodesWithPropertiesNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PropertiesNodeId = table.Column<string>(type: "text", nullable: false),
                    PropertiesNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    PropertiesNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_NodeModelVariableModel", x => new { x.NodesWithPropertiesNodeId, x.NodesWithPropertiesNodeSetModelUri, x.NodesWithPropertiesNodeSetPublicationDate, x.PropertiesNodeId, x.PropertiesNodeSetModelUri, x.PropertiesNodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_NodeModelVariableModel_Nodes_NodesWithPropertiesNodeId_Node~",
                        columns: x => new { x.NodesWithPropertiesNodeId, x.NodesWithPropertiesNodeSetModelUri, x.NodesWithPropertiesNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeModelVariableModel_Variables_PropertiesNodeId_Propertie~",
                        columns: x => new { x.PropertiesNodeId, x.PropertiesNodeSetModelUri, x.PropertiesNodeSetPublicationDate },
                        principalTable: "Variables",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
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
                name: "IX_VariableTypes_NodeSetVariableTypesModelUri_NodeSetVariableT~",
                table: "VariableTypes",
                columns: new[] { "NodeSetVariableTypesModelUri", "NodeSetVariableTypesPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_NodeSetPropertiesModelUri_NodeSetPropertiesPubli~",
                table: "Properties",
                columns: new[] { "NodeSetPropertiesModelUri", "NodeSetPropertiesPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ParentNodeId_ParentModelUri_ParentPublicationDate",
                table: "Properties",
                columns: new[] { "ParentNodeId", "ParentModelUri", "ParentPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_NodeSetUnknownNodesModelUri_NodeSetUnknownNodesPublic~",
                table: "Nodes",
                columns: new[] { "NodeSetUnknownNodesModelUri", "NodeSetUnknownNodesPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataVariables_NodeSetDataVariablesModelUri_NodeSetDataVaria~",
                table: "DataVariables",
                columns: new[] { "NodeSetDataVariablesModelUri", "NodeSetDataVariablesPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataVariables_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "DataVariables",
                columns: new[] { "ParentNodeId", "ParentModelUri", "ParentPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataVariableModelNodeModel_NodesWithDataVariablesNodeId_Nod~",
                table: "DataVariableModelNodeModel",
                columns: new[] { "NodesWithDataVariablesNodeId", "NodesWithDataVariablesNodeSetModelUri", "NodesWithDataVariablesNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InterfaceModelNodeModel_NodesWithInterfaceNodeId_NodesWithI~",
                table: "InterfaceModelNodeModel",
                columns: new[] { "NodesWithInterfaceNodeId", "NodesWithInterfaceNodeSetModelUri", "NodesWithInterfaceNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MethodModelNodeModel_NodesWithMethodsNodeId_NodesWithMethod~",
                table: "MethodModelNodeModel",
                columns: new[] { "NodesWithMethodsNodeId", "NodesWithMethodsNodeSetModelUri", "NodesWithMethodsNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeModelObjectModel_ObjectsNodeId_ObjectsNodeSetModelUri_O~",
                table: "NodeModelObjectModel",
                columns: new[] { "ObjectsNodeId", "ObjectsNodeSetModelUri", "ObjectsNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeModelObjectTypeModel_NodesWithEventsNodeId_NodesWithEve~",
                table: "NodeModelObjectTypeModel",
                columns: new[] { "NodesWithEventsNodeId", "NodesWithEventsNodeSetModelUri", "NodesWithEventsNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeModelVariableModel_PropertiesNodeId_PropertiesNodeSetMo~",
                table: "NodeModelVariableModel",
                columns: new[] { "PropertiesNodeId", "PropertiesNodeSetModelUri", "PropertiesNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencedNodes_ReferencedNodeId_ReferencedModel~",
                table: "Nodes_OtherReferencedNodes",
                columns: new[] { "ReferencedNodeId", "ReferencedModelUri", "ReferencedPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_DataTypes_NodeSets_NodeSetDataTypesModelUri_NodeSetDataType~",
                table: "DataTypes",
                columns: new[] { "NodeSetDataTypesModelUri", "NodeSetDataTypesPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_DataVariables_Nodes_ParentNodeId_ParentModelUri_ParentPubli~",
                table: "DataVariables",
                columns: new[] { "ParentNodeId", "ParentModelUri", "ParentPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_DataVariables_NodeSets_NodeSetDataVariablesModelUri_NodeSet~",
                table: "DataVariables",
                columns: new[] { "NodeSetDataVariablesModelUri", "NodeSetDataVariablesPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Interfaces_NodeSets_NodeSetInterfacesModelUri_NodeSetInterf~",
                table: "Interfaces",
                columns: new[] { "NodeSetInterfacesModelUri", "NodeSetInterfacesPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetM~",
                table: "Methods",
                columns: new[] { "TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate" },
                principalTable: "Methods",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Methods",
                columns: new[] { "ParentNodeId", "ParentModelUri", "ParentPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_NodeSets_NodeSetUnknownNodesModelUri_NodeSetUnknownNo~",
                table: "Nodes",
                columns: new[] { "NodeSetUnknownNodesModelUri", "NodeSetUnknownNodesPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Objects",
                columns: new[] { "ParentNodeId", "ParentModelUri", "ParentPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_NodeSets_NodeSetObjectsModelUri_NodeSetObjectsPubli~",
                table: "Objects",
                columns: new[] { "NodeSetObjectsModelUri", "NodeSetObjectsPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectTypes_NodeSets_NodeSetObjectTypesModelUri_NodeSetObje~",
                table: "ObjectTypes",
                columns: new[] { "NodeSetObjectTypesModelUri", "NodeSetObjectTypesPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Nodes_ParentNodeId_ParentModelUri_ParentPublicat~",
                table: "Properties",
                columns: new[] { "ParentNodeId", "ParentModelUri", "ParentPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_NodeSets_NodeSetPropertiesModelUri_NodeSetProper~",
                table: "Properties",
                columns: new[] { "NodeSetPropertiesModelUri", "NodeSetPropertiesPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTypes_NodeSets_NodeSetReferenceTypesModelUri_NodeS~",
                table: "ReferenceTypes",
                columns: new[] { "NodeSetReferenceTypesModelUri", "NodeSetReferenceTypesPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_DependentModelUri_DependentPubli~",
                table: "RequiredModelInfo",
                columns: new[] { "DependentModelUri", "DependentPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" },
                principalTable: "BaseTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_NodeSets_NodeSetVariableTypesModelUri_NodeSet~",
                table: "VariableTypes",
                columns: new[] { "NodeSetVariableTypesModelUri", "NodeSetVariableTypesPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataTypes_NodeSets_NodeSetDataTypesModelUri_NodeSetDataType~",
                table: "DataTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_DataVariables_Nodes_ParentNodeId_ParentModelUri_ParentPubli~",
                table: "DataVariables");

            migrationBuilder.DropForeignKey(
                name: "FK_DataVariables_NodeSets_NodeSetDataVariablesModelUri_NodeSet~",
                table: "DataVariables");

            migrationBuilder.DropForeignKey(
                name: "FK_Interfaces_NodeSets_NodeSetInterfacesModelUri_NodeSetInterf~",
                table: "Interfaces");

            migrationBuilder.DropForeignKey(
                name: "FK_Methods_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetM~",
                table: "Methods");

            migrationBuilder.DropForeignKey(
                name: "FK_Methods_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Methods");

            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_NodeSets_NodeSetUnknownNodesModelUri_NodeSetUnknownNo~",
                table: "Nodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_NodeSets_NodeSetObjectsModelUri_NodeSetObjectsPubli~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_ObjectTypes_NodeSets_NodeSetObjectTypesModelUri_NodeSetObje~",
                table: "ObjectTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Nodes_ParentNodeId_ParentModelUri_ParentPublicat~",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_NodeSets_NodeSetPropertiesModelUri_NodeSetProper~",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceTypes_NodeSets_NodeSetReferenceTypesModelUri_NodeS~",
                table: "ReferenceTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_DependentModelUri_DependentPubli~",
                table: "RequiredModelInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_NodeSets_NodeSetVariableTypesModelUri_NodeSet~",
                table: "VariableTypes");

            migrationBuilder.DropTable(
                name: "DataVariableModelNodeModel");

            migrationBuilder.DropTable(
                name: "InterfaceModelNodeModel");

            migrationBuilder.DropTable(
                name: "MethodModelNodeModel");

            migrationBuilder.DropTable(
                name: "NodeModelObjectModel");

            migrationBuilder.DropTable(
                name: "NodeModelObjectTypeModel");

            migrationBuilder.DropTable(
                name: "NodeModelVariableModel");

            migrationBuilder.DropTable(
                name: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropIndex(
                name: "IX_VariableTypes_DataTypeNodeId_DataTypeNodeSetModelUri_DataTy~",
                table: "VariableTypes");

            migrationBuilder.DropIndex(
                name: "IX_VariableTypes_NodeSetVariableTypesModelUri_NodeSetVariableT~",
                table: "VariableTypes");

            migrationBuilder.DropIndex(
                name: "IX_Properties_NodeSetPropertiesModelUri_NodeSetPropertiesPubli~",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_ParentNodeId_ParentModelUri_ParentPublicationDate",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_NodeSetUnknownNodesModelUri_NodeSetUnknownNodesPublic~",
                table: "Nodes");

            migrationBuilder.DropIndex(
                name: "IX_DataVariables_NodeSetDataVariablesModelUri_NodeSetDataVaria~",
                table: "DataVariables");

            migrationBuilder.DropIndex(
                name: "IX_DataVariables_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "DataVariables");

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
                name: "NodeSetVariableTypesModelUri",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "ValueRank",
                table: "VariableTypes");

            migrationBuilder.DropColumn(
                name: "EURangeAccessLevel",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "EURangeModelingRule",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "MinimumSamplingInterval",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "ArrayDimensions",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "FieldOrder",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "MaxStringLength",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "ValueRank",
                table: "StructureField");

            migrationBuilder.DropColumn(
                name: "NodeSetPropertiesModelUri",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "NodeSetPropertiesPublicationDate",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ParentModelUri",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "NodeSetUnknownNodesModelUri",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "NodeSetUnknownNodesPublicationDate",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "IsOptionSet",
                table: "DataTypes");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "VariableTypes",
                newName: "NodeSetModelModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeSetVariableTypesPublicationDate",
                table: "VariableTypes",
                newName: "NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "EngUnitModelingRule",
                table: "Variables",
                newName: "ParentNodeSetModelUri");

            migrationBuilder.RenameColumn(
                name: "EngUnitAccessLevel",
                table: "Variables",
                newName: "UserAccessLevel");

            migrationBuilder.RenameColumn(
                name: "EURangeNodeId",
                table: "Variables",
                newName: "ParentNodeId");

            migrationBuilder.RenameColumn(
                name: "DependentPublicationDate",
                table: "RequiredModelInfo",
                newName: "NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "DependentModelUri",
                table: "RequiredModelInfo",
                newName: "NodeSetModelModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeSetReferenceTypesPublicationDate",
                table: "ReferenceTypes",
                newName: "NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetReferenceTypesModelUri",
                table: "ReferenceTypes",
                newName: "NodeSetModelModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_ReferenceTypes_NodeSetReferenceTypesModelUri_NodeSetReferen~",
                table: "ReferenceTypes",
                newName: "IX_ReferenceTypes_NodeSetModelModelUri_NodeSetModelPublication~");

            migrationBuilder.RenameColumn(
                name: "ParentPublicationDate",
                table: "Properties",
                newName: "NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "ParentNodeId",
                table: "Properties",
                newName: "NodeSetModelModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeSetObjectTypesPublicationDate",
                table: "ObjectTypes",
                newName: "NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetObjectTypesModelUri",
                table: "ObjectTypes",
                newName: "NodeSetModelModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_ObjectTypes_NodeSetObjectTypesModelUri_NodeSetObjectTypesPu~",
                table: "ObjectTypes",
                newName: "IX_ObjectTypes_NodeSetModelModelUri_NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "ParentPublicationDate",
                table: "Objects",
                newName: "ParentNodeSetPublicationDate");

            migrationBuilder.RenameColumn(
                name: "ParentModelUri",
                table: "Objects",
                newName: "ParentNodeSetModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeSetObjectsPublicationDate",
                table: "Objects",
                newName: "NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetObjectsModelUri",
                table: "Objects",
                newName: "NodeSetModelModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_Objects_ParentNodeId_ParentModelUri_ParentPublicationDate",
                table: "Objects",
                newName: "IX_Objects_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetPub~");

            migrationBuilder.RenameIndex(
                name: "IX_Objects_NodeSetObjectsModelUri_NodeSetObjectsPublicationDate",
                table: "Objects",
                newName: "IX_Objects_NodeSetModelModelUri_NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "ReleaseStatus",
                table: "Nodes",
                newName: "Namespace");

            migrationBuilder.RenameColumn(
                name: "ParentPublicationDate",
                table: "Methods",
                newName: "ParentNodeSetPublicationDate");

            migrationBuilder.RenameColumn(
                name: "ParentModelUri",
                table: "Methods",
                newName: "ParentNodeSetModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_Methods_ParentNodeId_ParentModelUri_ParentPublicationDate",
                table: "Methods",
                newName: "IX_Methods_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetPub~");

            migrationBuilder.RenameColumn(
                name: "NodeSetInterfacesPublicationDate",
                table: "Interfaces",
                newName: "NodeSetModelPublicationDate1");

            migrationBuilder.RenameColumn(
                name: "NodeSetInterfacesModelUri",
                table: "Interfaces",
                newName: "NodeSetModelModelUri1");

            migrationBuilder.RenameIndex(
                name: "IX_Interfaces_NodeSetInterfacesModelUri_NodeSetInterfacesPubli~",
                table: "Interfaces",
                newName: "IX_Interfaces_NodeSetModelModelUri1_NodeSetModelPublicationDat~");

            migrationBuilder.RenameColumn(
                name: "ParentPublicationDate",
                table: "DataVariables",
                newName: "NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "ParentNodeId",
                table: "DataVariables",
                newName: "NodeSetModelModelUri");

            migrationBuilder.RenameColumn(
                name: "ParentModelUri",
                table: "DataVariables",
                newName: "NodeModelNodeSetModelUri");

            migrationBuilder.RenameColumn(
                name: "NodeSetDataVariablesPublicationDate",
                table: "DataVariables",
                newName: "NodeModelNodeSetPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetDataVariablesModelUri",
                table: "DataVariables",
                newName: "NodeModelNodeId");

            migrationBuilder.RenameColumn(
                name: "NodeSetDataTypesPublicationDate",
                table: "DataTypes",
                newName: "NodeSetModelPublicationDate");

            migrationBuilder.RenameColumn(
                name: "NodeSetDataTypesModelUri",
                table: "DataTypes",
                newName: "NodeSetModelModelUri");

            migrationBuilder.RenameIndex(
                name: "IX_DataTypes_NodeSetDataTypesModelUri_NodeSetDataTypesPublicat~",
                table: "DataTypes",
                newName: "IX_DataTypes_NodeSetModelModelUri_NodeSetModelPublicationDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "ParentNodeSetPublicationDate",
                table: "Variables",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "UaEnumField_DisplayName",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "UaEnumField_Description",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "StructureField_Description",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "ReferenceTypes_InverseName",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "NodeModelNodeId",
                table: "ObjectTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeModelNodeSetModelUri",
                table: "ObjectTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NodeModelNodeSetPublicationDate",
                table: "ObjectTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Nodes_DisplayName",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Nodes_Description",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "NodeModelNodeId1",
                table: "Interfaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeModelNodeSetModelUri1",
                table: "Interfaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NodeModelNodeSetPublicationDate1",
                table: "Interfaces",
                type: "timestamp with time zone",
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
                name: "IX_VariableTypes_NodeSetModelModelUri_NodeSetModelPublicationD~",
                table: "VariableTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Variables_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetP~",
                table: "Variables",
                columns: new[] { "ParentNodeId", "ParentNodeSetModelUri", "ParentNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "Properties",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTypes_NodeModelNodeId_NodeModelNodeSetModelUri_NodeMo~",
                table: "ObjectTypes",
                columns: new[] { "NodeModelNodeId", "NodeModelNodeSetModelUri", "NodeModelNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Interfaces_NodeModelNodeId1_NodeModelNodeSetModelUri1_NodeM~",
                table: "Interfaces",
                columns: new[] { "NodeModelNodeId1", "NodeModelNodeSetModelUri1", "NodeModelNodeSetPublicationDate1" });

            migrationBuilder.CreateIndex(
                name: "IX_DataVariables_NodeModelNodeId_NodeModelNodeSetModelUri_Node~",
                table: "DataVariables",
                columns: new[] { "NodeModelNodeId", "NodeModelNodeSetModelUri", "NodeModelNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataVariables_NodeSetModelModelUri_NodeSetModelPublicationD~",
                table: "DataVariables",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildAndReference_ChildNodeId_ChildNodeSetModelUri_ChildNod~",
                table: "ChildAndReference",
                columns: new[] { "ChildNodeId", "ChildNodeSetModelUri", "ChildNodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_DataTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPublica~",
                table: "DataTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_DataVariables_Nodes_NodeModelNodeId_NodeModelNodeSetModelUr~",
                table: "DataVariables",
                columns: new[] { "NodeModelNodeId", "NodeModelNodeSetModelUri", "NodeModelNodeSetPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_DataVariables_NodeSets_NodeSetModelModelUri_NodeSetModelPub~",
                table: "DataVariables",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Interfaces_Nodes_NodeModelNodeId1_NodeModelNodeSetModelUri1~",
                table: "Interfaces",
                columns: new[] { "NodeModelNodeId1", "NodeModelNodeSetModelUri1", "NodeModelNodeSetPublicationDate1" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Interfaces_NodeSets_NodeSetModelModelUri1_NodeSetModelPubli~",
                table: "Interfaces",
                columns: new[] { "NodeSetModelModelUri1", "NodeSetModelPublicationDate1" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_BaseTypes_TypeDefinitionNodeId_TypeDefinitionNodeSe~",
                table: "Methods",
                columns: new[] { "TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate" },
                principalTable: "BaseTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNode~",
                table: "Methods",
                columns: new[] { "ParentNodeId", "ParentNodeSetModelUri", "ParentNodeSetPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNode~",
                table: "Objects",
                columns: new[] { "ParentNodeId", "ParentNodeSetModelUri", "ParentNodeSetPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_NodeSets_NodeSetModelModelUri_NodeSetModelPublicati~",
                table: "Objects",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectTypes_Nodes_NodeModelNodeId_NodeModelNodeSetModelUri_~",
                table: "ObjectTypes",
                columns: new[] { "NodeModelNodeId", "NodeModelNodeSetModelUri", "NodeModelNodeSetPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPubli~",
                table: "ObjectTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_NodeSets_NodeSetModelModelUri_NodeSetModelPublic~",
                table: "Properties",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPu~",
                table: "ReferenceTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_NodeSetModelModelUri_NodeSetMode~",
                table: "RequiredModelInfo",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNo~",
                table: "Variables",
                columns: new[] { "ParentNodeId", "ParentNodeSetModelUri", "ParentNodeSetPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPub~",
                table: "VariableTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" },
                principalTable: "NodeSets",
                principalColumns: new[] { "ModelUri", "PublicationDate" });
        }
    }
}
