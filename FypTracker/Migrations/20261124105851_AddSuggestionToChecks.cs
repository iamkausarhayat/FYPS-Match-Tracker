using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FypTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestionToChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SuggestionJson",
                table: "SimilarityChecks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuggestionJson",
                table: "SimilarityChecks");
        }
    }
}
