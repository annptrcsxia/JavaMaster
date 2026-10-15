using JavaMaster.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace JavaMaster.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<LearnSection> LearnSections => Set<LearnSection>();
    public DbSet<LearnNode> LearnNodes => Set<LearnNode>();
    public DbSet<NodeQuestion> NodeQuestions => Set<NodeQuestion>();
    public DbSet<UserNodeProgress> UserNodeProgress => Set<UserNodeProgress>();
    public DbSet<Rank> Ranks => Set<Rank>();
    public DbSet<QuestionAttempt> QuestionAttempts => Set<QuestionAttempt>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.MicrosoftObjectId).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.MicrosoftObjectId).HasMaxLength(100);
        });

        b.Entity<Question>(e =>
        {
            e.Property(q => q.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(q => q.Purpose).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<LearnNode>(e =>
        {
            e.Property(n => n.NodeType).HasConversion<string>().HasMaxLength(20);
        });

        // Composite keys: no separate Id column needed
        b.Entity<NodeQuestion>(e =>
        {
            e.HasKey(x => new { x.LearnNodeId, x.QuestionId });
        });

        b.Entity<UserNodeProgress>(e =>
        {
            e.HasKey(x => new { x.UserId, x.LearnNodeId });
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
    }
}