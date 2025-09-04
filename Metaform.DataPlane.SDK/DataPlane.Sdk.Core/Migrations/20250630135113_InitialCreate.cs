using System;
using DataPlane.Sdk.Core.Domain;
using DataPlane.Sdk.Core.Domain.Model;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataPlane.Sdk.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataFlows",
                columns: table => new {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<DataAddress>(type: "jsonb", nullable: false),
                    Destination = table.Column<DataAddress>(type: "jsonb", nullable: false),
                    CallbackAddress = table.Column<string>(type: "text", nullable: true),
                    TransferType = table.Column<TransferType>(type: "jsonb", nullable: false),
                    RuntimeId = table.Column<string>(type: "text", nullable: false),
                    IsProvisionComplete = table.Column<bool>(type: "boolean", nullable: false),
                    IsProvisionRequested = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeprovisionComplete = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeprovisionRequested = table.Column<bool>(type: "boolean", nullable: false),
                    IsConsumer = table.Column<bool>(type: "boolean", nullable: false),
                    ParticipantId = table.Column<string>(type: "text", nullable: false),
                    AssetId = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    AgreementId = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_DataFlows", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataFlows");
        }
    }
}
