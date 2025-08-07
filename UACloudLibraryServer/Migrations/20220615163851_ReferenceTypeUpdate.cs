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
    public partial class ReferenceTypeUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceTypes_Nodes_NodeId_NodeSetModelUri_NodeSetPublicat~",
                table: "ReferenceTypes");

            migrationBuilder.AddColumn<bool>(
                name: "Symmetric",
                table: "ReferenceTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ReferenceTypes_InverseName",
                columns: table => new {
                    ReferenceTypeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    ReferenceTypeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    ReferenceTypeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_ReferenceTypes_InverseName", x => new { x.ReferenceTypeModelNodeId, x.ReferenceTypeModelNodeSetModelUri, x.ReferenceTypeModelNodeSetPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_ReferenceTypes_InverseName_ReferenceTypes_ReferenceTypeMode~",
                        columns: x => new { x.ReferenceTypeModelNodeId, x.ReferenceTypeModelNodeSetModelUri, x.ReferenceTypeModelNodeSetPublicationDate },
                        principalTable: "ReferenceTypes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTypes_BaseTypes_NodeId_NodeSetModelUri_NodeSetPubl~",
                table: "ReferenceTypes",
                columns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                principalTable: "BaseTypes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceTypes_BaseTypes_NodeId_NodeSetModelUri_NodeSetPubl~",
                table: "ReferenceTypes");

            migrationBuilder.DropTable(
                name: "ReferenceTypes_InverseName");

            migrationBuilder.DropColumn(
                name: "Symmetric",
                table: "ReferenceTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTypes_Nodes_NodeId_NodeSetModelUri_NodeSetPublicat~",
                table: "ReferenceTypes",
                columns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                principalTable: "Nodes",
                principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                onDelete: ReferentialAction.Cascade);
        }
    }
}
