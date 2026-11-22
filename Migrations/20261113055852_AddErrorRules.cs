using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JavaMaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pattern = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Help = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorRules", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ErrorRules",
                columns: new[] { "Id", "Help", "IsActive", "Message", "Pattern", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Add the missing ; and run it again.", true, "Java needs a ; at the end of this line.", "^';' expected", 1 },
                    { 2, "Add a \" at the end of the text and run it again.", true, "This text is missing its closing quotation mark.", "^unclosed string literal", 2 },
                    { 3, "Write the type first, like int, double or String.", true, "“{1}” needs a type before it.", "^cannot find symbol \\[variable (\\w+)\\] \\[missing-type\\]", 3 },
                    { 4, "{2} starts with a capital letter. Fix it and run it again.", true, "Java doesn't know “{1}”. Did you mean “{2}”?", "\\[(?:class|variable) (\\w+)\\] \\[did-you-mean (\\w+)\\]", 4 },
                    { 5, "Check the spelling and the capital letters.", true, "Java doesn't know the command “{1}”.", "^cannot find symbol \\[method (\\w+)\\]", 5 },
                    { 6, "Check the spelling and the capital letters.", true, "Java doesn't know the type “{1}”.", "^cannot find symbol \\[class (\\w+)\\]", 6 },
                    { 7, "If it's text, put it inside quotation marks. If it's a variable, create it first.", true, "Java doesn't know the name “{1}”.", "^cannot find symbol \\[variable (\\w+)\\]", 7 },
                    { 8, "Change int to double and run it again.", true, "An int can't hold decimals. Use double for numbers like 3.14.", "possible lossy conversion from double to int", 8 },
                    { 9, "Check that the value matches the variable's type.", true, "A {1} value can't be stored in a {2} variable.", "^incompatible types: (\\w+) cannot be converted to (\\w+)", 9 },
                    { 10, "Use a different name, or remove the second one.", true, "You already created a variable named “{1}”.", "^variable (\\w+) is already defined", 10 },
                    { 11, "Did you forget a command such as System.out.println?", true, "This line isn't a complete instruction.", "^not a statement", 11 },
                    { 12, "Check your curly braces { }.", true, "Some code is outside the class.", "^class, interface, enum, or record expected", 12 },
                    { 13, "Every { needs a } to close it.", true, "A closing curly brace } is missing.", "^reached end of file while parsing", 13 },
                    { 14, "Check this line for a missing symbol.", true, "Java expected a {1} here.", "^'(.)' expected", 14 },
                    { 15, "Look for an extra or missing character, or check the spelling near this spot.", true, "Something on this line is out of place.", "^(?:illegal start of (?:expression|type)|<identifier> expected)", 15 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorRules");
        }
    }
}
