using System.Text.Json;
using JavaMaster.Api.Entities;

namespace JavaMaster.Api.Services;

public static class AnswerChecker
{
    public static bool IsCorrect(Question q, JsonElement answer)
    {
        try
        {
            using var doc = JsonDocument.Parse(q.AnswerKey);
            var key = doc.RootElement;
            return q.Type switch
            {
                QuestionType.MultipleChoice => Choice(key, answer),
                QuestionType.FillBlank      => Text(key, answer),
                QuestionType.ArrangeTokens  => Order(key, answer),
                QuestionType.FindBug        => Line(key, answer),
                QuestionType.FillSlots      => Slots(key, answer),
                _ => false
            };
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException
                                      or KeyNotFoundException or FormatException)
        {
            return false;   // a wrongly shaped answer is simply wrong
        }
    }

    private static bool Choice(JsonElement key, JsonElement a) =>
        a.GetProperty("optionId").GetString() == key.GetProperty("correctOptionId").GetString();

    private static bool Text(JsonElement key, JsonElement a)
    {
        var text = a.GetProperty("text").GetString()?.Trim() ?? "";
        var exact = key.TryGetProperty("caseSensitive", out var cs) && cs.GetBoolean();
        var cmp = exact ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return key.GetProperty("accepted").EnumerateArray()
                  .Any(x => string.Equals(x.GetString()?.Trim(), text, cmp));
    }

    private static bool Order(JsonElement key, JsonElement a)
    {
        var given = a.GetProperty("order").EnumerateArray().Select(x => x.GetString()).ToList();
        var expected = key.GetProperty("order").EnumerateArray().Select(x => x.GetString()).ToList();
        return given.SequenceEqual(expected);
    }

    private static bool Line(JsonElement key, JsonElement a) =>
        a.GetProperty("lineNumber").GetInt32() == key.GetProperty("lineNumber").GetInt32();

    private static bool Slots(JsonElement key, JsonElement a)
    {
        var given = a.GetProperty("slots");
        foreach (var slot in key.GetProperty("slots").EnumerateObject())
            if (!given.TryGetProperty(slot.Name, out var v) || v.GetString() != slot.Value.GetString())
                return false;
        return true;
    }
}