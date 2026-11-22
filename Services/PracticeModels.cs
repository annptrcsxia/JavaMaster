namespace JavaMaster.Api.Services;

public record RunRequest(int ItemId, string Code, int NodeId = 4);

/// <summary>Status: success | compile_error | wrong_output | wrong_code | runtime_error | timeout | busy | bad_request</summary>
public record PracticeResponse(string Status, string Title, string Message, string Help, int? Line, string Output)
{
    public const string Almost = "Almost there!";
    public static PracticeResponse Success(string output) => new("success", "", "", "", null, output);
    public static PracticeResponse Fail(string status, string message, string help, int? line = null, string output = "") =>
        new(status, Almost, message, help, line, output);
}