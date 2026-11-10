using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueKeyOnAnalysesScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalysisScores_AnalysisId_ScoreType",
                schema: "Analysis",
                table: "AnalysisScores");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisScores_AnalysisId",
                schema: "Analysis",
                table: "AnalysisScores",
                column: "AnalysisId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalysisScores_AnalysisId",
                schema: "Analysis",
                table: "AnalysisScores");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisScores_AnalysisId_ScoreType",
                schema: "Analysis",
                table: "AnalysisScores",
                columns: new[] { "AnalysisId", "ScoreType" },
                unique: true);
        }
    }
}
