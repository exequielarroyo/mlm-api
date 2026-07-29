namespace mlm.Models;

public enum OrderStatus { PendingPayment, Completed, Refunded }
public enum CommissionStatus { Available, Paid, Reversed, RecoveryRequired }
public enum PayoutStatus { Draft, Paid }

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal EffectivePrice => DiscountPercent is > 0 ? Math.Round(Price * (1 - DiscountPercent.Value / 100m), 2) : Price;
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BuyerId { get; set; }
    public AppUser? Buyer { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal ProductSubtotal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
    public string? PaymentReference { get; set; }
}

public class OrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineSubtotal { get; set; }
}

public class Commission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid RecipientId { get; set; }
    public Guid BuyerId { get; set; }
    public int Level { get; set; }
    public decimal Rate { get; set; }
    public decimal CommissionableAmount { get; set; }
    public decimal Amount { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Available;
    public Guid? ReversalOfId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}

public class PayoutBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipientId { get; set; }
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public List<PayoutItem> Items { get; set; } = [];
}

public class PayoutItem
{
    public Guid PayoutBatchId { get; set; }
    public PayoutBatch? PayoutBatch { get; set; }
    public Guid CommissionId { get; set; }
    public Commission? Commission { get; set; }
}

public record ProductRequest(string Name, string? ImageUrl, decimal Price, decimal? DiscountPercent);
public record OrderLineRequest(Guid ProductId, int Quantity);
public record CreateOrderRequest(IReadOnlyList<OrderLineRequest> Lines);
public record PayOrderRequest(string Reference);
public record OrderLineDto(Guid Id, Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineSubtotal);
public record OrderDto(Guid Id, Guid BuyerId, string? BuyerName, OrderStatus Status, decimal ProductSubtotal, string? PaymentReference, DateTime CreatedAt, DateTime? CompletedAt, DateTime? RefundedAt, IReadOnlyList<OrderLineDto> Lines);
public record CommissionSummaryDto(decimal Available, decimal Paid, decimal Reversed, decimal RecoveryRequired);
public record CommissionDto(Guid Id, Guid OrderId, string BuyerName, int Level, decimal Rate, decimal CommissionableAmount, decimal Amount, CommissionStatus Status, DateTime CreatedAt, DateTime? PaidAt);
public record PayoutDto(Guid Id, decimal Amount, PayoutStatus Status, DateTime CreatedAt, DateTime? PaidAt);
public record CreatePayoutRequest(Guid RecipientId, IReadOnlyList<Guid>? CommissionIds);

public static class CommerceMappingExtensions
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id,
        order.BuyerId,
        $"{order.Buyer?.FirstName} {order.Buyer?.LastName}".Trim(),
        order.Status,
        order.ProductSubtotal,
        order.PaymentReference,
        order.CreatedAt,
        order.CompletedAt,
        order.RefundedAt,
        order.Lines.Select(line => new OrderLineDto(line.Id, line.ProductId, line.ProductName, line.UnitPrice, line.Quantity, line.LineSubtotal)).ToList());
}
