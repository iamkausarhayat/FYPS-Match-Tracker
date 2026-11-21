using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FypTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmbeddingJson",
                table: "FypProjects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "FypProjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingJson",
                table: "ExternalPapers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "ExternalPapers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "FypProjects",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmbeddingJson", "EmbeddingModel" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "FypProjects",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EmbeddingJson", "EmbeddingModel" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "FypProjects",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EmbeddingJson", "EmbeddingModel" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "FypProjects",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EmbeddingJson", "EmbeddingModel" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "FypProjects",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "EmbeddingJson", "EmbeddingModel" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "FypProjects",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "EmbeddingJson", "EmbeddingModel" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "FypProjects",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "EmbeddingJson", "EmbeddingModel" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "FypProjects",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "EmbeddingJson", "EmbeddingModel" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddingJson",
                table: "FypProjects");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "FypProjects");

            migrationBuilder.DropColumn(
                name: "EmbeddingJson",
                table: "ExternalPapers");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "ExternalPapers");
        }
    }
}
