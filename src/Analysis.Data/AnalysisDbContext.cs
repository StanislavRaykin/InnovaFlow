using Microsoft.EntityFrameworkCore;

namespace InnovaFlow.Analysis.Data;

/// <summary>
/// No query filter anywhere in this context. Access is decided by the Projects
/// service before an analysis is ever requested, and the Worker runs with no
/// user at all - filtering here would only give a false sense of enforcement.
/// Analysis.Api must check membership through Projects before returning results.
/// </summary>
public class AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : DbContext(options)
{
    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<AnalysisScore> Scores => Set<AnalysisScore>();

    public DbSet<MarketAnalysis> MarketAnalyses => Set<MarketAnalysis>();
    public DbSet<Competitor> Competitors => Set<Competitor>();
    public DbSet<AudienceAnalysis> AudienceAnalyses => Set<AudienceAnalysis>();
    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<RiskAnalysis> RiskAnalyses => Set<RiskAnalysis>();
    public DbSet<Risk> Risks => Set<Risk>();
    public DbSet<OpportunityAnalysis> OpportunityAnalyses => Set<OpportunityAnalysis>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FeasibilityAnalysis> FeasibilityAnalyses => Set<FeasibilityAnalysis>();
    public DbSet<BusinessAnalysis> BusinessAnalyses => Set<BusinessAnalysis>();
    public DbSet<BrandAnalysis> BrandAnalyses => Set<BrandAnalysis>();

    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<AIRequest> Requests => Set<AIRequest>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("Analysis");

        // -------------------------------------------------------------------
        // Job
        // -------------------------------------------------------------------

