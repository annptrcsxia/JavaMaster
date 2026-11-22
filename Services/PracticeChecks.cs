using System.Text.RegularExpressions;

namespace JavaMaster.Api.Services;

/// <summary>The answer rules for the 5 practice items. Check returns null when the answer is correct.</summary>
public static class PracticeChecks
{
    public const int ItemCount = 5;
    public static PracticeResponse? Check(int itemId, string code, string output) => itemId switch
    {
        1 => Expect("Hello, Java!", output),
        2 => Expect("Hello\nJava", output)
             ?? (Regex.Matches(code, @"System\.out\.print(ln)?\s*\(").Count >= 2 ? null
                 : Wrong("Your output is right, but use two println lines.", "One println for Hello and one for Java.")),
        3 => Expect("15", output)
             ?? Need(code, @"\bint\s+age\s*=\s*15\s*;", "The output is right, but create an int variable named age that stores 15.", "Write int age = 15; first.")
             ?? Need(code, @"System\.out\.println\s*\(\s*age\s*\)", "Print the variable age, not the number.", "Use System.out.println(age);"),
        4 => CheckString(code, output),
        5 => Expect("3.14", output)
             ?? Need(code, @"\bdouble\s+pi\s*=\s*3\.14\s*;", "The output is right, but store 3.14 in a double variable named pi.", "Write double pi = 3.14;")
             ?? Need(code, @"System\.out\.println\s*\(\s*pi\s*\)", "Print the variable pi, not the number.", "Use System.out.println(pi);"),
        _ => Wrong("Unknown practice item.", ""),
    };

    private static PracticeResponse? CheckString(string code, string output)
    {
        var m = Regex.Match(code, "\\bString\\s+name\\s*=\\s*\"([^\"\\n]+)\"\\s*;");
        if (!m.Success) return Wrong("Create a String variable named name with your name inside quotes.", "For example: String name = \"Alex\";");
        return Expect(m.Groups[1].Value, output)
               ?? Need(code, @"System\.out\.println\s*\(\s*name\s*\)", "Print the variable name, not the text.", "Use System.out.println(name);");
    }

    private static PracticeResponse? Expect(string expected, string output)
    {
        if (output == expected) return null;
        if (output.Length == 0)
            return PracticeResponse.Fail("wrong_output", "Your code ran but printed nothing.", "Use System.out.println to show the text.");
        var shown = output.Length > 40 ? output[..40] + "…" : output;
        return PracticeResponse.Fail("wrong_output", $"Your code printed \"{shown}\" but we expected \"{expected}\".",
            "Check the spelling, capital letters and punctuation.", output: output);
    }

    private static PracticeResponse? Need(string code, string pattern, string message, string help) =>
        Regex.IsMatch(code, pattern) ? null : Wrong(message, help);

    private static PracticeResponse Wrong(string message, string help) => PracticeResponse.Fail("wrong_code", message, help);
}