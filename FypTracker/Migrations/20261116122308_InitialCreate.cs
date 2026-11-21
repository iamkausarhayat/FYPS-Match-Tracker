using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FypTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalPapers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Abstract = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicationYear = table.Column<int>(type: "int", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Authors = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalPapers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FypProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    SupervisorName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FypProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FypProjects_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RollNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SimilarityChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    SupervisorName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    OverallScore = table.Column<double>(type: "float", nullable: false),
                    Verdict = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimilarityChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimilarityChecks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SimilarityMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MeaningScore = table.Column<double>(type: "float", nullable: false),
                    WordScore = table.Column<double>(type: "float", nullable: false),
                    FinalScore = table.Column<double>(type: "float", nullable: false),
                    CommonKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BestPassage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SimilarityCheckId = table.Column<int>(type: "int", nullable: false),
                    FypProjectId = table.Column<int>(type: "int", nullable: true),
                    ExternalPaperId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimilarityMatches", x => x.Id);
                    table.CheckConstraint("CK_SimilarityMatches_OneTarget", "([FypProjectId] IS NOT NULL AND [ExternalPaperId] IS NULL) OR ([FypProjectId] IS NULL AND [ExternalPaperId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SimilarityMatches_ExternalPapers_ExternalPaperId",
                        column: x => x.ExternalPaperId,
                        principalTable: "ExternalPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SimilarityMatches_FypProjects_FypProjectId",
                        column: x => x.FypProjectId,
                        principalTable: "FypProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SimilarityMatches_SimilarityChecks_SimilarityCheckId",
                        column: x => x.SimilarityCheckId,
                        principalTable: "SimilarityChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Computer Science" },
                    { 2, "Software Engineering" },
                    { 3, "Artificial Intelligence" },
                    { 4, "Data Science" }
                });

            migrationBuilder.InsertData(
                table: "FypProjects",
                columns: new[] { "Id", "CreatedAt", "DepartmentId", "Description", "SupervisorName", "Title", "Year" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Identifies each student from a live camera feed and records presence in the class register automatically, so teachers no longer call the roll.", "Dr. Imran Shah", "Automated Class Presence System with Camera-Based Student Identification", 2023 },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "A web portal that stores student records and lets teachers update attendance and marks, while students view their results online.", "Ms. Hina Malik", "Online Student Management Portal", 2022 },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Students reserve a study seat in advance, and the app releases the seat automatically if they do not arrive on time.", "Dr. Sana Iqbal", "Library Seat Booking App", 2021 },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "Farmers photograph a leaf and a trained model names the likely disease and suggests a treatment, even with a weak internet connection.", "Dr. Bilal Aslam", "Crop Disease Detection from Leaf Photos", 2024 },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Predicts which patients are likely to miss an appointment and sends them reminders, helping clinics reduce empty time slots.", "Ms. Ayesha Noor", "Hospital Appointment Reminder and No-Show Predictor", 2024 },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, "A voice-guided mobile app that describes routes between campus buildings and warns users about obstacles and stairs.", "Dr. Farhan Ali", "Campus Navigation Assistant for Visually Impaired Students", 2023 },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Restaurants list surplus meals at a discount and nearby users reserve them, so edible food is not thrown away at closing time.", "Mr. Usman Tariq", "Food Waste Reduction Platform for Local Restaurants", 2022 },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Uses past bus timings and weather data to predict delays and suggests the fastest route for daily commuters.", "Dr. Sadia Khan", "Public Transport Delay Prediction and Route Planner", 2025 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPapers_ExternalId",
                table: "ExternalPapers",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FypProjects_DepartmentId",
                table: "FypProjects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FypProjects_Title",
                table: "FypProjects",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimilarityChecks_UserId",
                table: "SimilarityChecks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SimilarityMatches_ExternalPaperId",
                table: "SimilarityMatches",
                column: "ExternalPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_SimilarityMatches_FypProjectId",
                table: "SimilarityMatches",
                column: "FypProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SimilarityMatches_SimilarityCheckId",
                table: "SimilarityMatches",
                column: "SimilarityCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimilarityMatches");

            migrationBuilder.DropTable(
                name: "ExternalPapers");

            migrationBuilder.DropTable(
                name: "FypProjects");

            migrationBuilder.DropTable(
                name: "SimilarityChecks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
