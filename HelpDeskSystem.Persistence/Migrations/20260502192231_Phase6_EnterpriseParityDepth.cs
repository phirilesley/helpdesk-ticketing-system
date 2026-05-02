using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_EnterpriseParityDepth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuardrailJson",
                table: "WorkflowDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxLoopCount",
                table: "WorkflowDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "WebhookSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RetryBackoffSeconds",
                table: "WebhookSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimeoutSeconds",
                table: "WebhookSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EntitlementOverridesJson",
                table: "TenantSubscriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Burndown",
                table: "SprintMetrics",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Burnup",
                table: "SprintMetrics",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CycleTimeHours",
                table: "SprintMetrics",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Velocity",
                table: "SprintMetrics",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DependencyGraphJson",
                table: "ReleasePlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DedupWindowMinutes",
                table: "OmnichannelConnectors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProviderKey",
                table: "OmnichannelConnectors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignatureAlgorithm",
                table: "OmnichannelConnectors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignatureHeaderName",
                table: "OmnichannelConnectors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalTimestampUtc",
                table: "InboundChannelEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPayloadJson",
                table: "InboundChannelEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RawPayloadJson",
                table: "InboundChannelEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AllowedRedirectUrisJson",
                table: "IdentityProviderConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnforceStrictIssuer",
                table: "IdentityProviderConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OidcRequirePkce",
                table: "IdentityProviderConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SamlAcsUrl",
                table: "IdentityProviderConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SamlAllowIdpInitiated",
                table: "IdentityProviderConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SamlAllowedCertificateThumbprints",
                table: "IdentityProviderConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SamlMetadataXml",
                table: "IdentityProviderConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SamlSpEntityId",
                table: "IdentityProviderConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SamlValidateSignature",
                table: "IdentityProviderConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Stage",
                table: "DataSubjectRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DsrProcessingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DataSubjectRequestId = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsrProcessingLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TenantSubscriptionId = table.Column<int>(type: "int", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubtotalUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LineItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceApps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManifestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinPlanName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceApps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaBreachActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BreachType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TriggerAfterBreachMinutes = table.Column<int>(type: "int", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaBreachActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaPauseRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PauseResponseSla = table.Column<bool>(type: "bit", nullable: false),
                    PauseResolutionSla = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPauseRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAppInstalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    MarketplaceAppId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InstalledVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAppInstalls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DsrProcessingLogs_TenantId_DataSubjectRequestId_CreatedAtUtc",
                table: "DsrProcessingLogs",
                columns: new[] { "TenantId", "DataSubjectRequestId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_InvoiceNumber",
                table: "Invoices",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceApps_AppKey",
                table: "MarketplaceApps",
                column: "AppKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaBreachActions_TenantId_BreachType_ExecutionOrder",
                table: "SlaBreachActions",
                columns: new[] { "TenantId", "BreachType", "ExecutionOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaPauseRules_TenantId_IsEnabled",
                table: "SlaPauseRules",
                columns: new[] { "TenantId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAppInstalls_TenantId_MarketplaceAppId",
                table: "TenantAppInstalls",
                columns: new[] { "TenantId", "MarketplaceAppId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DsrProcessingLogs");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "MarketplaceApps");

            migrationBuilder.DropTable(
                name: "SlaBreachActions");

            migrationBuilder.DropTable(
                name: "SlaPauseRules");

            migrationBuilder.DropTable(
                name: "TenantAppInstalls");

            migrationBuilder.DropColumn(
                name: "GuardrailJson",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxLoopCount",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "WebhookSubscriptions");

            migrationBuilder.DropColumn(
                name: "RetryBackoffSeconds",
                table: "WebhookSubscriptions");

            migrationBuilder.DropColumn(
                name: "TimeoutSeconds",
                table: "WebhookSubscriptions");

            migrationBuilder.DropColumn(
                name: "EntitlementOverridesJson",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "Burndown",
                table: "SprintMetrics");

            migrationBuilder.DropColumn(
                name: "Burnup",
                table: "SprintMetrics");

            migrationBuilder.DropColumn(
                name: "CycleTimeHours",
                table: "SprintMetrics");

            migrationBuilder.DropColumn(
                name: "Velocity",
                table: "SprintMetrics");

            migrationBuilder.DropColumn(
                name: "DependencyGraphJson",
                table: "ReleasePlans");

            migrationBuilder.DropColumn(
                name: "DedupWindowMinutes",
                table: "OmnichannelConnectors");

            migrationBuilder.DropColumn(
                name: "ProviderKey",
                table: "OmnichannelConnectors");

            migrationBuilder.DropColumn(
                name: "SignatureAlgorithm",
                table: "OmnichannelConnectors");

            migrationBuilder.DropColumn(
                name: "SignatureHeaderName",
                table: "OmnichannelConnectors");

            migrationBuilder.DropColumn(
                name: "ExternalTimestampUtc",
                table: "InboundChannelEvents");

            migrationBuilder.DropColumn(
                name: "NormalizedPayloadJson",
                table: "InboundChannelEvents");

            migrationBuilder.DropColumn(
                name: "RawPayloadJson",
                table: "InboundChannelEvents");

            migrationBuilder.DropColumn(
                name: "AllowedRedirectUrisJson",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "EnforceStrictIssuer",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "OidcRequirePkce",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "SamlAcsUrl",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "SamlAllowIdpInitiated",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "SamlAllowedCertificateThumbprints",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "SamlMetadataXml",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "SamlSpEntityId",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "SamlValidateSignature",
                table: "IdentityProviderConfigs");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "DataSubjectRequests");
        }
    }
}
