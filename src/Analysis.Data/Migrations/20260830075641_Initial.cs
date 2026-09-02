using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Analysis");

            migrationBuilder.CreateTable(
                name: "Analyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdeaVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OverallScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIRequests",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PromptTokens = table.Column<int>(type: "integer", nullable: true),
                    CompletionTokens = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIRequests_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisScores",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoreType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisScores_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AudienceAnalyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryAudience = table.Column<string>(type: "text", nullable: true),
                    SecondaryAudience = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudienceAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudienceAnalyses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrandAnalyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Positioning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CommunicationStyle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VisualIdentity = table.Column<string>(type: "text", nullable: true),
                    CoreValues = table.Column<string>(type: "jsonb", nullable: false),
                    NameSuggestions = table.Column<string>(type: "jsonb", nullable: false),
                    Slogans = table.Column<string>(type: "jsonb", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandAnalyses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessAnalyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessModel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RevenueModel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PricingStrategy = table.Column<string>(type: "text", nullable: true),
                    CostAnalysis = table.Column<string>(type: "text", nullable: true),
                    RevenuePotential = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessAnalyses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Competitors",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PricingModel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Strengths = table.Column<string>(type: "jsonb", nullable: false),
                    Weaknesses = table.Column<string>(type: "jsonb", nullable: false),
                    SimilarityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Competitors_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeasibilityAnalyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ResourceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ComplexityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeasibilityAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeasibilityAnalyses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketAnalyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketSize = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MarketTrend = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MarketOpportunity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketAnalyses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityAnalyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityAnalyses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recommendations_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskAnalyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskAnalyses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RetrievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    RelevanceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sources_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Personas",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AudienceAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AgeRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Occupation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Goals = table.Column<string>(type: "jsonb", nullable: false),
                    PainPoints = table.Column<string>(type: "jsonb", nullable: false),
                    Behaviors = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personas_AudienceAnalyses_AudienceAnalysisId",
                        column: x => x.AudienceAnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "AudienceAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OpportunityAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PotentialImpact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Difficulty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Opportunities_OpportunityAnalyses_OpportunityAnalysisId",
                        column: x => x.OpportunityAnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "OpportunityAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Risks",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RiskAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Probability = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Impact = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Mitigation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Risks_RiskAnalyses_RiskAnalysisId",
                        column: x => x.RiskAnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "RiskAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIRequests_AnalysisId",
                schema: "Analysis",
                table: "AIRequests",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_IdeaId",
                schema: "Analysis",
                table: "Analyses",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_IdeaId_StartedAt",
                schema: "Analysis",
                table: "Analyses",
                columns: new[] { "IdeaId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_IdeaVersionId",
                schema: "Analysis",
                table: "Analyses",
                column: "IdeaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisScores_AnalysisId_ScoreType",
                schema: "Analysis",
                table: "AnalysisScores",
                columns: new[] { "AnalysisId", "ScoreType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AudienceAnalyses_AnalysisId",
                schema: "Analysis",
                table: "AudienceAnalyses",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrandAnalyses_AnalysisId",
                schema: "Analysis",
                table: "BrandAnalyses",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAnalyses_AnalysisId",
                schema: "Analysis",
                table: "BusinessAnalyses",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_AnalysisId",
                schema: "Analysis",
                table: "Competitors",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_FeasibilityAnalyses_AnalysisId",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketAnalyses_AnalysisId",
                schema: "Analysis",
                table: "MarketAnalyses",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_OpportunityAnalysisId",
                schema: "Analysis",
                table: "Opportunities",
                column: "OpportunityAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityAnalyses_AnalysisId",
                schema: "Analysis",
                table: "OpportunityAnalyses",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personas_AudienceAnalysisId",
                schema: "Analysis",
                table: "Personas",
                column: "AudienceAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_AnalysisId",
                schema: "Analysis",
                table: "Recommendations",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAnalyses_AnalysisId",
                schema: "Analysis",
                table: "RiskAnalyses",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskAnalysisId",
                schema: "Analysis",
                table: "Risks",
                column: "RiskAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskAnalysisId_Severity",
                schema: "Analysis",
                table: "Risks",
                columns: new[] { "RiskAnalysisId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_Sources_AnalysisId",
                schema: "Analysis",
                table: "Sources",
                column: "AnalysisId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRequests",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "AnalysisScores",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "BrandAnalyses",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "BusinessAnalyses",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "Competitors",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "FeasibilityAnalyses",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "MarketAnalyses",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "Opportunities",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "Personas",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "Recommendations",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "Risks",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "Sources",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "OpportunityAnalyses",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "AudienceAnalyses",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "RiskAnalyses",
                schema: "Analysis");

            migrationBuilder.DropTable(
                name: "Analyses",
                schema: "Analysis");
        }
    }
}
