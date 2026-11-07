using JavaMaster.Api.Services;
using System.Text.Json;
using JavaMaster.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Data;

public static class DbSeeder
{
    private static string Json(object o) => JsonSerializer.Serialize(o);

    public static async Task SeedAsync(AppDbContext db)
    {
        // 1. Ranks
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

        // 2. Shop items
        if (!await db.ShopItems.AnyAsync())
        {
            db.ShopItems.AddRange(
                new ShopItem { Code = "HINT", Name = "Hints",
                    Description = "Removes one wrong option. Decks only.",
                    Price5Pack = 30, Price10Pack = 55 },
                new ShopItem { Code = "HEAL_POTION", Name = "Heal potions",
                    Description = "Restores your HP to full. Decks only.",
                    Price5Pack = 50, Price10Pack = 90 },
                new ShopItem { Code = "STREAK_REPAIR", Name = "Streak repairs",
                    Description = "Brings back your streak after one missed day.",
                    Price5Pack = 150, Price10Pack = 275 });
            await db.SaveChangesAsync();
        }

        // 3. Starting items for any user who has none yet (covers the existing test user)
        var usersWithoutKit = await db.Users
            .Where(u => !db.UserInventory.Any(i => i.UserId == u.Id)).ToListAsync();
        foreach (var u in usersWithoutKit)
            await StartingKit.GiveAsync(db, u);

        // 4. Daily challenge pool
        //    Insert only. The code puts these in the database once and then leaves
        //    them alone, so a professor can edit or retire a daily question without
        //    the next start putting the old wording back.
        var dailyDefs = new[]
        {
            (Code: "daily.q1",
             Prompt: "What data type is used to store a single 16-bit Unicode character?",
             Options: new[] { "String", "char", "int", "byte" }, Correct: 1,
             Explanation: "The char data type stores a single 16-bit Unicode character. Unlike String, which stores many characters, char holds exactly one."),
            (Code: "daily.q2",
             Prompt: "What is the default value of a boolean variable in a class?",
             Options: new[] { "true", "false", "null", "0" }, Correct: 1,
             Explanation: "The default value of a boolean declared in a class is false. It is assigned automatically if you give no initial value."),
            (Code: "daily.q3",
             Prompt: "What package must be imported to use the Scanner class?",
             Options: new[] { "java.io.*", "java.input.*", "java.scanner.*", "java.util.*" }, Correct: 3,
             Explanation: "Scanner is part of the java.util package. Import java.util.* or java.util.Scanner to use it."),
            (Code: "daily.q4",
             Prompt: "Which control statement is used to execute one block from many choices?",
             Options: new[] { "if", "switch", "loop", "select" }, Correct: 1,
             Explanation: "The switch statement runs one block of code out of many choices, based on the value of a variable."),
            (Code: "daily.q5",
             Prompt: "How do you create an array of integers with 5 elements?",
             Options: new[] { "int arr[] = new int[5];", "int arr = new int(5);", "int[5] arr;", "array int[5];" }, Correct: 0,
             Explanation: "In Java you create an array with int arr[] = new int[5]; or int[] arr = new int[5];. This allocates memory for 5 integers.")
        };

        // Questions seeded before codes existed are adopted rather than duplicated.
        foreach (var def in dailyDefs)
        {
            var legacy = await db.Questions.FirstOrDefaultAsync(q =>
                q.Purpose == QuestionPurpose.Daily && q.Code == null && q.Prompt == def.Prompt);
            if (legacy != null) legacy.Code = def.Code;
        }
        await db.SaveChangesAsync();

        var dailyTopic = await db.Topics.FirstOrDefaultAsync(t => t.Title == "Daily Challenge")
                         ?? new Topic { Title = "Daily Challenge" };

        foreach (var def in dailyDefs)
        {
            if (await db.Questions.AnyAsync(q => q.Code == def.Code)) continue;

            db.Questions.Add(new Question
            {
                Code = def.Code,
                Topic = dailyTopic, Type = QuestionType.MultipleChoice, Purpose = QuestionPurpose.Daily,
                Prompt = def.Prompt,
                Payload = Json(new { options = def.Options.Select((t, i) => new { id = ((char)('a' + i)).ToString(), text = t }).ToArray() }),
                AnswerKey = Json(new { correctOptionId = ((char)('a' + def.Correct)).ToString(), correctMessage = "Great job!" }),
                Explanation = def.Explanation
            });
        }
        await db.SaveChangesAsync();

        // 5. Learn path (runs once)
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
        //
        // The questions themselves are NOT created here. They live in
        // Data/LessonContent.cs, which is the single source of truth for the
        // lesson content, and it seeds them a moment later. Keeping a second
        // copy of them here meant a fresh install took the wording and the
        // answer keys from this file and never saw the ones in LessonContent.

        var intro = new LearnSection { Title = "Introduction to Java", SortOrder = 1, IsPublished = true };

        LearnNode Node(string title, string sub, NodeType type, int order, Topic? topic, int xp, int coins) =>
            new LearnNode { Title = title, Subtitle = sub, NodeType = type, SortOrder = order,
                            Topic = topic, XpReward = xp, CoinReward = coins };

        // Hello World: rewards match your prototype (120 XP, +30 coins)
        var hello = Node("Hello World", "Your journey starts here.", NodeType.Lesson, 1, tHello, 120, 30);
        hello.IntroText = "Welcome to the first chapter of the Java course! 🚀\n\nJava is a popular programming language designed by James Gosling to be \"written once, run anywhere\" with a Java Virtual Machine (JVM).";
        hello.OutroText = "Now that you can print a line of text, move on to the next lesson to start giving your code a place to keep its values.";

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
}
