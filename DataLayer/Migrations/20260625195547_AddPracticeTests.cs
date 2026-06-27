using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(900)", maxLength: 900, nullable: false),
                    IsTimed = table.Column<bool>(type: "bit", nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeTests_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PracticeTestAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PracticeTestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    ScorePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeTestAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeTestAttempts_PracticeTests_PracticeTestId",
                        column: x => x.PracticeTestId,
                        principalTable: "PracticeTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PracticeTestAttempts_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PracticeTestQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PracticeTestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OptionB = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OptionC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OptionD = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CorrectOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeTestQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeTestQuestions_PracticeTests_PracticeTestId",
                        column: x => x.PracticeTestId,
                        principalTable: "PracticeTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTestAttempts_StudentId",
                table: "PracticeTestAttempts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTestAttempts_TestId",
                table: "PracticeTestAttempts",
                column: "PracticeTestId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTestQuestions_Test_Order",
                table: "PracticeTestQuestions",
                columns: new[] { "PracticeTestId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTests_Category_Order",
                table: "PracticeTests",
                columns: new[] { "CategoryId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeTestAttempts");

            migrationBuilder.DropTable(
                name: "PracticeTestQuestions");

            migrationBuilder.DropTable(
                name: "PracticeTests");
        }
    }
}
