using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class RemoveUANodeTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "datatype");

            migrationBuilder.DropTable(
                name: "objecttype");

            migrationBuilder.DropTable(
                name: "referencetype");

            migrationBuilder.DropTable(
                name: "variabletype");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "datatype",
                columns: table => new {
                    datatype_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    datatype_browsename = table.Column<string>(type: "text", nullable: true),
                    datatype_namespace = table.Column<string>(type: "text", nullable: true),
                    nodeset_id = table.Column<long>(type: "bigint", nullable: false),
                    datatype_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_datatype", x => x.datatype_id);
                });

            migrationBuilder.CreateTable(
                name: "objecttype",
                columns: table => new {
                    objecttype_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    objecttype_browsename = table.Column<string>(type: "text", nullable: true),
                    objecttype_namespace = table.Column<string>(type: "text", nullable: true),
                    nodeset_id = table.Column<long>(type: "bigint", nullable: false),
                    objecttype_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_objecttype", x => x.objecttype_id);
                });

            migrationBuilder.CreateTable(
                name: "referencetype",
                columns: table => new {
                    referencetype_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    referencetype_browsename = table.Column<string>(type: "text", nullable: true),
                    referencetype_namespace = table.Column<string>(type: "text", nullable: true),
                    nodeset_id = table.Column<long>(type: "bigint", nullable: false),
                    referencetype_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_referencetype", x => x.referencetype_id);
                });

            migrationBuilder.CreateTable(
                name: "variabletype",
                columns: table => new {
                    variabletype_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    variabletype_browsename = table.Column<string>(type: "text", nullable: true),
                    variabletype_namespace = table.Column<string>(type: "text", nullable: true),
                    nodeset_id = table.Column<long>(type: "bigint", nullable: false),
                    variabletype_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_variabletype", x => x.variabletype_id);
                });
        }
    }
}
