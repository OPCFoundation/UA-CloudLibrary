/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Opc.Ua.Cloud.Library
{
    public partial class MetadataCleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseTypes_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUr~",
                table: "BaseTypes");

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
                name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferencedNodeId_Reference~",
                table: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferencingNodeId_Referen~",
                table: "Nodes_OtherReferencingNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_NodeSets_NodeSetObjectsModelUri_NodeSetObjectsPubli~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_ObjectTypes_TypeDefinitionNodeId_TypeDefinitionNode~",
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
                name: "FK_RequiredModelInfo_NodeSets_AvailableModelModelUri_Available~",
                table: "RequiredModelInfoModel");

            migrationBuilder.DropForeignKey(
                name: "FK_StructureField_BaseTypes_DataTypeNodeId_DataTypeNodeSetMode~",
                table: "StructureField");

            migrationBuilder.DropForeignKey(
                name: "FK_Variables_BaseTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_Variables_VariableTypes_TypeDefinitionNodeId_TypeDefinition~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_NodeSets_NodeSetVariableTypesModelUri_NodeSet~",
                table: "VariableTypes");

            migrationBuilder.AlterColumn<string>(
                name: "Identifier",
                table: "NodeSets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "NodeSets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_NodeSets_Identifier",
                table: "NodeSets",
                column: "Identifier");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IconUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbFiles",
                columns: table => new {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Blob = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DbFiles", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Organisations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceMeta",
                columns: table => new {
                    NodesetId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    ContributorId = table.Column<int>(type: "integer", nullable: false),
                    License = table.Column<string>(type: "text", nullable: true),
                    CopyrightText = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    DocumentationUrl = table.Column<string>(type: "text", nullable: true),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    LicenseUrl = table.Column<string>(type: "text", nullable: true),
                    Keywords = table.Column<string[]>(type: "text[]", nullable: true),
                    PurchasingInformationUrl = table.Column<string>(type: "text", nullable: true),
                    ReleaseNotesUrl = table.Column<string>(type: "text", nullable: true),
                    TestSpecificationUrl = table.Column<string>(type: "text", nullable: true),
                    SupportedLocales = table.Column<string[]>(type: "text[]", nullable: true),
                    NumberOfDownloads = table.Column<long>(type: "bigint", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "text", nullable: true),
                    ApprovalInformation = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_NamespaceMeta", x => x.NodesetId);
                    table.ForeignKey(
                        name: "FK_NamespaceMeta_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceMeta_Organisations_ContributorId",
                        column: x => x.ContributorId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalProperties",
                columns: table => new {
                    NodeSetId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_AdditionalProperties", x => new { x.NodeSetId, x.Id });
                    table.ForeignKey(
                        name: "FK_AdditionalProperties_NamespaceMeta_NodeSetId",
                        column: x => x.NodeSetId,
                        principalTable: "NamespaceMeta",
                        principalColumn: "NodesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_BrowseName",
                table: "Nodes",
                column: "BrowseName")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMeta_CategoryId",
                table: "NamespaceMeta",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMeta_ContributorId",
                table: "NamespaceMeta",
                column: "ContributorId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMeta_Title_Description",
                table: "NamespaceMeta",
                columns: ["Title", "Description"])
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_Name",
                table: "Organisations",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseTypes_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUr~",
                table: "BaseTypes",
                columns: ["SuperTypeNodeId", "SuperTypeNodeSetModelUri", "SuperTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataTypes_NodeSets_NodeSetDataTypesModelUri_NodeSetDataType~",
                table: "DataTypes",
                columns: ["NodeSetDataTypesModelUri", "NodeSetDataTypesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataVariables_Nodes_ParentNodeId_ParentModelUri_ParentPubli~",
                table: "DataVariables",
                columns: ["ParentNodeId", "ParentModelUri", "ParentPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DataVariables_NodeSets_NodeSetDataVariablesModelUri_NodeSet~",
                table: "DataVariables",
                columns: ["NodeSetDataVariablesModelUri", "NodeSetDataVariablesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Interfaces_NodeSets_NodeSetInterfacesModelUri_NodeSetInterf~",
                table: "Interfaces",
                columns: ["NodeSetInterfacesModelUri", "NodeSetInterfacesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetM~",
                table: "Methods",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"],
                principalTable: "Methods",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Methods",
                columns: ["ParentNodeId", "ParentModelUri", "ParentPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_NodeSets_NodeSetUnknownNodesModelUri_NodeSetUnknownNo~",
                table: "Nodes",
                columns: ["NodeSetUnknownNodesModelUri", "NodeSetUnknownNodesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferencedNodeId_Reference~",
                table: "Nodes_OtherReferencedNodes",
                columns: ["ReferencedNodeId", "ReferencedModelUri", "ReferencedPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferencingNodeId_Referen~",
                table: "Nodes_OtherReferencingNodes",
                columns: ["ReferencingNodeId", "ReferencingModelUri", "ReferencingPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            // Don't enforce in the database as we want NamespaceMeta to be able to exist without a Nodeset and vice versa
            //migrationBuilder.AddForeignKey(
            //    name: "FK_NodeSets_NamespaceMeta_Identifier",
            //    table: "NodeSets",
            //    column: "Identifier",
            //    principalTable: "NamespaceMeta",
            //    principalColumn: "NodesetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Objects",
                columns: ["ParentNodeId", "ParentModelUri", "ParentPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_NodeSets_NodeSetObjectsModelUri_NodeSetObjectsPubli~",
                table: "Objects",
                columns: ["NodeSetObjectsModelUri", "NodeSetObjectsPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_ObjectTypes_TypeDefinitionNodeId_TypeDefinitionNode~",
                table: "Objects",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"],
                principalTable: "ObjectTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectTypes_NodeSets_NodeSetObjectTypesModelUri_NodeSetObje~",
                table: "ObjectTypes",
                columns: ["NodeSetObjectTypesModelUri", "NodeSetObjectTypesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Nodes_ParentNodeId_ParentModelUri_ParentPublicat~",
                table: "Properties",
                columns: ["ParentNodeId", "ParentModelUri", "ParentPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_NodeSets_NodeSetPropertiesModelUri_NodeSetProper~",
                table: "Properties",
                columns: ["NodeSetPropertiesModelUri", "NodeSetPropertiesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTypes_NodeSets_NodeSetReferenceTypesModelUri_NodeS~",
                table: "ReferenceTypes",
                columns: ["NodeSetReferenceTypesModelUri", "NodeSetReferenceTypesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_AvailableModelModelUri_Available~",
                table: "RequiredModelInfoModel",
                columns: ["AvailableModelModelUri", "AvailableModelPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StructureField_BaseTypes_DataTypeNodeId_DataTypeNodeSetMode~",
                table: "StructureField",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_BaseTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
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
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_NodeSets_NodeSetVariableTypesModelUri_NodeSet~",
                table: "VariableTypes",
                columns: ["NodeSetVariableTypesModelUri", "NodeSetVariableTypesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"],
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseTypes_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUr~",
                table: "BaseTypes");

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
                name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferencedNodeId_Reference~",
                table: "Nodes_OtherReferencedNodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferencingNodeId_Referen~",
                table: "Nodes_OtherReferencingNodes");

            // Don't enforce in the database as we want NamespaceMeta to be able to exist without a Nodeset and vice versa
            //migrationBuilder.DropForeignKey(
            //    name: "FK_NodeSets_NamespaceMeta_Identifier",
            //    table: "NodeSets");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_NodeSets_NodeSetObjectsModelUri_NodeSetObjectsPubli~",
                table: "Objects");

            migrationBuilder.DropForeignKey(
                name: "FK_Objects_ObjectTypes_TypeDefinitionNodeId_TypeDefinitionNode~",
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
                name: "FK_RequiredModelInfo_NodeSets_AvailableModelModelUri_Available~",
                table: "RequiredModelInfoModel");

            migrationBuilder.DropForeignKey(
                name: "FK_StructureField_BaseTypes_DataTypeNodeId_DataTypeNodeSetMode~",
                table: "StructureField");

            migrationBuilder.DropForeignKey(
                name: "FK_Variables_BaseTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_Variables_VariableTypes_TypeDefinitionNodeId_TypeDefinition~",
                table: "Variables");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableTypes_NodeSets_NodeSetVariableTypesModelUri_NodeSet~",
                table: "VariableTypes");

            migrationBuilder.DropTable(
                name: "AdditionalProperties");

            migrationBuilder.DropTable(
                name: "DbFiles");

            migrationBuilder.DropTable(
                name: "NamespaceMeta");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Organisations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_NodeSets_Identifier",
                table: "NodeSets");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_BrowseName",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "NodeSets");

            migrationBuilder.AlterColumn<string>(
                name: "Identifier",
                table: "NodeSets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_BaseTypes_BaseTypes_SuperTypeNodeId_SuperTypeNodeSetModelUr~",
                table: "BaseTypes",
                columns: ["SuperTypeNodeId", "SuperTypeNodeSetModelUri", "SuperTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_DataTypes_NodeSets_NodeSetDataTypesModelUri_NodeSetDataType~",
                table: "DataTypes",
                columns: ["NodeSetDataTypesModelUri", "NodeSetDataTypesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_DataVariables_Nodes_ParentNodeId_ParentModelUri_ParentPubli~",
                table: "DataVariables",
                columns: ["ParentNodeId", "ParentModelUri", "ParentPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_DataVariables_NodeSets_NodeSetDataVariablesModelUri_NodeSet~",
                table: "DataVariables",
                columns: ["NodeSetDataVariablesModelUri", "NodeSetDataVariablesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Interfaces_NodeSets_NodeSetInterfacesModelUri_NodeSetInterf~",
                table: "Interfaces",
                columns: ["NodeSetInterfacesModelUri", "NodeSetInterfacesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Methods_TypeDefinitionNodeId_TypeDefinitionNodeSetM~",
                table: "Methods",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"],
                principalTable: "Methods",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Methods_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Methods",
                columns: ["ParentNodeId", "ParentModelUri", "ParentPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_NodeSets_NodeSetUnknownNodesModelUri_NodeSetUnknownNo~",
                table: "Nodes",
                columns: ["NodeSetUnknownNodesModelUri", "NodeSetUnknownNodesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferencedNodeId_Reference~",
                table: "Nodes_OtherReferencedNodes",
                columns: ["ReferencedNodeId", "ReferencedModelUri", "ReferencedPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferencingNodeId_Referen~",
                table: "Nodes_OtherReferencingNodes",
                columns: ["ReferencingNodeId", "ReferencingModelUri", "ReferencingPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_Nodes_ParentNodeId_ParentModelUri_ParentPublication~",
                table: "Objects",
                columns: ["ParentNodeId", "ParentModelUri", "ParentPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_NodeSets_NodeSetObjectsModelUri_NodeSetObjectsPubli~",
                table: "Objects",
                columns: ["NodeSetObjectsModelUri", "NodeSetObjectsPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Objects_ObjectTypes_TypeDefinitionNodeId_TypeDefinitionNode~",
                table: "Objects",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"],
                principalTable: "ObjectTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_ObjectTypes_NodeSets_NodeSetObjectTypesModelUri_NodeSetObje~",
                table: "ObjectTypes",
                columns: ["NodeSetObjectTypesModelUri", "NodeSetObjectTypesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Nodes_ParentNodeId_ParentModelUri_ParentPublicat~",
                table: "Properties",
                columns: ["ParentNodeId", "ParentModelUri", "ParentPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_NodeSets_NodeSetPropertiesModelUri_NodeSetProper~",
                table: "Properties",
                columns: ["NodeSetPropertiesModelUri", "NodeSetPropertiesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTypes_NodeSets_NodeSetReferenceTypesModelUri_NodeS~",
                table: "ReferenceTypes",
                columns: ["NodeSetReferenceTypesModelUri", "NodeSetReferenceTypesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredModelInfo_NodeSets_AvailableModelModelUri_Available~",
                table: "RequiredModelInfoModel",
                columns: ["AvailableModelModelUri", "AvailableModelPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_StructureField_BaseTypes_DataTypeNodeId_DataTypeNodeSetMode~",
                table: "StructureField",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_BaseTypes_DataTypeNodeId_DataTypeNodeSetModelUri_~",
                table: "Variables",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_VariableTypes_TypeDefinitionNodeId_TypeDefinition~",
                table: "Variables",
                columns: ["TypeDefinitionNodeId", "TypeDefinitionNodeSetModelUri", "TypeDefinitionNodeSetPublicationDate"],
                principalTable: "VariableTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_NodeSets_NodeSetVariableTypesModelUri_NodeSet~",
                table: "VariableTypes",
                columns: ["NodeSetVariableTypesModelUri", "NodeSetVariableTypesPublicationDate"],
                principalTable: "NodeSets",
                principalColumns: ["ModelUri", "PublicationDate"]);
        }
    }
}
