using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InnovaFlow.Identity.Data;

public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.HasDefaultSchema("Identity");

        // Framework defaults kept as-is: AspNetUsers, AspNetRoles,
        // AspNetUserRoles and the rest. They already match the ER diagram and
        // renaming them buys nothing.
        b.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        });

        b.Entity<Profile>(e =>
        {
            e.ToTable("Profiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Bio).HasMaxLength(2000);
            e.Property(x => x.AvatarUrl).HasMaxLength(500);

            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");

            // One profile per user.
            e.HasIndex(x => x.UserId).IsUnique();

            e.HasOne(x => x.User)
             .WithOne(u => u.Profile)
             .HasForeignKey<Profile>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
