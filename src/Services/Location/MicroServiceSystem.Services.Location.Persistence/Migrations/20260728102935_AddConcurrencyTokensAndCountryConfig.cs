using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroServiceSystem.Services.Location.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyTokensAndCountryConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin is a PostgreSQL system column — do not AddColumn it (conflicts with system name).
            // The model maps Version as a concurrency token onto xmin; snapshot carries that mapping.

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "location",
                table: "countries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "location",
                table: "countries",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "ix_countries_tenant_id_code",
                schema: "location",
                table: "countries",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_countries_tenant_id_code",
                schema: "location",
                table: "countries");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "location",
                table: "countries",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "location",
                table: "countries",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);
        }
    }
}
