using Microsoft.EntityFrameworkCore;

namespace InnovaFlow.Notifier.Data;

public class NotifierDbContext(DbContextOptions<NotifierDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("Notifier");

        b.Entity<Notification>(e =>
        {
            e.ToTable("Notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasMaxLength(1000);
            e.Property(x => x.SubjectType).HasMaxLength(50);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            // The bell icon asks one question: my unread, newest first.
            // The column name is quoted because PostgreSQL folds unquoted
            // identifiers to lower case.
            e.HasIndex(x => new { x.UserId, x.CreatedAt })
             .HasFilter("NOT \"IsRead\"")
             .HasDatabaseName("IX_Notifications_Unread");

            e.HasIndex(x => new { x.UserId, x.CreatedAt })
             .HasDatabaseName("IX_Notifications_UserId_CreatedAt");

            // Idempotent consumption - RabbitMQ guarantees at-least-once.
            e.HasIndex(x => x.SourceMessageId).IsUnique();
        });
    }
}
