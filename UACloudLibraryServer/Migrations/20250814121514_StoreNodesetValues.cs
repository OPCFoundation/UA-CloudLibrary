using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class StoreNodesetValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Values",
                table: "DbFiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Values",
                table: "DbFiles");
        }
    }
}
