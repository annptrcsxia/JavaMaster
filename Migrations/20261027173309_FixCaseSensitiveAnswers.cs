using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JavaMaster.Api.Migrations
{
    /// <summary>
    /// Brings the lesson content that is already in the database in line with
    /// the case-sensitivity rule.
    ///
    /// The seeder is insert-only on purpose - once a question is in the database
    /// the database owns it - so a change to the wording or the answer key in
    /// Data/LessonContent.cs does not reach a database that already has the
    /// question. This migration is the one-time patch that carries the change
    /// into the databases that already exist.
    ///
    /// It only touches the questions that carry a known code, so a question a
    /// professor added by hand is never affected.
    /// </summary>
    public partial class FixCaseSensitiveAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Java is case-sensitive, so a typed answer is compared exactly.
            // The cast question was the odd one out: it accepted DOUBLE and
            // FLOAT, while the type question in the lesson before it did not
            // accept DOUBLE. Now both compare exactly.
            Update(migrationBuilder, "operators-expressions.q2",
                answerKey: """{"accepted":["double","float"],"caseSensitive":true,"output":"2.5","correctMessage":"Correct - the cast forces decimal division."}""",
                explanation: "It goes inside the parentheses, before the operand, and it has to be one of Java's two floating-point types. Java is case-sensitive, so spell it exactly as Java writes it.");

            // The correction a learner reads when the answer is wrong has to name
            // the rule that was actually broken, and the case is now one of them.
            Update(migrationBuilder, "hello-world.q2",
                explanation: "Java is case-sensitive, so the text has to match exactly - the same capital letters and the same exclamation mark.");

            Update(migrationBuilder, "variables.q2",
                explanation: "Two things can go wrong here. Java is case-sensitive, so the type has to be spelled exactly as Java writes it - a capitalized version would not compile. And the other floating-point type needs an f after the value, so it would not compile here either.");

            // This one is the exception: Java accepts a capital L and a small l
            // for the long suffix, so the question opts out of the strict rule.
            Update(migrationBuilder, "data-types.q2",
                explanation: "It is a single letter, and it goes straight after the digits with no space. Java accepts either a capital L or a small l here - this suffix is the one place where the case does not matter.");

            // The arrange-tokens question lost the line it is drawn from when the
            // lesson content was copied into two places and the older copy won.
            // Without the line there is nowhere to put the tokens and the
            // question cannot be answered at all.
            Update(migrationBuilder, "hello-world.q3",
                payload: """{"tokens":["System","out","println","(",")","\"Hello World!\"",";"],"lines":[["{{1}}",".","{{2}}",".","{{3}}","{{4}}","{{5}}","{{6}}","{{7}}"]]}""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Update(migrationBuilder, "operators-expressions.q2",
                answerKey: """{"accepted":["double","float"],"caseSensitive":false,"output":"2.5","correctMessage":"Correct - the cast forces decimal division."}""",
                explanation: "It goes inside the parentheses, before the operand, and it has to be one of Java's two floating-point types.");

            Update(migrationBuilder, "hello-world.q2",
                explanation: "Type the text exactly as the question shows it, including the capital letters and the exclamation mark.");

            Update(migrationBuilder, "variables.q2",
                explanation: "The other floating-point type needs an f after the value, so it would not compile here.");

            Update(migrationBuilder, "data-types.q2",
                explanation: "It is a single letter, and it goes straight after the digits with no space.");

            Update(migrationBuilder, "hello-world.q3",
                payload: """{"tokens":["System","out","println","(",")","\"Hello World!\"",";"]}""");
        }

        // Writes the given columns of one question. Only the columns that are
        // passed are touched, so a question that only needs its explanation
        // rewritten keeps the answer key exactly as the professor left it.
        private static void Update(MigrationBuilder migrationBuilder, string code,
                                    string answerKey = null, string explanation = null,
                                    string payload = null)
        {
            var assignments = new List<string>();
            if (answerKey != null) assignments.Add($"AnswerKey = N'{Literal(answerKey)}'");
            if (explanation != null) assignments.Add($"Explanation = N'{Literal(explanation)}'");
            if (payload != null) assignments.Add($"Payload = N'{Literal(payload)}'");
            if (assignments.Count == 0) return;

            var set = string.Join(", ", assignments);
            migrationBuilder.Sql($"UPDATE Questions SET {set} WHERE Code = N'{Literal(code)}';");
        }

        // A single quote inside a T-SQL string literal is written twice.
        private static string Literal(string value) => value.Replace("'", "''");
    }
}