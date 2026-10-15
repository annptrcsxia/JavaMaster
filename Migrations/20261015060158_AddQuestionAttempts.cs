using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JavaMaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    LearnNodeId = table.Column<int>(type: "int", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAttempts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionAttempts");
        }
    }
}
