using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class AddDppAuditKeyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeyId",
                table: "DppAuditEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyId",
                table: "DppAuditCheckpoints",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeyId",
                table: "DppAuditEntries");

            migrationBuilder.DropColumn(
                name: "KeyId",
                table: "DppAuditCheckpoints");
        }
    }
}
