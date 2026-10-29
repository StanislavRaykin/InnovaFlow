using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleCounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedModules",
                schema: "Analysis",
                table: "Analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalModules",
                schema: "Analysis",
                table: "Analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "Analysis",
                table: "Analyses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedModules",
                schema: "Analysis",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "TotalModules",
                schema: "Analysis",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "Analysis",
                table: "Analyses");
        }
    }
}
