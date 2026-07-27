using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coordinator.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSagaLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "locked_by",
                schema: "coordinator",
                table: "register_user_sagas",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until_utc",
                schema: "coordinator",
                table: "register_user_sagas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_register_user_sagas_state_locked_until",
                schema: "coordinator",
                table: "register_user_sagas",
                columns: new[] { "state", "locked_until_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_register_user_sagas_state_locked_until",
                schema: "coordinator",
                table: "register_user_sagas");

            migrationBuilder.DropColumn(
                name: "locked_by",
                schema: "coordinator",
                table: "register_user_sagas");

            migrationBuilder.DropColumn(
                name: "locked_until_utc",
                schema: "coordinator",
                table: "register_user_sagas");
        }
    }
}
