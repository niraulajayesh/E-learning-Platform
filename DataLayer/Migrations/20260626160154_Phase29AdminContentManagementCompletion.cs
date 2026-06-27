using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class Phase29AdminContentManagementCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleAnswers",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Questions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "Questions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Easy");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Questions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionBankQuestionId",
                table: "Questions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "QuestionBankQuestions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedTimeSeconds",
                table: "QuestionBankQuestions",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<string>(
                name: "ExplanationImageUrl",
                table: "QuestionBankQuestions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "QuestionBankQuestions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionE",
                table: "QuestionBankQuestions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionF",
                table: "QuestionBankQuestions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionImageUrl",
                table: "QuestionBankQuestions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "QuestionBankQuestions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "QuestionBankQuestions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<string>(
                name: "Subtopic",
                table: "QuestionBankQuestions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "QuestionBankQuestions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "QuestionBankQuestions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"UPDATE qb SET qb.Status = 'Published', qb.Topic = CASE WHEN qb.Topic = '' THEN c.Name ELSE qb.Topic END FROM QuestionBankQuestions qb INNER JOIN Categories c ON qb.CategoryId = c.Id WHERE qb.Status = 'Draft';");

            migrationBuilder.AddColumn<bool>(
                name: "IsMockExam",
                table: "PracticeTests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PassingScorePercent",
                table: "PracticeTests",
                type: "int",
                nullable: false,
                defaultValue: 70);

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleAnswers",
                table: "PracticeTests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShuffleQuestions",
                table: "PracticeTests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OptionE",
                table: "PracticeTestQuestions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionF",
                table: "PracticeTestQuestions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionBankQuestionId",
                table: "PracticeTestQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_IsPublished",
                table: "Quizzes",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CategoryId",
                table: "Questions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionBankQuestionId",
                table: "Questions",
                column: "QuestionBankQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_Status",
                table: "QuestionBankQuestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_Topic",
                table: "QuestionBankQuestions",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTests_IsMockExam",
                table: "PracticeTests",
                column: "IsMockExam");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTestQuestions_QuestionBankQuestionId",
                table: "PracticeTestQuestions",
                column: "QuestionBankQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeTestQuestions_QuestionBankQuestions_QuestionBankQuestionId",
                table: "PracticeTestQuestions",
                column: "QuestionBankQuestionId",
                principalTable: "QuestionBankQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Categories_CategoryId",
                table: "Questions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_QuestionBankQuestions_QuestionBankQuestionId",
                table: "Questions",
                column: "QuestionBankQuestionId",
                principalTable: "QuestionBankQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeTestQuestions_QuestionBankQuestions_QuestionBankQuestionId",
                table: "PracticeTestQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Categories_CategoryId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_QuestionBankQuestions_QuestionBankQuestionId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_IsPublished",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CategoryId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_QuestionBankQuestionId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionBank_Status",
                table: "QuestionBankQuestions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionBank_Topic",
                table: "QuestionBankQuestions");

            migrationBuilder.DropIndex(
                name: "IX_PracticeTests_IsMockExam",
                table: "PracticeTests");

            migrationBuilder.DropIndex(
                name: "IX_PracticeTestQuestions_QuestionBankQuestionId",
                table: "PracticeTestQuestions");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "ShuffleAnswers",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionBankQuestionId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "EstimatedTimeSeconds",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "ExplanationImageUrl",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "OptionE",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "OptionF",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "QuestionImageUrl",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "Subtopic",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "QuestionBankQuestions");

            migrationBuilder.DropColumn(
                name: "IsMockExam",
                table: "PracticeTests");

            migrationBuilder.DropColumn(
                name: "PassingScorePercent",
                table: "PracticeTests");

            migrationBuilder.DropColumn(
                name: "ShuffleAnswers",
                table: "PracticeTests");

            migrationBuilder.DropColumn(
                name: "ShuffleQuestions",
                table: "PracticeTests");

            migrationBuilder.DropColumn(
                name: "OptionE",
                table: "PracticeTestQuestions");

            migrationBuilder.DropColumn(
                name: "OptionF",
                table: "PracticeTestQuestions");

            migrationBuilder.DropColumn(
                name: "QuestionBankQuestionId",
                table: "PracticeTestQuestions");
        }
    }
}

