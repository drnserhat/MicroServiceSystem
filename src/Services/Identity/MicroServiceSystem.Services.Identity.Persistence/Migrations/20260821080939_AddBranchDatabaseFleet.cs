using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroServiceSystem.Services.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchDatabaseFleet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "postgres_clusters",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    host = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    admin_secret_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    max_databases = table.Column<int>(type: "integer", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postgres_clusters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_database_bindings",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    database_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    secret_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    schema_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_error = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_database_bindings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_postgres_clusters_slug",
                schema: "identity",
                table: "postgres_clusters",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_database_bindings_cluster_id_database_name",
                schema: "identity",
                table: "tenant_database_bindings",
                columns: new[] { "cluster_id", "database_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_database_bindings_tenant_id_service_key",
                schema: "identity",
                table: "tenant_database_bindings",
                columns: new[] { "tenant_id", "service_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "postgres_clusters",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tenant_database_bindings",
                schema: "identity");
        }
    }
}