        b.Entity<Analysis>(e =>
        {
            e.ToTable("Analyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.OverallScore).HasPrecision(5, 2);

            e.HasIndex(x => x.IdeaId);
            e.HasIndex(x => x.IdeaVersionId);

            // The dashboard asks for the newest analysis of an idea constantly.
            e.HasIndex(x => new { x.IdeaId, x.StartedAt })
             .HasDatabaseName("IX_Analyses_IdeaId_StartedAt");
        });

        b.Entity<AnalysisScore>(e =>
        {
            e.ToTable("AnalysisScores");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.ScoreType).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Score).HasPrecision(5, 2);

            // One score per dimension per run - a rerun replaces, never appends.
            e.HasIndex(x => new { x.AnalysisId, x.ScoreType }).IsUnique();

            e.HasOne(x => x.Analysis)
             .WithMany(a => a.Scores)
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------------
        // Single-row node results
        //
        // Each has a unique AnalysisId: one result per node per run, which is
        // what stops a rerun from duplicating them.
        // -------------------------------------------------------------------

        b.Entity<MarketAnalysis>(e =>
        {
            e.ToTable("MarketAnalyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.MarketSize).HasMaxLength(200);
            e.Property(x => x.MarketTrend).HasMaxLength(200);
            e.Property(x => x.MarketOpportunity).HasPrecision(5, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.AnalysisId).IsUnique();

            e.HasOne(x => x.Analysis).WithMany()
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AudienceAnalysis>(e =>
        {
            e.ToTable("AudienceAnalyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.HasIndex(x => x.AnalysisId).IsUnique();

            e.HasOne(x => x.Analysis).WithMany()
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Personas)
             .WithOne(p => p.AudienceAnalysis)
             .HasForeignKey(p => p.AudienceAnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RiskAnalysis>(e =>
        {
            e.ToTable("RiskAnalyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.RiskScore).HasPrecision(5, 2);

            e.HasIndex(x => x.AnalysisId).IsUnique();

            e.HasOne(x => x.Analysis).WithMany()
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Risks)
             .WithOne(r => r.RiskAnalysis)
             .HasForeignKey(r => r.RiskAnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<OpportunityAnalysis>(e =>
        {
            e.ToTable("OpportunityAnalyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.OpportunityScore).HasPrecision(5, 2);

            e.HasIndex(x => x.AnalysisId).IsUnique();

            e.HasOne(x => x.Analysis).WithMany()
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Opportunities)
             .WithOne(o => o.OpportunityAnalysis)
             .HasForeignKey(o => o.OpportunityAnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<FeasibilityAnalysis>(e =>
        {
            e.ToTable("FeasibilityAnalyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.TechnicalScore).HasPrecision(5, 2);
            e.Property(x => x.ResourceScore).HasPrecision(5, 2);
            e.Property(x => x.ComplexityScore).HasPrecision(5, 2);

            e.HasIndex(x => x.AnalysisId).IsUnique();

            e.HasOne(x => x.Analysis).WithMany()
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BusinessAnalysis>(e =>
        {
            e.ToTable("BusinessAnalyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.BusinessModel).HasMaxLength(150);
            e.Property(x => x.RevenueModel).HasMaxLength(150);
            e.Property(x => x.RevenuePotential).HasPrecision(5, 2);

            e.HasIndex(x => x.AnalysisId).IsUnique();

            e.HasOne(x => x.Analysis).WithMany()
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BrandAnalysis>(e =>
        {
            e.ToTable("BrandAnalyses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Positioning).HasMaxLength(500);
            e.Property(x => x.CommunicationStyle).HasMaxLength(500);

            e.Property(x => x.CoreValues).HasColumnType("jsonb");
            e.Property(x => x.NameSuggestions).HasColumnType("jsonb");
            e.Property(x => x.Slogans).HasColumnType("jsonb");

            e.HasIndex(x => x.AnalysisId).IsUnique();

            e.HasOne(x => x.Analysis).WithMany()
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------------
        // Multi-row node results
        //
        // No unique constraint is possible here, so the Worker must delete by
        // parent id before inserting when a node reruns.
        // -------------------------------------------------------------------

        b.Entity<Competitor>(e =>
        {
            e.ToTable("Competitors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.WebsiteUrl).HasMaxLength(500);
            e.Property(x => x.Industry).HasMaxLength(100);
            e.Property(x => x.PricingModel).HasMaxLength(150);
            e.Property(x => x.SimilarityScore).HasPrecision(5, 2);

            e.Property(x => x.Strengths).HasColumnType("jsonb");
            e.Property(x => x.Weaknesses).HasColumnType("jsonb");

            e.HasIndex(x => x.AnalysisId);

            e.HasOne(x => x.Analysis).WithMany()
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Persona>(e =>
        {
            e.ToTable("Personas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.AgeRange).HasMaxLength(50);
            e.Property(x => x.Occupation).HasMaxLength(150);

            e.Property(x => x.Goals).HasColumnType("jsonb");
            e.Property(x => x.PainPoints).HasColumnType("jsonb");
            e.Property(x => x.Behaviors).HasColumnType("jsonb");

            e.HasIndex(x => x.AudienceAnalysisId);
        });

        b.Entity<Risk>(e =>
        {
            e.ToTable("Risks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Probability).HasPrecision(5, 2);
            e.Property(x => x.Impact).HasPrecision(5, 2);

            e.HasIndex(x => x.RiskAnalysisId);

            // The dashboard surfaces the single worst risk.
            e.HasIndex(x => new { x.RiskAnalysisId, x.Severity });
        });

        b.Entity<Opportunity>(e =>
        {
            e.ToTable("Opportunities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.PotentialImpact).HasMaxLength(100);
            e.Property(x => x.Difficulty).HasMaxLength(100);

            e.HasIndex(x => x.OpportunityAnalysisId);
        });

        b.Entity<Recommendation>(e =>
        {
            e.ToTable("Recommendations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Category).HasMaxLength(100);

            e.HasIndex(x => x.AnalysisId);

            e.HasOne(x => x.Analysis)
             .WithMany(a => a.Recommendations)
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Source>(e =>
        {
            e.ToTable("Sources");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Url).HasMaxLength(1000);
            e.Property(x => x.SourceType).HasMaxLength(50);
            e.Property(x => x.RelevanceScore).HasPrecision(5, 2);
            e.Property(x => x.RetrievedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.AnalysisId);

            e.HasOne(x => x.Analysis)
             .WithMany(a => a.Sources)
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------------------------------------------------------
        // Provider call log
        // -------------------------------------------------------------------

        b.Entity<AIRequest>(e =>
        {
            e.ToTable("AIRequests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.AgentType).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.Model).HasMaxLength(100);
            e.Property(x => x.StartedAt).HasDefaultValueSql("now()");

            e.HasIndex(x => x.AnalysisId);

            e.HasOne(x => x.Analysis)
             .WithMany(a => a.Requests)
             .HasForeignKey(x => x.AnalysisId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
