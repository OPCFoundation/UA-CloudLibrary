using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class ReferenceTypeUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceTypes_Nodes_NodeId_NodeSetModelUri_NodeSetPublicat~",
                table: "ReferenceTypes");

            migrationBuilder.AddColumn<bool>(
                name: "Symmetric",
                table: "ReferenceTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ReferenceTypes_InverseName",
                columns: table => new {
                    ReferenceTypeModelNodeId = table.Column<string>(type: "text", nullable: false),
                    ReferenceTypeModelNodeSetModelUri = table.Column<string>(type: "text", nullable: false),
                    ReferenceTypeModelNodeSetPublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_ReferenceTypes_InverseName", x => new { x.ReferenceTypeModelNodeId, x.ReferenceTypeModelNodeSetModelUri, x.ReferenceTypeModelNodeSetPublicationDate, x.Id });
                    table.ForeignKey(
                        name: "FK_ReferenceTypes_InverseName_ReferenceTypes_ReferenceTypeMode~",
                        columns: x => new { x.ReferenceTypeModelNodeId, x.ReferenceTypeModelNodeSetModelUri, x.ReferenceTypeModelNodeSetPublicationDate },
                        principalTable: "ReferenceTypes",
                        principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTypes_BaseTypes_NodeId_NodeSetModelUri_NodeSetPubl~",
                table: "ReferenceTypes",
                columns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                principalTable: "BaseTypes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceTypes_BaseTypes_NodeId_NodeSetModelUri_NodeSetPubl~",
                table: "ReferenceTypes");

            migrationBuilder.DropTable(
                name: "ReferenceTypes_InverseName");

            migrationBuilder.DropColumn(
                name: "Symmetric",
                table: "ReferenceTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceTypes_Nodes_NodeId_NodeSetModelUri_NodeSetPublicat~",
                table: "ReferenceTypes",
                columns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                principalTable: "Nodes",
                principalColumns: new[] { "NodeId", "NodeSetModelUri", "NodeSetPublicationDate" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
