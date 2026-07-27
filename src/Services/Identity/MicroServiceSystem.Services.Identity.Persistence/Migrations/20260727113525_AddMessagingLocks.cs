using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroServiceSystem.Services.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "locked_by",
                schema: "identity",
                table: "outbox_messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until_utc",
                schema: "identity",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until_utc",
                schema: "identity",
                table: "inbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_locked_until",
                schema: "identity",
                table: "inbox_messages",
                column: "locked_until_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_inbox_messages_locked_until",
                schema: "identity",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "locked_by",
                schema: "identity",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "locked_until_utc",
                schema: "identity",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "locked_until_utc",
                schema: "identity",
                table: "inbox_messages");
        }
    }
}
