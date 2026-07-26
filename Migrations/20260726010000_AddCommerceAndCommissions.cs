using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mlm.Migrations;

public partial class AddCommerceAndCommissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "Products", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            ImageUrl = table.Column<string>(type: "text", nullable: true),
            Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
            IsArchived = table.Column<bool>(type: "boolean", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Products", x => x.Id));

        migrationBuilder.CreateTable(name: "Orders", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            ProductSubtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => { table.PrimaryKey("PK_Orders", x => x.Id); table.ForeignKey("FK_Orders_AspNetUsers_BuyerId", x => x.BuyerId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade); });

        migrationBuilder.CreateTable(name: "Commissions", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), OrderId = table.Column<Guid>(type: "uuid", nullable: false), RecipientId = table.Column<Guid>(type: "uuid", nullable: false), BuyerId = table.Column<Guid>(type: "uuid", nullable: false), Level = table.Column<int>(type: "integer", nullable: false), Rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false), CommissionableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false), Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false), Status = table.Column<int>(type: "integer", nullable: false), ReversalOfId = table.Column<Guid>(type: "uuid", nullable: true), CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("PK_Commissions", x => x.Id));

        migrationBuilder.CreateTable(name: "OrderLines", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), OrderId = table.Column<Guid>(type: "uuid", nullable: false), ProductId = table.Column<Guid>(type: "uuid", nullable: false), ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false), UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false), Quantity = table.Column<int>(type: "integer", nullable: false), LineSubtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
        }, constraints: table => { table.PrimaryKey("PK_OrderLines", x => x.Id); table.ForeignKey("FK_OrderLines_Orders_OrderId", x => x.OrderId, "Orders", "Id", onDelete: ReferentialAction.Cascade); });

        migrationBuilder.CreateTable(name: "PayoutBatches", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), RecipientId = table.Column<Guid>(type: "uuid", nullable: false), Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false), Status = table.Column<int>(type: "integer", nullable: false), CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false), PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("PK_PayoutBatches", x => x.Id));

        migrationBuilder.CreateTable(name: "PayoutItems", columns: table => new { PayoutBatchId = table.Column<Guid>(type: "uuid", nullable: false), CommissionId = table.Column<Guid>(type: "uuid", nullable: false) }, constraints: table => { table.PrimaryKey("PK_PayoutItems", x => new { x.PayoutBatchId, x.CommissionId }); table.ForeignKey("FK_PayoutItems_PayoutBatches_PayoutBatchId", x => x.PayoutBatchId, "PayoutBatches", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex(name: "IX_Orders_BuyerId_Status", table: "Orders", columns: new[] { "BuyerId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_OrderLines_OrderId", table: "OrderLines", column: "OrderId");
        migrationBuilder.CreateIndex(name: "IX_Commissions_OrderId_RecipientId_Level", table: "Commissions", columns: new[] { "OrderId", "RecipientId", "Level" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_Commissions_RecipientId_Status", table: "Commissions", columns: new[] { "RecipientId", "Status" });
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrderLines"); migrationBuilder.DropTable(name: "PayoutItems"); migrationBuilder.DropTable(name: "Commissions"); migrationBuilder.DropTable(name: "Orders"); migrationBuilder.DropTable(name: "Products"); migrationBuilder.DropTable(name: "PayoutBatches");
    }
}
