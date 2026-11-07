using JavaMaster.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Data;

/// <summary>
/// The lesson content for the "Learn" path. This code is the single source of truth:
/// the questions, the answers and the feedback all live here and are written into the
/// database on start-up, so the browser can never see an answer before it has earned it.
///
/// Every Explanation is written as a nudge, not as the answer. It points at the rule the
/// learner should apply, so a wrong answer still makes them think instead of handing
/// over the correct option.
/// </summary>
public static class LessonContent
{
    public sealed record Q(
        string Code,
        QuestionType Type,
        string Topic,
        string Prompt,
        object Payload,
        object AnswerKey,
        string Explanation);

    public sealed record Lesson(int NodeOrder, string NodeTitle, string IntroText, string OutroText, Q[] Questions);

    public static readonly Lesson[] Lessons =
    {
        // ---------------------------------------------------------------- Hello World
        new(1, "Hello World",
            "Welcome to the first chapter of the Java course! 🚀\n\nJava is a popular programming language designed by James Gosling to be \"written once, run anywhere\" with a Java Virtual Machine (JVM).",
            "Now that you can print a line of text, move on to the next lesson to start giving your code a place to keep its values.",
            new[]
        {
            new Q("hello-world.q1", QuestionType.MultipleChoice, "Hello World",
                "In Java, we use certain statements to display output. For example, System.out.println(); is used to print text to the console.\n\nWhich Java statement prints text to the console?",
                new { options = new[] {
                    new { id = "a", text = "System.out.println(\"Hello\");" },
                    new { id = "b", text = "String message = \"Hello\";" } } },
                new { correctOptionId = "a", correctMessage = "Great job!" },
                "Only one of these actually sends text to the console. The other one stores a value in memory without displaying it."),

            new Q("hello-world.q2", QuestionType.FillBlank, "Hello World",
                "Code \"Hello World!\" between the opening and closing quotation marks.",
                new { template = new[] { "System", ".", "out", ".", "println", "(", "\"{{blank}}\"", ")", ";" } },
                new { accepted = new[] { "Hello World!" }, caseSensitive = true,
                      output = "Hello World!", correctMessage = "Excellent!" },
                "Java is case-sensitive, so the text has to match exactly - the same capital letters and the same exclamation mark."),

            new Q("hello-world.q3", QuestionType.ArrangeTokens, "Hello World",
                "Let's remember how to code an output. Code an output that says \"Hello World!\".",
                new { tokens = new[] { "System", "out", "println", "(", ")", "\"Hello World!\"", ";" },
                      lines = new[] { new[] { "{{1}}", ".", "{{2}}", ".", "{{3}}", "{{4}}", "{{5}}", "{{6}}", "{{7}}" } } },
                new { order = new[] { "System", "out", "println", "(", "\"Hello World!\"", ")", ";" },
                      output = "Hello World!", correctMessage = "Excellent!" },
                "The order of the pieces matters. Build it the way Java reads a call: the object first, then the method, then the value."),

            new Q("hello-world.q4", QuestionType.MultipleChoice, "Hello World",
                "In Java, System.out.println(); prints text and moves to a new line, while System.out.print(); prints text but stays on the same line.\n\nWhich statement prints text without moving to a new line?",
                new { options = new[] {
                    new { id = "a", text = "System.out.println(\"Welcome\");" },
                    new { id = "b", text = "System.out.print(\"Welcome\");" } } },
                new { correctOptionId = "b", correctMessage = "Well done!" },
                "One of these ends the line and moves the cursor down. The other leaves the cursor exactly where it is."),

            new Q("hello-world.q5", QuestionType.FillSlots, "Hello World",
                "Code an output that displays \"The result is: 42\".\nRemember print stays on the same line, println moves to a new line.",
                new { lines = new[] {
                        new[] { "System", ".", "out", ".", "{{1}}", "(", "\"The result is: \"", ")", ";" },
                        new[] { "System", ".", "out", ".", "{{2}}", "(", "42", ")", ";" } },
                      bank = new[] { "println", "print" } },
                new { slots = new Dictionary<string, string> { ["1"] = "print", ["2"] = "println" },
                      output = "The result is: 42", correctMessage = "Amazing!" },
                "The first statement has to leave the cursor on the same line. The second one has to start on a new line.")
        }),

        // ------------------------------------------------------------------- Variables
        new(2, "Variables",
            "Welcome to lesson 2! 🚀\n\nA variable is a named container that holds a value in memory. Before Java lets you use one, you must declare it — and a declaration always starts with the type of data it holds.",
            "Now that your values have names, move on to the next lesson to start choosing the right type for each one.",
            new[]
        {
            new Q("variables.q1", QuestionType.MultipleChoice, "Variables",
                "A declaration tells Java three things: the type of data the variable holds, the name you will use for it, and optionally the value it starts with.\n\nWhich of these is a valid variable declaration?",
                new { options = new[] {
                    new { id = "a", text = "int age = 20;" },
                    new { id = "b", text = "age = 20;" },
                    new { id = "c", text = "variable int age = 20;" } } },
                new { correctOptionId = "a", correctMessage = "Great job!" },
                "A Java declaration always puts the type first, then the name, then the value. Check each option for all three in that order."),

            new Q("variables.q2", QuestionType.FillBlank, "Variables",
                "The type tells Java how much memory to reserve and what the variable is allowed to hold. Exactly one of Java's primitive types stores a decimal number like 9.99 without needing a suffix on the value.\n\nFill in the missing type.",
                new { template = new[] { "{{blank}}", "price", "=", "9.99", ";" } },
                new { accepted = new[] { "double" }, caseSensitive = true,
                      correctMessage = "Perfect - double holds decimal values." },
                "Two things can go wrong here. Java is case-sensitive, so the type has to be spelled exactly as Java writes it - a capitalized version would not compile. And the other floating-point type needs an f after the value, so it would not compile here either."),

            new Q("variables.q3", QuestionType.FillSlots, "Variables",
                "A boolean variable can hold only two values: true or false. It is often used as a flag, such as isReady.\n\nBuild the statement that declares isReady and sets it to true.",
                new { lines = new[] { new[] { "{{1}}", "{{2}}", "=", "{{3}}", ";" } },
                  bank = new[] { "boolean", "isReady", "true", "false", "int", "String", "null" } },
                new { slots = new Dictionary<string, string> { ["1"] = "boolean", ["2"] = "isReady", ["3"] = "true" },
                      correctMessage = "Nice! A boolean is either true or false." },
                "There are three blanks: the type, the name and the value. The prompt gives you the name and the value, so the type is the one that can only be true or false."),

            new Q("variables.q4", QuestionType.MultipleChoice, "Variables",
                "Java identifiers - the names you give variables and methods - are case-sensitive, may contain letters, digits, underscores and dollar signs, and cannot start with a digit. They also cannot be a keyword such as class or int.\n\nWhich of these is not a valid variable name?",
                new { options = new[] {
                    new { id = "a", text = "_total" },
                    new { id = "b", text = "$rate" },
                    new { id = "c", text = "2ndValue" },
                    new { id = "d", text = "myScore" } } },
                new { correctOptionId = "c", correctMessage = "Right - a name cannot begin with a digit." },
                "Three of these follow every naming rule. The odd one out breaks the rule about where a name is allowed to begin."),

            new Q("variables.q5", QuestionType.FillSlots, "Variables",
                "A final variable cannot be changed after it is created, which makes it a constant. Constants are written in ALL CAPS by convention. The highest score is a whole number.\n\nBuild the constant declaration for MAX_SCORE equal to 100.",
                new { lines = new[] { new[] { "final", "{{1}}", "{{2}}", "=", "{{3}}", ";" } },
                  bank = new[] { "int", "boolean", "MAX_SCORE", "MIN_SCORE", "100", "50", "final", "static" } },
                new { slots = new Dictionary<string, string> { ["1"] = "int", ["2"] = "MAX_SCORE", ["3"] = "100" },
                      correctMessage = "Great - that is a constant int." },
                "There are three blanks after final: the type, the name and the value. The prompt gives you the name and the value, so the type has to be the one that fits a whole number.")
        }),

        // ----------------------------------------------------------------- Data Types
        new(3, "Data Types",
            "Welcome to lesson 3! 🚀\n\nJava has eight primitive data types: byte, short, int and long for whole numbers, float and double for decimals, plus char and boolean.",
            "Now that our variables know what kind of data they hold, move on to the next lesson to start giving them behaviors and actions.",
            new[]
        {
            new Q("data-types.q1", QuestionType.MultipleChoice, "Data Types",
                "The four whole-number types are byte, short, int and long. The two decimal types are float and double. Then there is char for a single character and boolean for true or false.\n\nWhich type stores a single 16-bit Unicode character, such as a letter?",
                new { options = new[] {
                    new { id = "a", text = "String" },
                    new { id = "b", text = "char" },
                    new { id = "c", text = "int" },
                    new { id = "d", text = "byte" } } },
                new { correctOptionId = "b", correctMessage = "Right - char holds exactly one character." },
                "Java stores text in two ways: a whole word and a single letter. One of these options does the second one."),

            new Q("data-types.q2", QuestionType.FillBlank, "Data Types",
                "When a whole number is too large for an int, Java lets you write it as a long by adding the letter L to the end of the number. Without the L, the number is just an int.\n\nFill in the missing suffix.",
                new { template = new[] { "long", "total", "=", "42", "{{blank}}", ";" } },
                new { accepted = new[] { "L", "l" }, caseSensitive = false,
                      correctMessage = "Correct - 42L is a long literal." },
                "It is a single letter, and it goes straight after the digits with no space. Java accepts either a capital L or a small l here - this suffix is the one place where the case does not matter."),

            new Q("data-types.q3", QuestionType.FillSlots, "Data Types",
                "Every decimal literal without a suffix is a double. To store a smaller decimal in a float you must add an f - otherwise the compiler assumes you meant a double.\n\nBuild the statement that stores the value 3.14 in a variable called pi.",
                new { lines = new[] { new[] { "{{1}}", "{{2}}", "=", "3.14", ";" } },
                  bank = new[] { "double", "float", "pi", "int", "long", "3.14", "3.14f" } },
                new { slots = new Dictionary<string, string> { ["1"] = "double", ["2"] = "pi" },
                      correctMessage = "Nice - double pi = 3.14;" },
                "Two blanks: the type and the variable name. The value is already there, so pick the type that a decimal without a suffix becomes."),

            new Q("data-types.q4", QuestionType.MultipleChoice, "Data Types",
                "When both operands are whole numbers, Java does integer division and throws away the decimal part. To keep the decimal you must use a double or a float.\n\nWhat does  int x = 9; int y = 2; System.out.println(x / y);  print?",
                new { options = new[] {
                    new { id = "a", text = "4.5" },
                    new { id = "b", text = "4" },
                    new { id = "c", text = "5" },
                    new { id = "d", text = "Error" } } },
                new { correctOptionId = "b", correctMessage = "Right - 9 / 2 is 4, the remainder is dropped." },
                "Both operands are whole numbers here, so Java does integer division and drops the remainder. Work out 9 divided by 2 with the remainder thrown away."),

            new Q("data-types.q5", QuestionType.FillSlots, "Data Types",
                "A char literal is wrapped in single quotes, and a boolean holds only true or false. The declarations below store the letter A, and a flag that starts out false because the work is not finished yet.\n\nBuild the two declarations below.",
                new { lines = new[] {
                        new[] { "char", "letter", "=", "{{1}}", ";" },
                        new[] { "boolean", "finished", "=", "{{2}}", ";" } },
                  bank = new[] { "'A'", "A", "true", "false", "'B'", "'1'" } },
                new { slots = new Dictionary<string, string> { ["1"] = "'A'", ["2"] = "false" },
                      correctMessage = "Perfect - char uses single quotes, and a not-yet-finished flag is false." },
                "Two blanks, one per line. The first holds a character and needs the quote marks around it. The second holds a flag that has not finished yet.")
        }),

        // ----------------------------------------------------- Operators & Expressions
        new(5, "Operators & Expressions",
            "Welcome to lesson 4! 🚀\n\nOperators let you work with values — adding and dividing them, comparing them, and combining conditions. An expression is any combination of values and operators that produces a result.",
            "Now that you can calculate and compare values, move on to the next lesson to start making your programs make decisions.",
            new[]
        {
            new Q("operators-expressions.q1", QuestionType.MultipleChoice, "Operators & Expressions",
                "Java groups its operators into families: arithmetic (+ - * / %), unary (+ - ++ --), relational (> < >= <= == !=), logical (&& || !) and assignment (= += -= *= /=).\n\nWhich operator returns the remainder of a division?",
                new { options = new[] {
                    new { id = "a", text = "/" },
                    new { id = "b", text = "%" },
                    new { id = "c", text = "*" },
                    new { id = "d", text = "+" } } },
                new { correctOptionId = "b", correctMessage = "Right - % is the modulus operator." },
                "One of these is named after the mathematical term for remainder. The others either divide, multiply or add."),

            new Q("operators-expressions.q2", QuestionType.FillBlank, "Operators & Expressions",
                "When both operands are whole numbers, Java does integer division and drops the remainder - so 5 / 2 is 2, not 2.5. Casting one operand to a floating-point type forces a decimal result. Java has two: float and double. Either one works here.\n\nFill in the missing type inside the cast.",
                new { template = new[] { "System", ".", "out", ".", "println", "(", "(", "{{blank}}", ")", "a", "/", "b", ")", ";" } },
                new { accepted = new[] { "double", "float" }, caseSensitive = true,
                      output = "2.5", correctMessage = "Correct - the cast forces decimal division." },
                "It goes inside the parentheses, before the operand, and it has to be one of Java's two floating-point types. Java is case-sensitive, so spell it exactly as Java writes it."),

            new Q("operators-expressions.q3", QuestionType.FillSlots, "Operators & Expressions",
                "The compound assignment operator += adds and assigns in one step, so a += 3 means a = a + 3. It can be used directly inside a print statement.\n\nBuild the statement that adds 3 to a and prints the new value, when a is 5.",
                new { lines = new[] { new[] { "System", ".", "out", ".", "println", "(", "a", "{{1}}", "3", ")", ";" } },
                  bank = new[] { "+=", "-=", "*=", "=", "++" } },
                new { slots = new Dictionary<string, string> { ["1"] = "+=" },
                      output = "8", correctMessage = "Nice - a += 3 adds three and prints 8." },
                "One blank. It has to add three and assign the result in a single step, not just assign the number three."),

            new Q("operators-expressions.q4", QuestionType.MultipleChoice, "Operators & Expressions",
                "Relational operators compare two values and produce a boolean. == asks \"are they equal?\", while a single = assigns a value. The && operator means \"and\" and is true only when both sides are true.\n\nWhat does  int a = 5; int b = 2; System.out.println(a > b && b < a);  print?",
                new { options = new[] {
                    new { id = "a", text = "true" },
                    new { id = "b", text = "false" },
                    new { id = "c", text = "Error" },
                    new { id = "d", text = "1" } } },
                new { correctOptionId = "a", correctMessage = "Right - 5 > 2 and 2 < 5 are both true." },
                "Check each comparison on its own before combining them. With && the result depends on both of them."),

            new Q("operators-expressions.q5", QuestionType.FillSlots, "Operators & Expressions",
                "x++ is post-increment: it uses the old value of x and then increases it. ++x is pre-increment: it increases first and then uses the new value.\n\nBuild the statement that prints a after increasing it first, when a is 5.",
                new { lines = new[] { new[] { "System", ".", "out", ".", "println", "(", "{{1}}", "a", ")", ";" } },
                  bank = new[] { "++", "--", "+", "-" } },
                new { slots = new Dictionary<string, string> { ["1"] = "++" },
                      output = "6", correctMessage = "Exactly - ++a increases a before it is used, so it prints 6." },
                "One blank, and it has to go before the variable because the question asks for the value after the increase.")
        })
    };

