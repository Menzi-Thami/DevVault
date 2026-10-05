using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevVault.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SnippetListKeysetIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Snippets_CreatedByUserId_CreatedAt_Id",
                table: "Snippets",
                columns: new[] { "CreatedByUserId", "CreatedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Snippets_CreatedByUserId_CreatedAt_Id",
                table: "Snippets");
        }
    }
}
