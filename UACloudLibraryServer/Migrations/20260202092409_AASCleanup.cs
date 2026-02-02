using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    /// <inheritdoc />
    public partial class AASCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Key");

            migrationBuilder.DropTable(
                name: "PersistSpecificAssetIds");

            migrationBuilder.DropTable(
                name: "SpecificAssetId");

            migrationBuilder.DropTable(
                name: "Reference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reference",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Key",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_Key", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Key_Reference_ReferenceId",
                        column: x => x.ReferenceId,
                        principalTable: "Reference",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SpecificAssetId",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalSubjectIdId = table.Column<int>(type: "integer", nullable: true),
                    SemanticIdId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_SpecificAssetId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecificAssetId_Reference_ExternalSubjectIdId",
                        column: x => x.ExternalSubjectIdId,
                        principalTable: "Reference",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SpecificAssetId_Reference_SemanticIdId",
                        column: x => x.SemanticIdId,
                        principalTable: "Reference",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PersistSpecificAssetIds",
                columns: table => new {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetIdId = table.Column<int>(type: "integer", nullable: true),
                    AASId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => {
                    table.PrimaryKey("PK_PersistSpecificAssetIds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersistSpecificAssetIds_SpecificAssetId_AssetIdId",
                        column: x => x.AssetIdId,
                        principalTable: "SpecificAssetId",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Key_ReferenceId",
                table: "Key",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_PersistSpecificAssetIds_AssetIdId",
                table: "PersistSpecificAssetIds",
                column: "AssetIdId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificAssetId_ExternalSubjectIdId",
                table: "SpecificAssetId",
                column: "ExternalSubjectIdId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificAssetId_SemanticIdId",
                table: "SpecificAssetId",
                column: "SemanticIdId");
        }
    }
}
