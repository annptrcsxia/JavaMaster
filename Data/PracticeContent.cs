using JavaMaster.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Data;

/// <summary>
/// The content of the Practice node on the Learn path (Java Basics 1).
/// Written into the database on start-up. Once a question is in the database it belongs to the database.
///
/// The learner types the code for the answer. The server compares it with the accepted answers.
/// Nothing is compiled or run. The Explanation is a nudge, never the answer.
/// "tip" in the payload is the text shown by the page's Hint button.
/// </summary>
public static class PracticeContent
{
    public sealed record Q(
        string Code,
        string Prompt,
        object Payload,
        object AnswerKey,
        string Explanation);

    public sealed record Practice(int NodeOrder, string NodeTitle, string OutroText, Q[] Questions);

    public static readonly Practice[] Practices =
    {
        new(4, "Java Basics 1",
            "You practiced printing output, declaring variables and choosing the right data types.\n\nNext up, learn how to calculate and compare values with operators and expressions.",
            new[]
        {
            new Q("java-basics-1-v2.q1",
                "Write code that prints <b>Hello, Java!</b> on the screen.",
                new { template = new[] { "{{blank}}" }, tip = "Tip: every Java statement ends with a ;" },
                new { accepted = new[] { "System.out.println(\"Hello, Java!\");" }, caseSensitive = true,
                      output = "Hello, Java!", correctMessage = "You did it! That was your first line of Java." },
                "Check four things: the capital letter in System, the spelling of println, the text between the quotation marks, and the ; at the end."),

            new Q("java-basics-1-v2.q2",
                "Write code that prints <b>Hello</b> on one line and <b>Java</b> on the next line.",
                new { template = new[] { "{{blank}}" }, tip = "Tip: use println twice, one for each line." },
                new { accepted = new[] { "System.out.println(\"Hello\");\nSystem.out.println(\"Java\");" }, caseSensitive = true,
                      output = "Hello\nJava", correctMessage = "Two lines, two printlns. Nice!" },
                "Each println prints one line. Use one for Hello and one for Java, each on its own line."),

            new Q("java-basics-1-v2.q3",
                "Create an <b>int</b> variable named <b>age</b> that stores <b>15</b>, then print it.",
                new { template = new[] { "{{blank}}" }, tip = "Tip: a variable needs a type, a name and a value." },
                new { accepted = new[] { "int age = 15;\nSystem.out.println(age);", "int age=15;\nSystem.out.println(age);" }, caseSensitive = true,
                      output = "15", correctMessage = "Your first variable. Well done!" },
                "Start with the type, then the name, then = and the value. Print the variable on the next line."),

            new Q("java-basics-1-v2.q4",
                "Create a <b>String</b> variable named <b>name</b> that stores <b>\"Alex\"</b>, then print it.",
                new { template = new[] { "{{blank}}" }, tip = "Tip: String is the type for text, and text goes inside quotes." },
                new { accepted = new[] { "String name = \"Alex\";\nSystem.out.println(name);", "String name=\"Alex\";\nSystem.out.println(name);" }, caseSensitive = true,
                      output = "Alex", correctMessage = "Text stored and printed. Great!" },
                "Text needs the type String with a capital S, and the text goes inside quotation marks. Then print the variable name."),

            new Q("java-basics-1-v2.q5",
                "Store the decimal number <b>3.14</b> in a variable named <b>pi</b>, then print it.",
                new { template = new[] { "{{blank}}" }, tip = "Tip: whole numbers and decimals use different types." },
                new { accepted = new[] { "double pi = 3.14;\nSystem.out.println(pi);", "double pi=3.14;\nSystem.out.println(pi);" }, caseSensitive = true,
                      output = "3.14", correctMessage = "Practice complete! Great job!" },
                "Decimals need a different type than whole numbers, and Java type names are lowercase. Print pi on the next line.")
        })
    };

    public static async Task SeedAsync(AppDbContext db)
    {
        var nodes = await db.LearnNodes
            .Include(n => n.NodeQuestions).ThenInclude(nq => nq.Question)
            .ToListAsync();

        foreach (var practice in Practices)
        {
            var node = nodes.FirstOrDefault(n => n.SortOrder == practice.NodeOrder && n.NodeType == NodeType.Practice);
            if (node == null) continue;

            // Text belongs to the database once it is there, so it is only written when empty.
            if (string.IsNullOrEmpty(node.OutroText)) node.OutroText = practice.OutroText;

            var topic = await db.Topics.FirstOrDefaultAsync(t => t.Title == practice.NodeTitle);
            if (topic == null)
            {
                topic = new Topic { Title = practice.NodeTitle };
                db.Topics.Add(topic);
                await db.SaveChangesAsync();
            }

            for (int i = 0; i < practice.Questions.Length; i++)
            {
                var def = practice.Questions[i];
                var question = await db.Questions.FirstOrDefaultAsync(q => q.Code == def.Code);

                if (question == null)
                {
                    question = new Question
                    {
                        Code = def.Code,
                        Topic = topic,
                        Purpose = QuestionPurpose.Lesson,
                        Type = QuestionType.FillBlank,
                        Prompt = def.Prompt,
                        CodeSnippet = null,
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