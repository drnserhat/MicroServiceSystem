using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroServiceSystem.Services.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistUserRoleIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "role_ids",
                schema: "identity",
                table: "identity_users",
                type: "uuid[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role_ids",
                schema: "identity",
                table: "identity_users");
        }
    }
}
