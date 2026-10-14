namespace JavaMaster.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string MicrosoftObjectId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? StudentNumber { get; set; }
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Student;

    public int TotalXp { get; set; }
    public DateTime XpUpdatedAt { get; set; } = DateTime.UtcNow;
    public int Coins { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastActivityDate { get; set; }

    public bool MusicOn { get; set; } = true;
    public bool SfxOn { get; set; } = true;
    public bool NotifSoundOn { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}