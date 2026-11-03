using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitorSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Competitors_AnalysisId",
                schema: "Analysis",
                table: "Competitors");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "Analysis",
                table: "Competitors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_AnalysisId_Source",
                schema: "Analysis",
                table: "Competitors",
                columns: new[] { "AnalysisId", "Source" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Competitors_AnalysisId_Source",
                schema: "Analysis",
                table: "Competitors");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "Analysis",
                table: "Competitors");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_AnalysisId",
                schema: "Analysis",
                table: "Competitors",
                column: "AnalysisId");
        }
    }
}
