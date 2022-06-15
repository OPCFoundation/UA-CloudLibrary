using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class AddNodesetIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NodeSets",
                columns: table => new {
                    ModelUri = table.Column<string>(type: "text", nullable: false),
                    PublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: true),
                    Identifier = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    ValidationStatus = table.Column<string>(type: "text", nullable: true),
                    ValidationStatusInfo = table.Column<string>(type: "text", nullable: true),
                    ValidationElapsedTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ValidationFinishedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidationErrors = table.Column<string[]>(type: "text[]", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_NodeSets", x => new { x.ModelUri, x.PublicationDate });
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BrowseName = table.Column<string>(type: "text", nullable: true),
                    SymbolicName = table.Column<string>(type: "text", nullable: true),
                    Documentation = table.Column<string>(type: "text", nullable: true),
                    Namespace = table.Column<string>(type: "text", nullable: true),
                    Categories = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Nodes", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_Nodes_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequiredModelInfo",
                columns: table => new {
                    NodeSetModelModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModelUri = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "text", nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailableModelModelUri = table.Column<string>(type: "text", nullable: true),
                    AvailableModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_RequiredModelInfo", x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_RequiredModelInfo_NodeSets_AvailableModelModelUri_Available~",
                        columns: x => new { x.AvailableModelModelUri, x.AvailableModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_RequiredModelInfo_NodeSets_NodeSetModelModelUri_NodeSetMode~",
                        columns: x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseTypes",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAbstract = table.Column<bool>(type: "boolean", nullable: false),
                    SuperTypeNodeId = table.Column<string>(type: "text", nullable: true),
                    SuperTypeNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    SuperTypeNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_BaseTypes", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_BaseTypes_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUr~",
                        columns: x => new { x.SuperTypeNodeId, x.SuperTypeNodeSetModelUri, x.SuperTypeNodeSetPublicationDate },
                        principalTable: "BaseTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_BaseTypes_Nodes_NodeId_NodeSetModelUri_NodeSetPublicationDa~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaseTypes_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Nodes_Description",
                columns: table => new {
                    NodeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    NodeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Nodes_Description", x => new { x.NodeModelNodeId, x.NodeModelNodeSetModelUri, x.NodeModelNodeSetPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_Nodes_Description_Nodes_NodeModelNodeId_NodeModelNodeSetMod~",
                        columns: x => new { x.NodeModelNodeId, x.NodeModelNodeSetModelUri, x.NodeModelNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nodes_DisplayName",
                columns: table => new {
                    NodeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    NodeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Nodes_DisplayName", x => new { x.NodeModelNodeId, x.NodeModelNodeSetModelUri, x.NodeModelNodeSetPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_Nodes_DisplayName_Nodes_NodeModelNodeId_NodeModelNodeSetMod~",
                        columns: x => new { x.NodeModelNodeId, x.NodeModelNodeSetModelUri, x.NodeModelNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "DataTypes",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodeSetModelModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeSetModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DataTypes", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_DataTypes_BaseTypes_NodeId_NodeSetModelUri_NodeSetPublicati~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "BaseTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPublica~",
                        columns: x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_DataTypes_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Methods",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModelingRule = table.Column<string>(type: "text", nullable: true),
                    ParentNodeId = table.Column<string>(type: "text", nullable: true),
                    ParentNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    ParentNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TypeDefinitionNodeId = table.Column<string>(type: "text", nullable: true),
                    TypeDefinitionNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    TypeDefinitionNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Methods", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_Methods_BaseTypes_TypeDefinitionNodeId_TypeDefinitionNodeSe~",
                        columns: x => new { x.TypeDefinitionNodeId, x.TypeDefinitionNodeSetModelUri, x.TypeDefinitionNodeSetPublicationDate },
                        principalTable: "BaseTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_Methods_Nodes_NodeId_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Methods_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNode~",
                        columns: x => new { x.ParentNodeId, x.ParentNodeSetModelUri, x.ParentNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_Methods_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectTypes",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodeModelNodeId = table.Column<string>(type: "text", nullable: true),
                    NodeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NodeSetModelModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeSetModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_ObjectTypes", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_ObjectTypes_BaseTypes_NodeId_NodeSetModelUri_NodeSetPublica~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "BaseTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjectTypes_Nodes_NodeModelNodeId_NodeModelNodeSetModelUri_~",
                        columns: x => new { x.NodeModelNodeId, x.NodeModelNodeSetModelUri, x.NodeModelNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_ObjectTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPubli~",
                        columns: x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_ObjectTypes_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariableTypes",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodeSetModelModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeSetModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_VariableTypes", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_VariableTypes_BaseTypes_NodeId_NodeSetModelUri_NodeSetPubli~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "BaseTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VariableTypes_NodeSets_NodeSetModelModelUri_NodeSetModelPub~",
                        columns: x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_VariableTypes_NodeSets_NodeSetModelUri_NodeSetPublicationDa~",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StructureField",
                columns: table => new {
                    DataTypeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    DataTypeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    DataTypeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    DataTypeNodeId = table.Column<string>(type: "text", nullable: true),
                    DataTypeNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    DataTypeNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_StructureField", x => new { x.DataTypeModelNodeId, x.DataTypeModelNodeSetModelUri, x.DataTypeModelNodeSetPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_StructureField_BaseTypes_DataTypeNodeId_DataTypeNodeSetMode~",
                        columns: x => new { x.DataTypeNodeId, x.DataTypeNodeSetModelUri, x.DataTypeNodeSetPublicationDate },
                        principalTable: "BaseTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_StructureField_DataTypes_DataTypeModelNodeId_DataTypeModelN~",
                        columns: x => new { x.DataTypeModelNodeId, x.DataTypeModelNodeSetModelUri, x.DataTypeModelNodeSetPublicationDate },
                        principalTable: "DataTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UaEnumField",
                columns: table => new {
                    DataTypeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    DataTypeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    DataTypeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UaEnumField", x => new { x.DataTypeModelNodeId, x.DataTypeModelNodeSetModelUri, x.DataTypeModelNodeSetPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_UaEnumField_DataTypes_DataTypeModelNodeId_DataTypeModelNode~",
                        columns: x => new { x.DataTypeModelNodeId, x.DataTypeModelNodeSetModelUri, x.DataTypeModelNodeSetPublicationDate },
                        principalTable: "DataTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interfaces",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodeModelNodeId1 = table.Column<string>(type: "text", nullable: true),
                    NodeModelNodeSetModelUri1 = table.Column<string>(type: "text", nullable: true),
                    NodeModelNodeSetPublicationDate1 = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NodeSetModelModelUri1 = table.Column<string>(type: "text", nullable: true),
                    NodeSetModelPublicationDate1 = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Interfaces", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_Interfaces_Nodes_NodeModelNodeId1_NodeModelNodeSetModelUri1~",
                        columns: x => new { x.NodeModelNodeId1, x.NodeModelNodeSetModelUri1, x.NodeModelNodeSetPublicationDate1 },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_Interfaces_NodeSets_NodeSetModelModelUri1_NodeSetModelPubli~",
                        columns: x => new { x.NodeSetModelModelUri1, x.NodeSetModelPublicationDate1 },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_Interfaces_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Interfaces_ObjectTypes_NodeId_NodeSetModelUri_NodeSetPublic~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "ObjectTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Objects",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodeSetModelModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeSetModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModelingRule = table.Column<string>(type: "text", nullable: true),
                    ParentNodeId = table.Column<string>(type: "text", nullable: true),
                    ParentNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    ParentNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TypeDefinitionNodeId = table.Column<string>(type: "text", nullable: true),
                    TypeDefinitionNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    TypeDefinitionNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Objects", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_Objects_Nodes_NodeId_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Objects_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNode~",
                        columns: x => new { x.ParentNodeId, x.ParentNodeSetModelUri, x.ParentNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_Objects_NodeSets_NodeSetModelModelUri_NodeSetModelPublicati~",
                        columns: x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_Objects_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Objects_ObjectTypes_TypeDefinitionNodeId_TypeDefinitionNode~",
                        columns: x => new { x.TypeDefinitionNodeId, x.TypeDefinitionNodeSetModelUri, x.TypeDefinitionNodeSetPublicationDate },
                        principalTable: "ObjectTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                });

            migrationBuilder.CreateTable(
                name: "Variables",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataTypeNodeId = table.Column<string>(type: "text", nullable: true),
                    DataTypeNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    DataTypeNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValueRank = table.Column<int>(type: "integer", nullable: true),
                    ArrayDimensions = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true),
                    EngineeringUnit_DisplayName_Text = table.Column<string>(type: "text", nullable: true),
                    EngineeringUnit_DisplayName_Locale = table.Column<string>(type: "text", nullable: true),
                    EngineeringUnit_Description_Text = table.Column<string>(type: "text", nullable: true),
                    EngineeringUnit_Description_Locale = table.Column<string>(type: "text", nullable: true),
                    EngineeringUnit_NamespaceUri = table.Column<string>(type: "text", nullable: true),
                    EngineeringUnit_UnitId = table.Column<int>(type: "integer", nullable: true),
                    EngUnitNodeId = table.Column<string>(type: "text", nullable: true),
                    MinValue = table.Column<double>(type: "double precision", nullable: true),
                    MaxValue = table.Column<double>(type: "double precision", nullable: true),
                    InstrumentMinValue = table.Column<double>(type: "double precision", nullable: true),
                    InstrumentMaxValue = table.Column<double>(type: "double precision", nullable: true),
                    EnumValue = table.Column<long>(type: "bigint", nullable: true),
                    AccessLevel = table.Column<long>(type: "bigint", nullable: true),
                    UserAccessLevel = table.Column<long>(type: "bigint", nullable: true),
                    AccessRestrictions = table.Column<int>(type: "integer", nullable: true),
                    WriteMask = table.Column<long>(type: "bigint", nullable: true),
                    UserWriteMask = table.Column<long>(type: "bigint", nullable: true),
                    ModelingRule = table.Column<string>(type: "text", nullable: true),
                    ParentNodeId = table.Column<string>(type: "text", nullable: true),
                    ParentNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    ParentNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TypeDefinitionNodeId = table.Column<string>(type: "text", nullable: true),
                    TypeDefinitionNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    TypeDefinitionNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Variables", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_Variables_BaseTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                        columns: x => new { x.DataTypeNodeId, x.DataTypeNodeSetModelUri, x.DataTypeNodeSetPublicationDate },
                        principalTable: "BaseTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_Variables_Nodes_NodeId_NodeSetModelUri_NodeSetPublicationDa~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Variables_Nodes_ParentNodeId_ParentNodeSetModelUri_ParentNo~",
                        columns: x => new { x.ParentNodeId, x.ParentNodeSetModelUri, x.ParentNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_Variables_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Variables_VariableTypes_TypeDefinitionNodeId_TypeDefinition~",
                        columns: x => new { x.TypeDefinitionNodeId, x.TypeDefinitionNodeSetModelUri, x.TypeDefinitionNodeSetPublicationDate },
                        principalTable: "VariableTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                });

            migrationBuilder.CreateTable(
                name: "StructureField_Description",
                columns: table => new {
                    StructureFieldDataTypeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    StructureFieldDataTypeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    StructureFieldDataTypeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StructureFieldId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_StructureField_Description", x => new { x.StructureFieldDataTypeModelNodeId, x.StructureFieldDataTypeModelNodeSetModelUri, x.StructureFieldDataTypeModelNodeSetPublicationDate, x.StructureFieldId, x.Id });
                    table.ForeignKey(
                        name: "FK_StructureField_Description_StructureField_StructureFieldDat~",
                        columns: x => new { x.StructureFieldDataTypeModelNodeId, x.StructureFieldDataTypeModelNodeSetModelUri, x.StructureFieldDataTypeModelNodeSetPublicationDate, x.StructureFieldId },
                        principalTable: "StructureField",
                        principalColumns: new[] { "DataTypeModelNodeId", "DataTypeModelNodeSetModelUri", "DataTypeModelNodeSetPublicationDate", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UaEnumField_Description",
                columns: table => new {
                    UaEnumFieldDataTypeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    UaEnumFieldDataTypeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    UaEnumFieldDataTypeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UaEnumFieldId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UaEnumField_Description", x => new { x.UaEnumFieldDataTypeModelNodeId, x.UaEnumFieldDataTypeModelNodeSetModelUri, x.UaEnumFieldDataTypeModelNodeSetPublicationDate, x.UaEnumFieldId, x.Id });
                    table.ForeignKey(
                        name: "FK_UaEnumField_Description_UaEnumField_UaEnumFieldDataTypeMode~",
                        columns: x => new { x.UaEnumFieldDataTypeModelNodeId, x.UaEnumFieldDataTypeModelNodeSetModelUri, x.UaEnumFieldDataTypeModelNodeSetPublicationDate, x.UaEnumFieldId },
                        principalTable: "UaEnumField",
                        principalColumns: new[] { "DataTypeModelNodeId", "DataTypeModelNodeSetModelUri", "DataTypeModelNodeSetPublicationDate", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UaEnumField_DisplayName",
                columns: table => new {
                    UaEnumFieldDataTypeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    UaEnumFieldDataTypeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    UaEnumFieldDataTypeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UaEnumFieldId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_UaEnumField_DisplayName", x => new { x.UaEnumFieldDataTypeModelNodeId, x.UaEnumFieldDataTypeModelNodeSetModelUri, x.UaEnumFieldDataTypeModelNodeSetPublicationDate, x.UaEnumFieldId, x.Id });
                    table.ForeignKey(
                        name: "FK_UaEnumField_DisplayName_UaEnumField_UaEnumFieldDataTypeMode~",
                        columns: x => new { x.UaEnumFieldDataTypeModelNodeId, x.UaEnumFieldDataTypeModelNodeSetModelUri, x.UaEnumFieldDataTypeModelNodeSetPublicationDate, x.UaEnumFieldId },
                        principalTable: "UaEnumField",
                        principalColumns: new[] { "DataTypeModelNodeId", "DataTypeModelNodeSetModelUri", "DataTypeModelNodeSetPublicationDate", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataVariables",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodeModelNodeId = table.Column<string>(type: "text", nullable: true),
                    NodeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NodeSetModelModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeSetModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DataVariables", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_DataVariables_Nodes_NodeModelNodeId_NodeModelNodeSetModelUr~",
                        columns: x => new { x.NodeModelNodeId, x.NodeModelNodeSetModelUri, x.NodeModelNodeSetPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" });
                    table.ForeignKey(
                        name: "FK_DataVariables_NodeSets_NodeSetModelModelUri_NodeSetModelPub~",
                        columns: x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_DataVariables_NodeSets_NodeSetModelUri_NodeSetPublicationDa~",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataVariables_Variables_NodeId_NodeSetModelUri_NodeSetPubli~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "Variables",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new {
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    NodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    NodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NodeSetModelModelUri = table.Column<string>(type: "text", nullable: true),
                    NodeSetModelPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Properties", x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate });
                    table.ForeignKey(
                        name: "FK_Properties_NodeSets_NodeSetModelModelUri_NodeSetModelPublic~",
                        columns: x => new { x.NodeSetModelModelUri, x.NodeSetModelPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" });
                    table.ForeignKey(
                        name: "FK_Properties_NodeSets_NodeSetModelUri_NodeSetPublicationDate",
                        columns: x => new { x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "NodeSets",
                        principalColumns: new[] { "ModelUri", "PublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Properties_Variables_NodeId_NodeSetModelUri_NodeSetPublicat~",
                        columns: x => new { x.NodeId, x.NodeSetModelUri, x.NodeSetPublicationDate },
                        principalTable: "Variables",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseTypes_NodeSetModelUri_NodeSetPublicationDate",
                table: "BaseTypes",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUri_SuperTyp~",
                table: "BaseTypes",
                columns: new[] { "SuperTypeNodeId", "SuperTypeNodeSetModelUri", "SuperTypeNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildAndReference_ChildNodeId_ChildNodeSetModelUri_ChildNod~",
                table: "ChildAndReference",
                columns: new[] { "ChildNodeId", "ChildNodeSetModelUri", "ChildNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataTypes_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "DataTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataTypes_NodeSetModelUri_NodeSetPublicationDate",
                table: "DataTypes",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataVariables_NodeModelNodeId_NodeModelNodeSetModelUri_Node~",
                table: "DataVariables",
                columns: new[] { "NodeModelNodeId", "NodeModelNodeSetModelUri", "NodeModelNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataVariables_NodeSetModelModelUri_NodeSetModelPublicationD~",
                table: "DataVariables",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataVariables_NodeSetModelUri_NodeSetPublicationDate",
                table: "DataVariables",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Interfaces_NodeModelNodeId1_NodeModelNodeSetModelUri1_NodeM~",
                table: "Interfaces",
                columns: new[] { "NodeModelNodeId1", "NodeModelNodeSetModelUri1", "NodeModelNodeSetPublicationDate1" });

            migrationBuilder.CreateIndex(
                name: "IX_Interfaces_NodeSetModelModelUri1_NodeSetModelPublicationDat~",
                table: "Interfaces",
                columns: new[] { "NodeSetModelModelUri1", "NodeSetModelPublicationDate1" });

            migrationBuilder.CreateIndex(
                name: "IX_Interfaces_NodeSetModelUri_NodeSetPublicationDate",
                table: "Interfaces",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Methods_NodeSetModelUri_NodeSetPublicationDate",
                table: "Methods",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Methods_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetPub~",
                table: "Methods",
                columns: new[] { "ParentNodeId", "ParentNodeSetModelUri", "ParentNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUri_~",
                table: "Methods",
                columns: new[] { "TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_NodeSetModelUri_NodeSetPublicationDate",
                table: "Nodes",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Objects_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "Objects",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Objects_NodeSetModelUri_NodeSetPublicationDate",
                table: "Objects",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Objects_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetPub~",
                table: "Objects",
                columns: new[] { "ParentNodeId", "ParentNodeSetModelUri", "ParentNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Objects_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUri_~",
                table: "Objects",
                columns: new[] { "TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTypes_NodeModelNodeId_NodeModelNodeSetModelUri_NodeMo~",
                table: "ObjectTypes",
                columns: new[] { "NodeModelNodeId", "NodeModelNodeSetModelUri", "NodeModelNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTypes_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "ObjectTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTypes_NodeSetModelUri_NodeSetPublicationDate",
                table: "ObjectTypes",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_NodeSetModelModelUri_NodeSetModelPublicationDate",
                table: "Properties",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_NodeSetModelUri_NodeSetPublicationDate",
                table: "Properties",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTypes_NodeSetModelModelUri_NodeSetModelPublication~",
                table: "ReferenceTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTypes_NodeSetModelUri_NodeSetPublicationDate",
                table: "ReferenceTypes",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RequiredModelInfo_AvailableModelModelUri_AvailableModelPubl~",
                table: "RequiredModelInfo",
                columns: new[] { "AvailableModelModelUri", "AvailableModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StructureField_DataTypeNodeId_DataTypeNodeSetModelUri_DataT~",
                table: "StructureField",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Variables_DataTypeNodeId_DataTypeNodeSetModelUri_DataTypeNo~",
                table: "Variables",
                columns: new[] { "DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Variables_NodeSetModelUri_NodeSetPublicationDate",
                table: "Variables",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Variables_ParentNodeId_ParentNodeSetModelUri_ParentNodeSetP~",
                table: "Variables",
                columns: new[] { "ParentNodeId", "ParentNodeSetModelUri", "ParentNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Variables_TypeDefinitionNodeId_TypeDefinitionNodeSetModelUr~",
                table: "Variables",
                columns: new[] { "TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableTypes_NodeSetModelModelUri_NodeSetModelPublicationD~",
                table: "VariableTypes",
                columns: new[] { "NodeSetModelModelUri", "NodeSetModelPublicationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VariableTypes_NodeSetModelUri_NodeSetPublicationDate",
                table: "VariableTypes",
                columns: new[] { "NodeSetModelUri", "NodeSetPublicationDate" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildAndReference");

            migrationBuilder.DropTable(
                name: "DataVariables");

            migrationBuilder.DropTable(
                name: "Interfaces");

            migrationBuilder.DropTable(
                name: "Methods");

            migrationBuilder.DropTable(
                name: "Nodes_Description");

            migrationBuilder.DropTable(
                name: "Nodes_DisplayName");

            migrationBuilder.DropTable(
                name: "Objects");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "ReferenceTypes");

            migrationBuilder.DropTable(
                name: "RequiredModelInfo");

            migrationBuilder.DropTable(
                name: "StructureField_Description");

            migrationBuilder.DropTable(
                name: "UaEnumField_Description");

            migrationBuilder.DropTable(
                name: "UaEnumField_DisplayName");

            migrationBuilder.DropTable(
                name: "ObjectTypes");

            migrationBuilder.DropTable(
                name: "Variables");

            migrationBuilder.DropTable(
                name: "StructureField");

            migrationBuilder.DropTable(
                name: "UaEnumField");

            migrationBuilder.DropTable(
                name: "VariableTypes");

            migrationBuilder.DropTable(
                name: "DataTypes");

            migrationBuilder.DropTable(
                name: "BaseTypes");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "NodeSets");
        }
    }
}