    /// <summary>
    /// Writes the lesson content into the database. Existing questions are updated in place
    /// so their ids (and the learner's attempt history) stay intact.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db)
    {
        var nodes = await db.LearnNodes
            .Include(n => n.NodeQuestions).ThenInclude(nq => nq.Question)
            .ToListAsync();

        foreach (var lesson in Lessons)
        {
            var node = nodes.FirstOrDefault(n => n.SortOrder == lesson.NodeOrder && n.NodeType == NodeType.Lesson);
            if (node == null) continue;

            // The text of a lesson belongs to the database once it is there, so a
            // professor can rewrite it in the app without the code overwriting it.
            if (string.IsNullOrEmpty(node.IntroText)) node.IntroText = lesson.IntroText;
            if (string.IsNullOrEmpty(node.OutroText)) node.OutroText = lesson.OutroText;

            var topic = await db.Topics.FirstOrDefaultAsync(t => t.Title == lesson.NodeTitle);
            if (topic == null)
            {
                topic = new Topic { Title = lesson.NodeTitle };
                db.Topics.Add(topic);
                await db.SaveChangesAsync();
            }

            var links = node.NodeQuestions.OrderBy(nq => nq.SortOrder).ToList();

            // Questions that were seeded before codes existed are adopted rather
            // than duplicated, so upgrading does not throw the content away.
            for (int i = 0; i < lesson.Questions.Length && i < links.Count; i++)
            {
                var existing = links[i].Question;
                if (existing != null && existing.Code == null) existing.Code = lesson.Questions[i].Code;
            }
            await db.SaveChangesAsync();

            for (int i = 0; i < lesson.Questions.Length; i++)
            {
                var def = lesson.Questions[i];

                // The code provides the starting content. Once a question is in the
                // database it belongs to the database, so a professor who edits it in
                // the app does not lose that edit on the next start.
                var question = await db.Questions.FirstOrDefaultAsync(q => q.Code == def.Code);

                if (question == null)
                {
                    question = new Question
                    {
                        Code = def.Code,
                        Topic = topic,
                        Purpose = QuestionPurpose.Lesson,
                        Type = def.Type,
                        Prompt = def.Prompt,
                        Payload = Json(def.Payload),
                        AnswerKey = Json(def.AnswerKey),
                        Explanation = def.Explanation,
                        Hint = null,               // Learn mode has no power-ups.
                        Difficulty = 1,
                        IsActive = true
                    };
                    db.Questions.Add(question);

                    var link = new NodeQuestion { LearnNodeId = node.Id, Question = question, SortOrder = i + 1 };
                    db.NodeQuestions.Add(link);
                    node.NodeQuestions.Add(link);
                }
                else if (!node.NodeQuestions.Any(nq => nq.QuestionId == question.Id))
                {
                    // The question exists but is not in this node yet.
                    var link = new NodeQuestion { LearnNodeId = node.Id, Question = question, SortOrder = i + 1 };
                    db.NodeQuestions.Add(link);
                    node.NodeQuestions.Add(link);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static string Json(object o) => System.Text.Json.JsonSerializer.Serialize(o);
}
