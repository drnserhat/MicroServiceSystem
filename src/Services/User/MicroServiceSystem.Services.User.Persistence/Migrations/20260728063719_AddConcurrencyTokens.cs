using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroServiceSystem.Services.User.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyTokens : Migration
    {
        // Deliberately empty. The model now maps a concurrency token onto the xmin system column that
        // PostgreSQL maintains on every table, so there is nothing to create. The AddColumn statement
        // EF scaffolded here would fail with "column name \"xmin\" conflicts with a system column name".

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
