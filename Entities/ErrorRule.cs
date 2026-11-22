using System.ComponentModel.DataAnnotations;

namespace JavaMaster.Api.Entities;

/// <summary>One row of the friendly-error table. Edit rows in SSMS to change messages without redeploying.</summary>
public class ErrorRule
{
    public int Id { get; set; }

    /// <summary>Regex tested against the normalized Java error (see FriendlyErrorService).</summary>
    [MaxLength(300)] public string Pattern { get; set; } = "";

    /// <summary>Friendly message. {1}, {2} are replaced by the regex groups.</summary>
    [MaxLength(300)] public string Message { get; set; } = "";

    [MaxLength(300)] public string Help { get; set; } = "";

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}