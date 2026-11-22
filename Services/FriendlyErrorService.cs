using System.Text.RegularExpressions;
using JavaMaster.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JavaMaster.Api.Entities;

namespace JavaMaster.Api.Services;

/// <summary>
/// Turns a raw javac error into a friendly one using the ErrorRules table.
/// Step 1: read the first "Practice.java:3: error: ..." line (and the "symbol:" line below it).
/// Step 2: build a "key" such as  cannot find symbol [class string] [did-you-mean String].
/// Step 3: the first active rule whose Pattern matches the key gives the Message and Help.
/// </summary>
public class FriendlyErrorService
{
    private static readonly string[] KnownNames = { "String", "System", "Integer", "Double", "Math", "Scanner" };
    private static readonly Regex ErrorLine = new(@"^Practice\.java:(\d+): error: (.+)$", RegexOptions.Compiled);
    private static readonly Regex SymbolLine = new(@"^\s*symbol:\s+(variable|class|method)\s+([A-Za-z_]\w*)", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public FriendlyErrorService(AppDbContext db, IMemoryCache cache) { _db = db; _cache = cache; }

    public async Task<PracticeResponse> FromCompileErrorAsync(string stderr, string code)
    {
        var lines = stderr.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        for (var i = 0; i < lines.Length; i++)
        {
            var m = ErrorLine.Match(lines[i]);
            if (!m.Success) continue;

            var line = int.Parse(m.Groups[1].Value);
            var raw = m.Groups[2].Value;
            (string Kind, string Name)? symbol = null;
            for (var j = i + 1; j < Math.Min(i + 6, lines.Length) && !lines[j].StartsWith("Practice.java:"); j++)
            {
                var s = SymbolLine.Match(lines[j]);
                if (s.Success) { symbol = (s.Groups[1].Value, s.Groups[2].Value); break; }
            }

            var key = BuildKey(raw, symbol, code);
            foreach (var rule in await GetRulesAsync())
            {
                Match rm;
                try { rm = Regex.Match(key, rule.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200)); }
                catch (ArgumentException) { continue; }          // bad pattern typed in the table: skip it
                catch (RegexMatchTimeoutException) { continue; }
                if (rm.Success) return PracticeResponse.Fail("compile_error", Fill(rule.Message, rm), Fill(rule.Help, rm), line);
            }
            return PracticeResponse.Fail("compile_error", $"Something isn't right on line {line}.",
                "Check this line for typos, missing symbols and capital letters.", line);
        }
        return PracticeResponse.Fail("compile_error", "Java couldn't read your code.", "Check your brackets and quotation marks.");
    }

    public static PracticeResponse FromRuntimeError(string stderr) =>
        Regex.IsMatch(stderr, "Could not find or load main class|Main method not found")
            ? PracticeResponse.Fail("runtime_error", "Java can't find the Main class.", "Keep the class name Main and the main method as they were.")
            : PracticeResponse.Fail("runtime_error", "Your program stopped because of an error.", "Read your code again and check what each line does.");

    private static string BuildKey(string raw, (string Kind, string Name)? symbol, string code)
    {
        var key = raw.StartsWith("incompatible types:") ? Regex.Replace(raw, @"\bString\b", "text") : raw;
        string? name = null;
        if (symbol is { } s)
        {
            key += $" [{s.Kind} {s.Name}]";
            name = s.Name;
            if (s.Kind == "variable" && Regex.IsMatch(code, @"^\s*" + Regex.Escape(s.Name) + @"\s*=", RegexOptions.Multiline))
                key += " [missing-type]";
        }
        else
        {
            var pkg = Regex.Match(raw, @"^package (\w+) does not exist");
            if (pkg.Success) { name = pkg.Groups[1].Value; key += $" [class {name}]"; }
        }
        var fix = name is null ? null : KnownNames.FirstOrDefault(t => t.Equals(name, StringComparison.OrdinalIgnoreCase) && t != name);
        return fix is null ? key : key + $" [did-you-mean {fix}]";
    }

    private static string Fill(string template, Match m)
    {
        var result = template;
        for (var g = 1; g < m.Groups.Count; g++)
        {
            var v = m.Groups[g].Value;
            result = result.Replace("{" + g + "}", v);
        }
        return result;
    }

    private Task<List<ErrorRule>> GetRulesAsync() =>
        _cache.GetOrCreateAsync("error-rules", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);   // edits in SSMS show up within 10 minutes
            return await _db.ErrorRules.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.SortOrder).ToListAsync();
        })!;
}