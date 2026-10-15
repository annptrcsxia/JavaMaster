using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JavaMaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeIntroOutro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntroText",
                table: "LearnNodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutroText",
                table: "LearnNodes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntroText",
                table: "LearnNodes");

            migrationBuilder.DropColumn(
                name: "OutroText",
                table: "LearnNodes");
        }
    }
}
