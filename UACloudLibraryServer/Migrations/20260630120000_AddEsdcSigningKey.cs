using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class AddEsdcSigningKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Single-row table holding the server-generated ESDC signing key. Persisting it keeps the
            // key stable across restarts and shared between instances; without it, each process signs
            // with its own throwaway key and previously issued ESDCs stop verifying.
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
                name: "EsdcSigningKeys");
        }
    }
}
