using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FypTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverageToChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComparedProjectCount",
                table: "SimilarityChecks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ComparedUniversityCount",
                table: "SimilarityChecks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComparedProjectCount",
                table: "SimilarityChecks");

            migrationBuilder.DropColumn(
                name: "ComparedUniversityCount",
                table: "SimilarityChecks");
        }
    }
}
