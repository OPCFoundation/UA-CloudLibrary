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
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"],
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Nodes_OtherReferencingNodes_Nodes_ReferencingNodeId_Referen~",
                        columns: x => new { x.ReferencingNodeId, x.ReferencingModelUri, x.ReferencingPublicationDate },
                        principalTable: "Nodes",
                        principalColumns: ["NodeId", "NodeSetModelUri", "NodeSetPublicationDate"]);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_OtherReferencingNodes_ReferencingNodeId_ReferencingMo~",
                table: "Nodes_OtherReferencingNodes",
                columns: ["ReferencingNodeId", "ReferencingModelUri", "ReferencingPublicationDate"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nodes_OtherReferencingNodes");
        }
    }
}
