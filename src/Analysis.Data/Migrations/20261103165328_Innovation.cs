using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class Innovation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InnovationAnalyses",
                schema: "Analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    OriginalityExplanation = table.Column<string>(type: "text", nullable: false),
                    TechnologicalInnovationScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TechnologicalInnovationExplanation = table.Column<string>(type: "text", nullable: false),
                    BusinessInnovationScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    BusinessInnovationExplanation = table.Column<string>(type: "text", nullable: false),
                    GrowthPotentialScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    GrowthPotentialExplanation = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnovationAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InnovationAnalyses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalSchema: "Analysis",
                        principalTable: "Analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InnovationAnalyses_AnalysisId",
                schema: "Analysis",
                table: "InnovationAnalyses",
                column: "AnalysisId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InnovationAnalyses",
                schema: "Analysis");
        }
    }
}
