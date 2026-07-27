using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroServiceSystem.Services.Settings.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyTokensAndSettingConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin is a PostgreSQL system column — do not AddColumn it (conflicts with system name).
            // The model maps Version as a concurrency token onto xmin; snapshot carries that mapping.

            migrationBuilder.AlterColumn<string>(
                name: "key",
                schema: "settings",
                table: "settings",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "ix_settings_tenant_id_key",
                schema: "settings",
                table: "settings",
                columns: new[] { "tenant_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_settings_tenant_id_key",
                schema: "settings",
                table: "settings");

            migrationBuilder.AlterColumn<string>(
                name: "key",
                schema: "settings",
                table: "settings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);
        }
    }
}
