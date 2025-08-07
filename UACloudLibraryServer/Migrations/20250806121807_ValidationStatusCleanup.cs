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

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class ValidationStatusCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "NodeSets");

            migrationBuilder.DropColumn(
                name: "ValidationElapsedTime",
                table: "NodeSets");

            migrationBuilder.DropColumn(
                name: "ValidationErrors",
                table: "NodeSets");

            migrationBuilder.DropColumn(
                name: "ValidationFinishedTime",
                table: "NodeSets");

            migrationBuilder.DropColumn(
                name: "ValidationStatus",
                table: "NodeSets");

            migrationBuilder.DropColumn(
                name: "ValidationStatusInfo",
                table: "NodeSets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "NodeSets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ValidationElapsedTime",
                table: "NodeSets",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "ValidationErrors",
                table: "NodeSets",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidationFinishedTime",
                table: "NodeSets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationStatus",
                table: "NodeSets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationStatusInfo",
                table: "NodeSets",
                type: "text",
                nullable: true);
        }
    }
}
