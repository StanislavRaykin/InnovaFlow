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
    public DbSet<IdeaVersion> IdeaVersions => Set<IdeaVersion>();
    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<StrategyMetric> StrategyMetrics => Set<StrategyMetric>();
    public DbSet<AIConversation> Conversations => Set<AIConversation>();
    public DbSet<AIMessage> Messages => Set<AIMessage>();
    public DbSet<IdeaStats> Stats => Set<IdeaStats>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("Projects");

        // -------------------------------------------------------------------
        // Ideas and access
        // -------------------------------------------------------------------

        b.Entity<Idea>(e =>
        {
            e.ToTable("Ideas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Industry).HasMaxLength(100);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");

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

            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.JoinedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.UserId);

            e.HasOne(x => x.Idea)
             .WithMany(i => i.Members)
             .HasForeignKey(x => x.IdeaId)
             .OnDelete(DeleteBehavior.Cascade);

            // Deliberately no query filter. The filter on Idea would recurse
            // through Members, and the membership rows themselves must stay
            // readable when resolving a user's effective role.
        });

        b.Entity<Team>(e =>
        {
            e.ToTable("Teams");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.OwnerId);
        });

        b.Entity<TeamMember>(e =>
        {
            e.ToTable("TeamMembers");
            e.HasKey(x => new { x.TeamId, x.UserId });

            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.JoinedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.UserId);

            e.HasOne(x => x.Team)
             .WithMany(t => t.Members)
             .HasForeignKey(x => x.TeamId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------------
        // Files
        // -------------------------------------------------------------------

        b.Entity<IdeaFile>(e =>
        {
            e.ToTable("Files");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
            e.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
            e.Property(x => x.UploadedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.IdeaId);

            e.HasOne(x => x.Idea)
             .WithMany(i => i.Files)
             .HasForeignKey(x => x.IdeaId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => x.Idea.DeletedAt == null);
        });

        // -------------------------------------------------------------------
        // Versions and strategies
        // -------------------------------------------------------------------

        b.Entity<IdeaVersion>(e =>
        {
            e.ToTable("IdeaVersions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.VariantName).HasMaxLength(100);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            // Version numbers are per idea and never reused.
            e.HasIndex(x => new { x.IdeaId, x.VersionNumber }).IsUnique();

            e.HasOne(x => x.Idea)
             .WithMany(i => i.Versions)
             .HasForeignKey(x => x.IdeaId)
             .OnDelete(DeleteBehavior.Cascade);

            // A strategy can spawn several variants; deleting it must not take
            // the resulting versions with it.
            e.HasOne(x => x.DerivedFromStrategy)
             .WithMany()
             .HasForeignKey(x => x.DerivedFromStrategyId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasQueryFilter(x => x.Idea.DeletedAt == null);
        });

        b.Entity<Strategy>(e =>
        {
            e.ToTable("Strategies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.StrategyType).HasMaxLength(60);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.IdeaId);

            // At most one selected strategy per idea, as a database guarantee.
            // The column name is quoted because PostgreSQL folds unquoted
            // identifiers to lower case.
            e.HasIndex(x => x.IdeaId)
             .IsUnique()
             .HasFilter("\"IsSelected\"")
             .HasDatabaseName("UX_Strategies_OneSelectedPerIdea");

            e.HasOne(x => x.Idea)
             .WithMany(i => i.Strategies)
             .HasForeignKey(x => x.IdeaId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Metrics)
             .WithOne(m => m.Strategy)
             .HasForeignKey(m => m.StrategyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => x.Idea.DeletedAt == null);
        });

        b.Entity<StrategyMetric>(e =>
        {
            e.ToTable("StrategyMetrics");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.MetricName).HasMaxLength(60).IsRequired();
            e.Property(x => x.Score).HasPrecision(5, 2);

            // One row per metric per strategy - a re-proposal replaces,
            // never appends.
            e.HasIndex(x => new { x.StrategyId, x.MetricName }).IsUnique();

            e.HasQueryFilter(x => x.Strategy.Idea.DeletedAt == null);
        });

        // -------------------------------------------------------------------
        // AI chat
        // -------------------------------------------------------------------

        b.Entity<AIConversation>(e =>
        {
            e.ToTable("AIConversations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.IdeaId);
            e.HasIndex(x => new { x.IdeaId, x.CreatedAt });

            e.HasOne(x => x.Idea)
             .WithMany(i => i.Conversations)
             .HasForeignKey(x => x.IdeaId)
             .OnDelete(DeleteBehavior.Cascade);

            // Visible to every member of the idea, not just the author.
            e.HasQueryFilter(x => x.Idea.DeletedAt == null);
        });

        b.Entity<AIMessage>(e =>
        {
            e.ToTable("AIMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Content).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            // Replaying a thread reads it in order.
            e.HasIndex(x => new { x.ConversationId, x.CreatedAt });

            e.HasOne(x => x.Conversation)
             .WithMany(c => c.Messages)
             .HasForeignKey(x => x.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => x.Conversation.Idea.DeletedAt == null);
        });

        // -------------------------------------------------------------------
        // Read model
        // -------------------------------------------------------------------

        b.Entity<IdeaStats>(e =>
        {
            e.ToTable("IdeaStats");
            e.HasKey(x => x.IdeaId);
            e.Property(x => x.LatestOverallScore).HasPrecision(5, 2);
        });
    }
}
