using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_OutboundDeliveryAndMultiRegionReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboundChannelMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ConnectorId = table.Column<int>(type: "int", nullable: false),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecipientAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PartitionKey = table.Column<int>(type: "int", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundChannelMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboundDeliveryReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OutboundChannelMessageId = table.Column<int>(type: "int", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundDeliveryReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegionSyntheticChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CheckType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionSyntheticChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantRegionPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PrimaryRegion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecondaryRegion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailoverMode = table.Column<int>(type: "int", nullable: false),
                    AutoFailbackEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RunbookUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonitoringConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRegionPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundChannelMessages_ConnectorId_IdempotencyKey",
                table: "OutboundChannelMessages",
                columns: new[] { "ConnectorId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundChannelMessages_Status_NextAttemptAtUtc_PartitionKey",
                table: "OutboundChannelMessages",
                columns: new[] { "Status", "NextAttemptAtUtc", "PartitionKey" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundDeliveryReceipts_OutboundChannelMessageId_ProviderMessageId_Status",
                table: "OutboundDeliveryReceipts",
                columns: new[] { "OutboundChannelMessageId", "ProviderMessageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionSyntheticChecks_TenantId_Region_CheckedAtUtc",
                table: "RegionSyntheticChecks",
                columns: new[] { "TenantId", "Region", "CheckedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRegionPolicies_TenantId",
                table: "TenantRegionPolicies",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboundChannelMessages");

            migrationBuilder.DropTable(
                name: "OutboundDeliveryReceipts");

            migrationBuilder.DropTable(
                name: "RegionSyntheticChecks");

            migrationBuilder.DropTable(
                name: "TenantRegionPolicies");
        }
    }
}
