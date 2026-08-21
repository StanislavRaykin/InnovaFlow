using Microsoft.EntityFrameworkCore;

namespace InnovaFlow.Analysis.Data;

public class AnalysisDbContext(
    DbContextOptions<AnalysisDbContext> options,
    ICurrentUser currentUser) : DbContext(options)
{
    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<AIRequest> AIRequests => Set<AIRequest>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("analysis");

        // -------------------------------------------------------------------
        // Analysis
        // -------------------------------------------------------------------
        b.Entity<Analysis>(e =>
        {
            e.ToTable("Analyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.State).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.AuthorizedUserIds).HasColumnType("uuid[]");
            e.Property(x => x.OverallScore).HasPrecision(5, 2);
            e.Property(x => x.IdempotencyKey).HasMaxLength(128);

            e.HasIndex(x => x.IdeaId);
            e.HasIndex(x => x.IdempotencyKey)
             .IsUnique()
             .HasFilter("idempotency_key IS NOT NULL");

            // Containment lookups (authorized_user_ids @> ARRAY[:userId])
            e.HasIndex(x => x.AuthorizedUserIds).HasMethod("gin");

            // Soft delete + authorization, enforced at the context level so a
            // forgotten .Where() in a handler cannot leak another user's job.
            e.HasQueryFilter(x =>
                x.DeletedAt == null &&
                (currentUser.IsSystem ||
                 (currentUser.Id != null && x.AuthorizedUserIds.Contains(currentUser.Id.Value))));

            e.HasMany(x => x.Nodes)
             .WithOne(n => n.Analysis)
             .HasForeignKey(n => n.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------------
        // AIRequest
        // -------------------------------------------------------------------
        b.Entity<AIRequest>(e =>
        {
            e.ToTable("AIRequests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.NodeKey).HasMaxLength(64).IsRequired();
            e.Property(x => x.State).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.DependsOn).HasColumnType("text[]");
            e.Property(x => x.InputHash).HasMaxLength(64);
            e.Property(x => x.ResultJson).HasColumnType("jsonb");
            e.Property(x => x.Model).HasMaxLength(64);
            e.Property(x => x.SkippedBecause).HasMaxLength(64);

            // xmin - optimistic concurrency. Two workers cannot both take a node
            // out of Ready; the loser gets a DbUpdateConcurrencyException.
            e.Property(x => x.Version).IsRowVersion();

            // Database-level idempotency: a redelivered creation message
            // physically cannot insert a duplicate node.
            e.HasIndex(x => new { x.AnalysisId, x.NodeKey }).IsUnique();

            // Partial index - only rows the dispatcher actually scans.
            e.HasIndex(x => x.State)
             .HasFilter("state = 'Ready'");

            // Partial index - only rows the lease janitor scans.
            e.HasIndex(x => x.LeaseExpiresAt)
             .HasFilter("state = 'Running'");

            e.HasIndex(x => x.InputHash);

            // Inherit the parent's visibility rules.
            e.HasQueryFilter(x =>
                x.Analysis.DeletedAt == null &&
                (currentUser.IsSystem ||
                 (currentUser.Id != null &&
                  x.Analysis.AuthorizedUserIds.Contains(currentUser.Id.Value))));
        });

        // -------------------------------------------------------------------
        // Outbox
        // -------------------------------------------------------------------
        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("Outbox");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityAlwaysColumn();
            e.Property(x => x.MessageType).HasMaxLength(128).IsRequired();
            e.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();

            // The dispatcher's only query: unpublished, oldest first.
            e.HasIndex(x => x.Id).HasFilter("published_at IS NULL");
        });

        b.Entity<ProcessedMessage>(e =>
        {
            e.ToTable("ProcessedMessages");
            e.HasKey(x => new { x.MessageId, x.Consumer });
            e.Property(x => x.Consumer).HasMaxLength(64);
        });

        // snake_case everything so hand-written SQL and EF agree.
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties())
                prop.SetColumnName(ToSnakeCase(prop.Name));
    }

    private static string ToSnakeCase(string name) =>
        string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c)
                                     : char.ToLowerInvariant(c).ToString()));
}
