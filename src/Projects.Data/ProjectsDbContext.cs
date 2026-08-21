using Microsoft.EntityFrameworkCore;

namespace InnovaFlow.Projects.Data;

public class ProjectsDbContext(
    DbContextOptions<ProjectsDbContext> options,
    ICurrentUser currentUser) : DbContext(options)
{
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<IdeaMember> IdeaMembers => Set<IdeaMember>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<IdeaFile> Files => Set<IdeaFile>();
    public DbSet<IdeaStats> Stats => Set<IdeaStats>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("projects");

        b.Entity<Idea>(e =>
        {
            e.ToTable("Ideas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            e.HasIndex(x => x.OwnerId);
            e.HasIndex(x => x.TeamId);

            e.HasOne(x => x.Team)
             .WithMany(t => t.Ideas)
             .HasForeignKey(x => x.TeamId)
             .OnDelete(DeleteBehavior.SetNull);

            // Access = direct idea membership OR membership of the owning team.
            // Two independent grant paths, one filter, one place.
            e.HasQueryFilter(x =>
                x.DeletedAt == null &&
                (currentUser.IsSystem ||
                 (currentUser.Id != null &&
                  (x.Members.Any(m => m.UserId == currentUser.Id.Value) ||
                   (x.TeamId != null &&
                    x.Team!.Members.Any(tm => tm.UserId == currentUser.Id.Value))))));
        });

        b.Entity<IdeaMember>(e =>
        {
            e.ToTable("IdeaMembers");

            // Composite key - the same user cannot appear twice on one idea
            // with conflicting roles.
            e.HasKey(x => new { x.IdeaId, x.UserId });
            e.Property(x => x.Role).HasConversion<int>();

            e.HasIndex(x => x.UserId);   // "list ideas shared with me"

            e.HasOne(x => x.Idea)
             .WithMany(i => i.Members)
             .HasForeignKey(x => x.IdeaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Team>(e =>
        {
            e.ToTable("Teams");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.OwnerId);
        });

        b.Entity<TeamMember>(e =>
        {
            e.ToTable("TeamMembers");
            e.HasKey(x => new { x.TeamId, x.UserId });
            e.Property(x => x.Role).HasConversion<int>();
            e.HasIndex(x => x.UserId);

            e.HasOne(x => x.Team)
             .WithMany(t => t.Members)
             .HasForeignKey(x => x.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<IdeaFile>(e =>
        {
            e.ToTable("Files");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.StoragePath).HasMaxLength(512).IsRequired();

            e.HasOne(x => x.Idea)
             .WithMany(i => i.Files)
             .HasForeignKey(x => x.IdeaId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => x.DeletedAt == null && x.Idea.DeletedAt == null);
        });

        b.Entity<IdeaStats>(e =>
        {
            e.ToTable("IdeaStats");
            e.HasKey(x => x.IdeaId);
            e.Property(x => x.LatestOverallScore).HasPrecision(5, 2);
        });

        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties())
                prop.SetColumnName(ToSnakeCase(prop.Name));
    }

    private static string ToSnakeCase(string name) =>
        string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c)
                                     : char.ToLowerInvariant(c).ToString()));
}
