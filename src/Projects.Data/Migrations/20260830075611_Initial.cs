using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projects.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Projects");

            migrationBuilder.CreateTable(
                name: "IdeaStats",
                schema: "Projects",
                columns: table => new
                {
                    IdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAnalyses = table.Column<int>(type: "integer", nullable: false),
                    CompletedAnalyses = table.Column<int>(type: "integer", nullable: false),
                    FailedAnalyses = table.Column<int>(type: "integer", nullable: false),
                    LatestOverallScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    LastAnalysedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaStats", x => x.IdeaId);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ideas",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ProblemStatement = table.Column<string>(type: "text", nullable: true),
                    TargetAudience = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ideas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ideas_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Projects",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                schema: "Projects",
                columns: table => new
                {
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => new { x.TeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "Projects",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIConversations",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIConversations_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalSchema: "Projects",
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Files_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalSchema: "Projects",
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdeaMembers",
                schema: "Projects",
                columns: table => new
                {
                    IdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaMembers", x => new { x.IdeaId, x.UserId });
                    table.ForeignKey(
                        name: "FK_IdeaMembers_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalSchema: "Projects",
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Strategies",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StrategyType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    ProposedByAnalysisId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Strategies_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalSchema: "Projects",
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIMessages",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIMessages_AIConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "Projects",
                        principalTable: "AIConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IdeaVersions",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    IdeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    VariantName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DerivedFromStrategyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ProblemStatement = table.Column<string>(type: "text", nullable: true),
                    TargetAudience = table.Column<string>(type: "text", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdeaVersions_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalSchema: "Projects",
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdeaVersions_Strategies_DerivedFromStrategyId",
                        column: x => x.DerivedFromStrategyId,
                        principalSchema: "Projects",
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StrategyMetrics",
                schema: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyMetrics_Strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalSchema: "Projects",
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_IdeaId",
                schema: "Projects",
                table: "AIConversations",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_IdeaId_CreatedAt",
                schema: "Projects",
                table: "AIConversations",
                columns: new[] { "IdeaId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIMessages_ConversationId_CreatedAt",
                schema: "Projects",
                table: "AIMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Files_IdeaId",
                schema: "Projects",
                table: "Files",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaMembers_UserId",
                schema: "Projects",
                table: "IdeaMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_OwnerId",
                schema: "Projects",
                table: "Ideas",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_TeamId",
                schema: "Projects",
                table: "Ideas",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaVersions_DerivedFromStrategyId",
                schema: "Projects",
                table: "IdeaVersions",
                column: "DerivedFromStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaVersions_IdeaId_VersionNumber",
                schema: "Projects",
                table: "IdeaVersions",
                columns: new[] { "IdeaId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Strategies_OneSelectedPerIdea",
                schema: "Projects",
                table: "Strategies",
                column: "IdeaId",
                unique: true,
                filter: "\"IsSelected\"");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyMetrics_StrategyId_MetricName",
                schema: "Projects",
                table: "StrategyMetrics",
                columns: new[] { "StrategyId", "MetricName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                schema: "Projects",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OwnerId",
                schema: "Projects",
                table: "Teams",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIMessages",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "Files",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "IdeaMembers",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "IdeaStats",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "IdeaVersions",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "StrategyMetrics",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "TeamMembers",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "AIConversations",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "Strategies",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "Ideas",
                schema: "Projects");

            migrationBuilder.DropTable(
                name: "Teams",
                schema: "Projects");
        }
    }
}
