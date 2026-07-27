using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordinator.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDeadLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dead_lettered_on_utc",
                schema: "coordinator",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_dead_lettered",
                schema: "coordinator",
                table: "outbox_messages",
                column: "dead_lettered_on_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_dead_lettered",
                schema: "coordinator",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "dead_lettered_on_utc",
                schema: "coordinator",
                table: "outbox_messages");
        }
    }
}
