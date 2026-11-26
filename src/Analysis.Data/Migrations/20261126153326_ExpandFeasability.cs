using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFeasability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TechnicalScore",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ResourceScore",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ComplexityScore",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Challenges",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Resources",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScalabilityScore",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Technologies",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Challenges",
                schema: "Analysis",
                table: "FeasibilityAnalyses");

            migrationBuilder.DropColumn(
                name: "Resources",
                schema: "Analysis",
                table: "FeasibilityAnalyses");

            migrationBuilder.DropColumn(
                name: "ScalabilityScore",
                schema: "Analysis",
                table: "FeasibilityAnalyses");

            migrationBuilder.DropColumn(
                name: "Technologies",
                schema: "Analysis",
                table: "FeasibilityAnalyses");

            migrationBuilder.AlterColumn<decimal>(
                name: "TechnicalScore",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ResourceScore",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ComplexityScore",
                schema: "Analysis",
                table: "FeasibilityAnalyses",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
