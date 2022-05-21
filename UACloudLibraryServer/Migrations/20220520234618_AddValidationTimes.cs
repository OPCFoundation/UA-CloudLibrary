using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opc.Ua.Cloud.Library
{
    public partial class AddValidationTimes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationElapsedTime",
                table: "NodeSets");

            migrationBuilder.DropColumn(
                name: "ValidationErrors",
                table: "NodeSets");

            migrationBuilder.DropColumn(
                name: "ValidationFinishedTime",
                table: "NodeSets");
        }
    }
}
