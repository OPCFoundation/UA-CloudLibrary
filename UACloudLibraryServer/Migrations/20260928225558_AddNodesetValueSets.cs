using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class AddNodesetValueSets : Migration
    {
        private static readonly string[] s_valueSetNameIndexColumns = new[] { "NodesetIdentifier", "UserId", "Name" };
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The scaffolder also emitted AddColumn NamespaceMeta.IsPublished because that column
            // had never been in the model snapshot. It is deliberately omitted: Startup applies it
            // separately via Migrations/Scripts/AddIsPublishedToNamespaceMeta.sql (ADD COLUMN IF NOT
            // EXISTS), which runs right after MigrateAsync, so this migration works on databases
            // with or without the column. The snapshot now includes it so it is not re-offered.
            migrationBuilder.CreateTable(
                name: "NodesetValueSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodesetIdentifier = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodesetValueSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NodesetValueSetEntries",
                columns: table => new
                {
                    ValueSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodesetValueSetEntries", x => new { x.ValueSetId, x.NodeId });
                    table.ForeignKey(
                        name: "FK_NodesetValueSetEntries_NodesetValueSets_ValueSetId",
                        column: x => x.ValueSetId,
                        principalTable: "NodesetValueSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NodesetValueSets_NodesetIdentifier_UserId_Name",
                table: "NodesetValueSets",
                columns: s_valueSetNameIndexColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodesetValueSetEntries");

            migrationBuilder.DropTable(
                name: "NodesetValueSets");
        }
    }
}
