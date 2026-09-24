using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class AddDppAuditCheckpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Single-row table holding the expected tail of the audit chain. Without it, deleting the
            // most recent entries leaves a still-valid prefix and truncation goes undetected.
            migrationBuilder.CreateTable(
                name: "DppAuditCheckpoints",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    EntryCount = table.Column<long>(type: "bigint", nullable: false),
                    TailHash = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DppAuditCheckpoints", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DppAuditCheckpoints");
        }
    }
}
