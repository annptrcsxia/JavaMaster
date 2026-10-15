using System.Text.Json;
using JavaMaster.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Data;

public static class DbSeeder
{
    private static string Json(object o) => JsonSerializer.Serialize(o);

    public static async Task SeedAsync(AppDbContext db)
    {
        // Ranks are seeded first, on their own check, so they still get created
        // even when the learn content below has already been seeded.
        if (!await db.Ranks.AnyAsync())
        {
            db.Users.Add(new User // TEMPORARY test user, removed when Microsoft login is built
                {
                MicrosoftObjectId = "test-user",
                Email = "test.student@example.com",
                FullName = "Test Student",
                Coins = 80
                });
            await db.SaveChangesAsync();
            if (!await db.Ranks.AnyAsync())
            {
                db.Ranks.AddRange(
                    new Rank { Name = "Starter",  MinXp = 0,    MaxXp = 300,    IconFile = "starter.png" },
                    new Rank { Name = "Beginner", MinXp = 300,  MaxXp = 700,    IconFile = "beginner.png" },
                    new Rank { Name = "Rising",   MinXp = 700,  MaxXp = 1200,   IconFile = "rising.png" },
                    new Rank { Name = "Skilled",  MinXp = 1200, MaxXp = 1800,   IconFile = "skilled.png" },
                    new Rank { Name = "Pro",      MinXp = 1800, MaxXp = 3000,   IconFile = "pro.png" },
                    new Rank { Name = "Elite",    MinXp = 3000, MaxXp = 4500,   IconFile = "elite.png" },
                    new Rank { Name = "Boss",     MinXp = 4500, MaxXp = 6500,   IconFile = "boss.png" },
                    new Rank { Name = "Legend",   MinXp = 6500, MaxXp = 999999, IconFile = "legend.png" });
                await db.SaveChangesAsync();
            }

        if (await db.LearnSections.AnyAsync()) return;

        async Task<Topic> GetTopic(string title)
        {
            var t = await db.Topics.FirstOrDefaultAsync(x => x.Title == title);
            if (t == null) { t = new Topic { Title = title }; db.Topics.Add(t); }
            return t;
        }
        var tHello = await GetTopic("Hello World");
        var tVars  = await GetTopic("Variables");
        var tTypes = await GetTopic("Data Types");
        var tOps   = await GetTopic("Operators & Expressions");

        // NOTE: Hint is null on purpose. Learn mode has no power-ups.
        // Explanation = message shown when the answer is WRONG.
        // AnswerKey (server only) holds the answer, the praise message, and the output panel text.

        // Q1: multiple choice
        var q1 = new Question
        {
            Topic = tHello, Type = QuestionType.MultipleChoice, Purpose = QuestionPurpose.Lesson,
            Prompt = "In Java, we use certain statements to display output. For example, System.out.println(); is used to print text to the console.\n\nWhich Java statement prints text to the console?",
            Payload = Json(new { options = new[] {
                new { id = "a", text = "System.out.println(\"Hello\");" },
                new { id = "b", text = "String message = \"Hello\";" } } }),
            AnswerKey = Json(new { correctOptionId = "a", correctMessage = "Great job!" }),
            Explanation = "That only creates a variable. Use System.out.println() to print."
        };

        // Q2: type inside the code box
        var q2 = new Question
        {
            Topic = tHello, Type = QuestionType.FillBlank, Purpose = QuestionPurpose.Lesson,
            Prompt = "Code \"Hello World!\" between the opening and closing quotation marks.",
            Payload = Json(new { template = new[] { "System", ".", "out", ".", "println", "(", "\"{{blank}}\"", ")", ";" } }),
            AnswerKey = Json(new { accepted = new[] { "Hello World!" }, caseSensitive = true,
                                   output = "Hello World!", correctMessage = "Excellent!" }),
            Explanation = "Type exactly \"Hello World!\"."
        };

        // Q3: arrange tokens (the dots between tokens are drawn by the frontend)
        var q3 = new Question
        {
            Topic = tHello, Type = QuestionType.ArrangeTokens, Purpose = QuestionPurpose.Lesson,
            Prompt = "Let's remember how to code an output. Code an output that says \"Hello World!\".",
            Payload = Json(new { tokens = new[] { "System", "out", "println", "(", ")", "\"Hello World!\"", ";" } }),
            AnswerKey = Json(new { order = new[] { "System", "out", "println", "(", "\"Hello World!\"", ")", ";" },
                                   output = "Hello World!", correctMessage = "Excellent!" }),
            Explanation = "The order is off. It should be System.out.println(\"Hello World!\");"
        };

        // Q4: multiple choice
        var q4 = new Question
        {
            Topic = tHello, Type = QuestionType.MultipleChoice, Purpose = QuestionPurpose.Lesson,
            Prompt = "In Java, System.out.println(); prints text and moves to a new line, while System.out.print(); prints text but stays on the same line.\n\nWhich statement prints text without moving to a new line?",
            Payload = Json(new { options = new[] {
                new { id = "a", text = "System.out.println(\"Welcome\");" },
                new { id = "b", text = "System.out.print(\"Welcome\");" } } }),
            AnswerKey = Json(new { correctOptionId = "b", correctMessage = "Well done!" }),
            Explanation = "That moves to a new line. Use System.out.print() to stay on the same line."
        };

        // Q5: fill the slots from a bank (new type: FillSlots)
        var q5 = new Question
        {
            Topic = tHello, Type = QuestionType.FillSlots, Purpose = QuestionPurpose.Lesson,
            Prompt = "Code an output that displays \"The result is: 42\".\nRemember print stays on the same line, println moves to a new line.",
            Payload = Json(new {
                lines = new[] {
                    new[] { "System", ".", "out", ".", "{{1}}", "(", "\"The result is: \"", ")", ";" },
                    new[] { "System", ".", "out", ".", "{{2}}", "(", "42", ")", ";" } },
                bank = new[] { "println", "print" } }),
            AnswerKey = Json(new { slots = new Dictionary<string, string> { ["1"] = "print", ["2"] = "println" },
                                   output = "The result is: 42", correctMessage = "Amazing!" }),
            Explanation = "Use print for the first line so 42 stays on the same line."
        };

        var helloQuestions = new[] { q1, q2, q3, q4, q5 };

        var intro = new LearnSection { Title = "Introduction to Java", SortOrder = 1, IsPublished = true };

        LearnNode Node(string title, string sub, NodeType type, int order, Topic? topic, int xp, int coins) =>
            new LearnNode { Title = title, Subtitle = sub, NodeType = type, SortOrder = order,
                            Topic = topic, XpReward = xp, CoinReward = coins };

        // Hello World: rewards match your prototype (120 XP, +30 coins)
        var hello = Node("Hello World", "Your journey starts here.", NodeType.Lesson, 1, tHello, 120, 30);
        hello.IntroText = "Welcome to the first chapter of the Java course! 🚀\n\nJava is a popular programming language designed by James Gosling to be \"written once, run anywhere\" with a Java Virtual Machine (JVM).";
        hello.OutroText = "Awesome! We've learned the basics of Java\n\nBefore we add actions to them, move on to the next lesson to learn about the Java syntax present on every program out there.";
        for (int i = 0; i < helloQuestions.Length; i++)
            hello.NodeQuestions.Add(new NodeQuestion { Question = helloQuestions[i], SortOrder = i + 1 });

        intro.Nodes.Add(hello);
        // The other nodes have no questions yet. Their rewards are placeholders.
        intro.Nodes.Add(Node("Variables", "A named container for data.", NodeType.Lesson, 2, tVars, 10, 5));
        intro.Nodes.Add(Node("Data Types", "Structuring values with data types.", NodeType.Lesson, 3, tTypes, 10, 5));
        intro.Nodes.Add(Node("Java Basics 1", "Practice", NodeType.Practice, 4, null, 15, 8));
        intro.Nodes.Add(Node("Operators & Expressions", "Calculate and compare values.", NodeType.Lesson, 5, tOps, 10, 5));
        intro.Nodes.Add(Node("Java Basics 2", "Practice", NodeType.Practice, 6, null, 15, 8));
        intro.Nodes.Add(Node("Test", "Test your problem-solving skill here.", NodeType.Test, 7, null, 50, 25));

        var teaser = new LearnSection { Title = "Java Fundamentals", SortOrder = 2, IsPublished = false };

        db.LearnSections.AddRange(intro, teaser);
        await db.SaveChangesAsync();
    }
}}