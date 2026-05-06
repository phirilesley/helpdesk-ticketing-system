using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCsatAndSourceChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CsatComment",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CsatRating",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CsatSubmittedAtUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceChannel",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CsatComment",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CsatRating",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CsatSubmittedAtUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SourceChannel",
                table: "Tickets");
        }
    }
}
