using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyGuides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyGuides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Theory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Examples = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyConcepts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tips = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyGuides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyGuides_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyGuideBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyGuideBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyGuideBookmarks_StudyGuides_StudyGuideId",
                        column: x => x.StudyGuideId,
                        principalTable: "StudyGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyGuideBookmarks_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudyGuideBookmarks_Guide_Student",
                table: "StudyGuideBookmarks",
                columns: new[] { "StudyGuideId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyGuideBookmarks_StudentId",
                table: "StudyGuideBookmarks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyGuides_Category_Order",
                table: "StudyGuides",
                columns: new[] { "CategoryId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyGuideBookmarks");

            migrationBuilder.DropTable(
                name: "StudyGuides");
        }
    }
}
