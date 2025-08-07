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

namespace Opc.Ua.Cloud.Library
{
    public partial class RefencetypeSchemauri : Migration
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
                columns: ["ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencedNodes_ReferenceTypeNodeId_ReferenceTyp~",
                table: "Nodes_OtherReferencedNodes",
                columns: ["ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate"]);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_OtherReferencedNodes_Nodes_ReferenceTypeNodeId_Refere~",
                table: "Nodes_OtherReferencedNodes",
                columns: ["ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferenceTypeNodeId_Refer~",
                table: "Nodes_OtherReferencingNodes",
                columns: ["ReferenceTypeNodeId", "ReferenceTypeModelUri", "ReferenceTypePublicationDate"],
                principalTable: "Nodes",
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
                name: "FK_VariableTypes_DataTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "DataTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
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
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableTypes_BaseTypes_DataTypeNodeId_DataTypeNodeSetModel~",
                table: "VariableTypes",
                columns: ["DataTypeNodeId", "DataTypeNodeSetModelUri", "DataTypeNodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);
        }
    }
}
