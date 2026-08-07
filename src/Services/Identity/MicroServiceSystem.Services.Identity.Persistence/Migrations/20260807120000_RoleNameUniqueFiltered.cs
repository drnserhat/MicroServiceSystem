using MicroServiceSystem.Services.Identity.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroServiceSystem.Services.Identity.Persistence.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260807120000_RoleNameUniqueFiltered")]
public partial class RoleNameUniqueFiltered : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_roles_tenant_id_normalized_name",
            schema: "identity",
            table: "roles");

        migrationBuilder.CreateIndex(
            name: "ix_roles_tenant_id_normalized_name",
            schema: "identity",
            table: "roles",
            columns: ["tenant_id", "normalized_name"],
            unique: true,
            filter: "NOT is_deleted");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_roles_tenant_id_normalized_name",
            schema: "identity",
            table: "roles");

        migrationBuilder.CreateIndex(
            name: "ix_roles_tenant_id_normalized_name",
            schema: "identity",
            table: "roles",
            columns: ["tenant_id", "normalized_name"],
            unique: true);
    }
}
