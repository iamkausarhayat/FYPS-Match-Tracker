using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FypTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Title ab unique nahi (alag universities ke titles same ho sakte hain).
            //    Index pehle hatana zaroori hai, warna column ki length badal nahi sakti.
            migrationBuilder.DropIndex(
                name: "IX_FypProjects_Title",
                table: "FypProjects");

            // 2) Lambe titles / abstracts / supervisor naam ke liye columns bade kiye
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "FypProjects",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FypProjects",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3000)",
                oldMaxLength: 3000);

            migrationBuilder.AlterColumn<string>(
                name: "SupervisorName",
                table: "FypProjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            // 3) Universities table
            migrationBuilder.CreateTable(
                name: "Universities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universities", x => x.Id);
                });

            // 4) Starting data. Ye FOREIGN KEY lagane se PEHLE honi chahiye
            //    (isi liye is file ko hath se theek kiya gaya hai).
            migrationBuilder.InsertData(
                table: "Universities",
                columns: new[] { "Id", "ApiBaseUrl", "Country", "Name", "ShortName", "SourceType", "Website" },
                values: new object[,]
                {
                    { 1, null, "Pakistan", "National University of Modern Languages", "NUML", "Manual", "https://www.numl.edu.pk" },
                    { 2, null, "-", "Sample Data (demo, not real projects)", "Sample", "Manual", null },
                    { 3, "https://escholar.umt.edu.pk/server", "Pakistan", "University of Management and Technology", "UMT", "DSpace", "https://escholar.umt.edu.pk" },
                    { 4, "https://repository.cuilahore.edu.pk/server", "Pakistan", "COMSATS University Islamabad, Lahore Campus", "CUI Lahore", "DSpace", "https://repository.cuilahore.edu.pk" },
                    { 5, "https://research.superior.edu.pk/server", "Pakistan", "Superior University", "Superior", "DSpace", "https://research.superior.edu.pk" }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Name" },
                values: new object[] { 5, "Other / Not specified" });

            // 5) FypProjects mein naye columns. Purane projects ko UniversityId = 1 (NUML) milta hai.
            migrationBuilder.AddColumn<int>(
                name: "UniversityId",
                table: "FypProjects",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "FypProjects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "FypProjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorsText",
                table: "FypProjects",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "FypProjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataQuality",
                table: "FypProjects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "FypProjects",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFetchedAt",
                table: "FypProjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAward",
                table: "FypProjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AwardText",
                table: "FypProjects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // 6) Code mein pehle se likhe 8 sample projects (Id 1 se 8) nakli hain,
            //    unhein "Sample Data" university (Id 2) mein daal do.
            for (int id = 1; id <= 8; id++)
            {
                migrationBuilder.UpdateData(
                    table: "FypProjects",
                    keyColumn: "Id",
                    keyValue: id,
                    column: "UniversityId",
                    value: 2);
            }

            // 7) Indexes aur foreign key
            migrationBuilder.CreateIndex(
                name: "IX_Universities_Name",
                table: "Universities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FypProjects_UniversityId_ExternalId",
                table: "FypProjects",
                columns: new[] { "UniversityId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FypProjects_ContentHash",
                table: "FypProjects",
                column: "ContentHash");

            migrationBuilder.AddForeignKey(
                name: "FK_FypProjects_Universities_UniversityId",
                table: "FypProjects",
                column: "UniversityId",
                principalTable: "Universities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FypProjects_Universities_UniversityId",
                table: "FypProjects");

            migrationBuilder.DropIndex(
                name: "IX_FypProjects_UniversityId_ExternalId",
                table: "FypProjects");

            migrationBuilder.DropIndex(
                name: "IX_FypProjects_ContentHash",
                table: "FypProjects");

            migrationBuilder.DropTable(
                name: "Universities");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(name: "UniversityId", table: "FypProjects");
            migrationBuilder.DropColumn(name: "SourceUrl", table: "FypProjects");
            migrationBuilder.DropColumn(name: "ExternalId", table: "FypProjects");
            migrationBuilder.DropColumn(name: "AuthorsText", table: "FypProjects");
            migrationBuilder.DropColumn(name: "Area", table: "FypProjects");
            migrationBuilder.DropColumn(name: "DataQuality", table: "FypProjects");
            migrationBuilder.DropColumn(name: "ContentHash", table: "FypProjects");
            migrationBuilder.DropColumn(name: "LastFetchedAt", table: "FypProjects");
            migrationBuilder.DropColumn(name: "IsAward", table: "FypProjects");
            migrationBuilder.DropColumn(name: "AwardText", table: "FypProjects");

            // Agar naye lambe titles save ho chuke hon to Title wali line fail ho sakti hai
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "FypProjects",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FypProjects",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "SupervisorName",
                table: "FypProjects",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_FypProjects_Title",
                table: "FypProjects",
                column: "Title",
                unique: true);
        }
    }
}