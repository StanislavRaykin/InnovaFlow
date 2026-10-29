using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                schema: "Analysis",
                table: "OutboxMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboxMessage_OutboxState_OutboxId",
                schema: "Analysis",
                table: "OutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_OutboxState_BusName_Created",
                schema: "Analysis",
                table: "OutboxState");

            migrationBuilder.DropColumn(
                name: "BusName",
                schema: "Analysis",
                table: "OutboxState");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                schema: "Analysis",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                schema: "Analysis",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                schema: "Analysis",
                table: "OutboxMessage",
                column: "ExpirationTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxState_Created",
                schema: "Analysis",
                table: "OutboxState");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                schema: "Analysis",
                table: "OutboxMessage");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                schema: "Analysis",
                table: "OutboxMessage");

            migrationBuilder.AddColumn<string>(
                name: "BusName",
                schema: "Analysis",
                table: "OutboxState",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_BusName_Created",
                schema: "Analysis",
                table: "OutboxState",
                columns: new[] { "BusName", "Created" });

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                schema: "Analysis",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId" },
                principalSchema: "Analysis",
                principalTable: "InboxState",
                principalColumns: new[] { "MessageId", "ConsumerId" });

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxMessage_OutboxState_OutboxId",
                schema: "Analysis",
                table: "OutboxMessage",
                column: "OutboxId",
                principalSchema: "Analysis",
                principalTable: "OutboxState",
                principalColumn: "OutboxId");
        }
    }
}
