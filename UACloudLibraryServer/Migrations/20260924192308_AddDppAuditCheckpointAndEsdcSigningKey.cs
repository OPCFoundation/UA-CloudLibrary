using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class AddDppAuditCheckpointAndEsdcSigningKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NamespaceMeta.IsPublished is deliberately omitted here: it is applied separately by
            // Startup via Migrations/Scripts/AddIsPublishedToNamespaceMeta.sql and is not tracked in
            // the model snapshot, so scaffolding repeatedly offers to add it. Including it would
            // conflict with the column that script has already created.
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

            migrationBuilder.CreateTable(
                name: "EsdcSigningKeys",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PrivateKeyPem = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_EsdcSigningKeys", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DppAuditCheckpoints");

            migrationBuilder.DropTable(
                name: "EsdcSigningKeys");
        }
    }
}
