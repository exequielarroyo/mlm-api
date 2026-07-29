using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mlm.Migrations
{
    /// <inheritdoc />
    public partial class ConnectBinaryPairsToPayouts : Migration
    {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PayoutItems_Commissions_CommissionId",
            table: "PayoutItems");

        migrationBuilder.DropPrimaryKey(
            name: "PK_PayoutItems",
            table: "PayoutItems");

        migrationBuilder.AlterColumn<Guid>(
            name: "CommissionId",
            table: "PayoutItems",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        // Add Id as nullable first, seed unique GUIDs per row, then non-null.
        migrationBuilder.AddColumn<Guid>(
            name: "Id",
            table: "PayoutItems",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("UPDATE \"PayoutItems\" SET \"Id\" = gen_random_uuid()");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "PayoutItems",
            type: "uuid",
            nullable: false);

        migrationBuilder.AddColumn<Guid>(
            name: "BinaryPairId",
            table: "PayoutItems",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PaidAt",
            table: "BinaryPairs",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_PayoutItems",
            table: "PayoutItems",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_PayoutItems_BinaryPairId",
            table: "PayoutItems",
            column: "BinaryPairId");

        migrationBuilder.CreateIndex(
            name: "IX_PayoutItems_PayoutBatchId",
            table: "PayoutItems",
            column: "PayoutBatchId");

        migrationBuilder.AddForeignKey(
            name: "FK_PayoutItems_BinaryPairs_BinaryPairId",
            table: "PayoutItems",
            column: "BinaryPairId",
            principalTable: "BinaryPairs",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PayoutItems_Commissions_CommissionId",
            table: "PayoutItems",
            column: "CommissionId",
            principalTable: "Commissions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayoutItems_BinaryPairs_BinaryPairId",
                table: "PayoutItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PayoutItems_Commissions_CommissionId",
                table: "PayoutItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PayoutItems",
                table: "PayoutItems");

            migrationBuilder.DropIndex(
                name: "IX_PayoutItems_BinaryPairId",
                table: "PayoutItems");

            migrationBuilder.DropIndex(
                name: "IX_PayoutItems_PayoutBatchId",
                table: "PayoutItems");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PayoutItems");

            migrationBuilder.DropColumn(
                name: "BinaryPairId",
                table: "PayoutItems");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "BinaryPairs");

            migrationBuilder.AlterColumn<Guid>(
                name: "CommissionId",
                table: "PayoutItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayoutItems",
                table: "PayoutItems",
                columns: new[] { "PayoutBatchId", "CommissionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PayoutItems_Commissions_CommissionId",
                table: "PayoutItems",
                column: "CommissionId",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
