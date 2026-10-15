using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JavaMaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearnSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LearnSectionId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NodeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    XpReward = table.Column<int>(type: "int", nullable: false),
                    CoinReward = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnNodes_LearnSections_LearnSectionId",
                        column: x => x.LearnSectionId,
                        principalTable: "LearnSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearnNodes_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NodeQuestions",
                columns: table => new
                {
                    LearnNodeId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeQuestions", x => new { x.LearnNodeId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_NodeQuestions_LearnNodes_LearnNodeId",
                        column: x => x.LearnNodeId,
                        principalTable: "LearnNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeQuestions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNodeProgress",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LearnNodeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TimesCompleted = table.Column<int>(type: "int", nullable: false),
                    LastPlayedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNodeProgress", x => new { x.UserId, x.LearnNodeId });
                    table.ForeignKey(
                        name: "FK_UserNodeProgress_LearnNodes_LearnNodeId",
                        column: x => x.LearnNodeId,
                        principalTable: "LearnNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNodeProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnNodes_LearnSectionId",
                table: "LearnNodes",
                column: "LearnSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnNodes_TopicId",
                table: "LearnNodes",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeQuestions_QuestionId",
                table: "NodeQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNodeProgress_LearnNodeId",
                table: "UserNodeProgress",
                column: "LearnNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodeQuestions");

            migrationBuilder.DropTable(
                name: "UserNodeProgress");

            migrationBuilder.DropTable(
                name: "LearnNodes");

            migrationBuilder.DropTable(
                name: "LearnSections");
        }
    }
}
