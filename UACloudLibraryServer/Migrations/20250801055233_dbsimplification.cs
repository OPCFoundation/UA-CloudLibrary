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

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class DBSimplification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NamespaceMeta_Categories_CategoryId",
                table: "NamespaceMeta");

            migrationBuilder.DropForeignKey(
                name: "FK_NamespaceMeta_Organisations_ContributorId",
                table: "NamespaceMeta");

            migrationBuilder.DropTable(
                name: "AdditionalProperties");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Organisations");

            migrationBuilder.DropIndex(
                name: "IX_NamespaceMeta_CategoryId",
                table: "NamespaceMeta");

            migrationBuilder.DropIndex(
                name: "IX_NamespaceMeta_ContributorId",
                table: "NamespaceMeta");

            migrationBuilder.DropColumn(
                name: "Categories",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "NamespaceMeta");

            migrationBuilder.DropColumn(
                name: "ContributorId",
                table: "NamespaceMeta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Categories",
                table: "Nodes",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "NamespaceMeta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContributorId",
                table: "NamespaceMeta",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Organisations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMeta_CategoryId",
                table: "NamespaceMeta",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMeta_ContributorId",
                table: "NamespaceMeta",
                column: "ContributorId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_Name",
                table: "Organisations",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NamespaceMeta_Categories_CategoryId",
                table: "NamespaceMeta",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NamespaceMeta_Organisations_ContributorId",
                table: "NamespaceMeta",
                column: "ContributorId",
                principalTable: "Organisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
