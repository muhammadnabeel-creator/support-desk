using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportDesk.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDraftReplyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftReply",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DraftReplyGeneratedAt",
                table: "Tickets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftReply",
                table: "Tickets",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DraftReplyGeneratedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);
        }
    }
}
