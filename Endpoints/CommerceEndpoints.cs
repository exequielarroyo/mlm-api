using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mlm.Data;
using mlm.Models;

namespace mlm.Endpoints;

public static class CommerceEndpoints
{
    private static readonly decimal[] CommissionRates = [0.10m, 0.05m, 0.03m, 0.02m, 0.01m];
    private static readonly AuthorizeAttribute AdminOnly = new() { Roles = "Admin" };

    public static IEndpointRouteBuilder MapCommerceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products", async (AppDbContext db) =>
            await db.Products.Where(p => !p.IsArchived).OrderBy(p => p.Name).ToListAsync())
            .WithTags("Products");
        app.MapGet("/products/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsArchived);
            return product is null ? Results.NotFound() : Results.Ok(product);
        }).WithTags("Products");

        var products = app.MapGroup("/admin/products").RequireAuthorization(AdminOnly).WithTags("Admin Products");
        products.MapGet("/", async (AppDbContext db) => await db.Products.OrderBy(p => p.Name).ToListAsync());
        products.MapPost("/", async (ProductRequest request, AppDbContext db) =>
        {
            if (!ValidProduct(request, out var error)) return Results.BadRequest(error);
            var product = new Product { Name = request.Name.Trim(), ImageUrl = request.ImageUrl?.Trim(), Price = request.Price, DiscountPercent = request.DiscountPercent };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/products/{product.Id}", product);
        });
        products.MapPut("/{id:guid}", async (Guid id, ProductRequest request, AppDbContext db) =>
        {
            if (!ValidProduct(request, out var error)) return Results.BadRequest(error);
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();
            product.Name = request.Name.Trim(); product.ImageUrl = request.ImageUrl?.Trim(); product.Price = request.Price; product.DiscountPercent = request.DiscountPercent;
            await db.SaveChangesAsync();
            return Results.Ok(product);
        });
        products.MapPost("/{id:guid}/archive", async (Guid id, AppDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();
            product.IsArchived = true;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapPost("/orders", async (CreateOrderRequest request, ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db) =>
        {
            var buyer = await users.GetUserAsync(principal);
            if (buyer is null) return Results.NotFound();
            if (request.Lines is null || request.Lines.Count == 0 || request.Lines.Any(l => l.Quantity is < 1 or > 999))
                return Results.BadRequest("Provide at least one product with a quantity from 1 to 999.");

            var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
            if (productIds.Count != request.Lines.Count) return Results.BadRequest("Duplicate products must be combined into one line.");
            var productMap = await db.Products.Where(p => productIds.Contains(p.Id) && !p.IsArchived).ToDictionaryAsync(p => p.Id);
            if (productMap.Count != productIds.Count) return Results.BadRequest("One or more products are unavailable.");

            var order = new Order { BuyerId = buyer.Id };
            foreach (var requestLine in request.Lines)
            {
                var product = productMap[requestLine.ProductId];
                var unitPrice = product.EffectivePrice;
                order.Lines.Add(new OrderLine { ProductId = product.Id, ProductName = product.Name, UnitPrice = unitPrice, Quantity = requestLine.Quantity, LineSubtotal = unitPrice * requestLine.Quantity });
            }
            order.ProductSubtotal = order.Lines.Sum(l => l.LineSubtotal);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return Results.Created($"/orders/{order.Id}", order.ToDto());
        }).RequireAuthorization().WithTags("Orders");

        app.MapGet("/orders", async (ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null) return Results.NotFound();
            var orders = await db.Orders.Where(o => o.BuyerId == me.Id).Include(o => o.Lines).Include(o => o.Buyer).OrderByDescending(o => o.CreatedAt).ToListAsync();
            return Results.Ok(orders.Select(o => o.ToDto()));
        }).RequireAuthorization().WithTags("Orders");
        app.MapGet("/orders/{id:guid}", async (Guid id, ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null) return Results.NotFound();
            var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id && o.BuyerId == me.Id);
            return order is null ? Results.NotFound() : Results.Ok(order.ToDto());
        }).RequireAuthorization().WithTags("Orders");

        app.MapPost("/orders/{id:guid}/pay", async (Guid id, PayOrderRequest request, ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null) return Results.NotFound();
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.BuyerId == me.Id);
            if (order is null) return Results.NotFound();
            if (order.Status != OrderStatus.PendingPayment) return Results.BadRequest("Only pending orders can be paid.");
            if (string.IsNullOrWhiteSpace(request.Reference)) return Results.BadRequest("Payment reference is required.");

            order.PaymentReference = request.Reference.Trim();
            await db.SaveChangesAsync();
            return Results.Ok(order.ToDto());
        }).RequireAuthorization().WithTags("Orders");

        var adminOrders = app.MapGroup("/admin/orders").RequireAuthorization(AdminOnly).WithTags("Admin Orders");
        adminOrders.MapGet("/", async (AppDbContext db) =>
        {
            var orders = await db.Orders.Include(o => o.Lines).Include(o => o.Buyer).OrderByDescending(o => o.CreatedAt).ToListAsync();
            return orders.Select(o => o.ToDto());
        });
        adminOrders.MapPost("/{id:guid}/complete", async (Guid id, AppDbContext db) => await CompleteOrder(id, db));
        adminOrders.MapPost("/{id:guid}/refund", async (Guid id, AppDbContext db) => await RefundOrder(id, db));
        app.MapGet("/admin/commissions", async (AppDbContext db) => await db.Commissions.OrderByDescending(c => c.CreatedAt).ToListAsync())
            .RequireAuthorization(AdminOnly).WithTags("Admin Commissions");

        app.MapGet("/me/earnings", async (ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null) return Results.NotFound();
            var rows = await db.Commissions.Where(c => c.RecipientId == me.Id).GroupBy(c => c.Status).Select(g => new { Status = g.Key, Amount = g.Sum(x => x.Amount) }).ToListAsync();
            decimal Sum(CommissionStatus status) => rows.Where(r => r.Status == status).Sum(r => r.Amount);
            return Results.Ok(new CommissionSummaryDto(Sum(CommissionStatus.Available), Sum(CommissionStatus.Paid), Sum(CommissionStatus.Reversed), Sum(CommissionStatus.RecoveryRequired)));
        }).RequireAuthorization().WithTags("Commissions");

        app.MapGet("/me/commissions", async (int? page, int? pageSize, ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db) =>
        {
            var me = await users.GetUserAsync(principal);
            if (me is null) return Results.NotFound();
            var take = Math.Clamp(pageSize ?? 25, 1, 100); var skip = (Math.Max(page ?? 1, 1) - 1) * take;
            var query = from c in db.Commissions join buyer in db.Users on c.BuyerId equals buyer.Id where c.RecipientId == me.Id orderby c.CreatedAt descending select new CommissionDto(c.Id, c.OrderId, buyer.FirstName + " " + buyer.LastName, c.Level, c.Rate, c.CommissionableAmount, c.Amount, c.Status, c.CreatedAt, c.PaidAt);
            return Results.Ok(await query.Skip(skip).Take(take).ToListAsync());
        }).RequireAuthorization().WithTags("Commissions");

        app.MapGet("/me/payouts", async (ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db) =>
        {
            var me = await users.GetUserAsync(principal);
            return me is null ? Results.NotFound() : Results.Ok(await db.PayoutBatches.Where(p => p.RecipientId == me.Id).OrderByDescending(p => p.CreatedAt).Select(p => new PayoutDto(p.Id, p.Amount, p.Status, p.CreatedAt, p.PaidAt)).ToListAsync());
        }).RequireAuthorization().WithTags("Commissions");

        var payouts = app.MapGroup("/admin/payouts").RequireAuthorization(AdminOnly).WithTags("Admin Payouts");
        payouts.MapGet("/", async (AppDbContext db) => await db.PayoutBatches.OrderByDescending(p => p.CreatedAt).Select(p => new PayoutDto(p.Id, p.Amount, p.Status, p.CreatedAt, p.PaidAt)).ToListAsync());
        payouts.MapPost("/", async (CreatePayoutRequest request, AppDbContext db) =>
        {
            var eligible = db.Commissions.Where(c => c.RecipientId == request.RecipientId && c.Status == CommissionStatus.Available);
            if (request.CommissionIds is { Count: > 0 }) eligible = eligible.Where(c => request.CommissionIds.Contains(c.Id));
            var commissions = await eligible.ToListAsync();
            if (commissions.Count == 0) return Results.BadRequest("No available commissions were selected.");
            var payout = new PayoutBatch { RecipientId = request.RecipientId, Amount = commissions.Sum(c => c.Amount) };
            payout.Items = commissions.Select(c => new PayoutItem { CommissionId = c.Id }).ToList();
            db.PayoutBatches.Add(payout); await db.SaveChangesAsync(); return Results.Created($"/admin/payouts/{payout.Id}", new PayoutDto(payout.Id, payout.Amount, payout.Status, payout.CreatedAt, payout.PaidAt));
        });
        payouts.MapPost("/{id:guid}/paid", async (Guid id, AppDbContext db) =>
        {
            var payout = await db.PayoutBatches.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
            if (payout is null) return Results.NotFound();
            if (payout.Status == PayoutStatus.Paid) return Results.Ok(new PayoutDto(payout.Id, payout.Amount, payout.Status, payout.CreatedAt, payout.PaidAt));
            var now = DateTime.UtcNow; var commissionIds = payout.Items.Select(i => i.CommissionId).ToList();
            var commissions = await db.Commissions.Where(c => commissionIds.Contains(c.Id) && c.Status == CommissionStatus.Available).ToListAsync();
            foreach (var commission in commissions) { commission.Status = CommissionStatus.Paid; commission.PaidAt = now; }
            payout.Status = PayoutStatus.Paid; payout.PaidAt = now; await db.SaveChangesAsync();
            return Results.Ok(new PayoutDto(payout.Id, payout.Amount, payout.Status, payout.CreatedAt, payout.PaidAt));
        });
        return app;
    }

    private static bool ValidProduct(ProductRequest request, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200) error = "Product name is required and must be at most 200 characters.";
        else if (request.Price < 0) error = "Price cannot be negative.";
        else if (request.DiscountPercent is < 0 or > 100) error = "Discount must be between 0 and 100.";
        return error.Length == 0;
    }

    private static async Task<IResult> CompleteOrder(Guid id, AppDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return Results.NotFound();
        if (order.Status == OrderStatus.Refunded) return Results.BadRequest("A refunded order cannot be completed.");
        if (order.Status == OrderStatus.Completed) return Results.Ok(order.ToDto());
        var buyer = await db.Users.FindAsync(order.BuyerId);
        if (buyer is null) return Results.BadRequest("Order buyer was not found.");

        // Bulk fetch all ancestors
        var ancestors = await db.Users.FromSqlRaw(@$"
            WITH RECURSIVE AncestorTree AS (
                SELECT * FROM ""AspNetUsers"" WHERE ""Id"" = {{0}}
                UNION ALL
                SELECT u.* FROM ""AspNetUsers"" u
                INNER JOIN AncestorTree a ON u.""Id"" = a.""SponsorId""
            )
            SELECT * FROM AncestorTree
        ", buyer.SponsorId).ToListAsync();

        var level = 0;
        foreach (var sponsor in ancestors)
        {
            if (level >= CommissionRates.Length) break;
            level++;
            db.Commissions.Add(new Commission
            {
                OrderId = order.Id,
                BuyerId = buyer.Id,
                RecipientId = sponsor.Id,
                Level = level,
                Rate = CommissionRates[level - 1],
                CommissionableAmount = order.ProductSubtotal,
                Amount = Math.Round(order.ProductSubtotal * CommissionRates[level - 1], 2)
            });
        }

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch (DbUpdateException) { await transaction.RollbackAsync(); return Results.Conflict("Commissions have already been created for this order."); }
        await db.Entry(order).Collection(o => o.Lines).LoadAsync();
        return Results.Ok(order.ToDto());
    }

    private static async Task<IResult> RefundOrder(Guid id, AppDbContext db)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null) return Results.NotFound();
        if (order.Status == OrderStatus.Refunded)
        {
        await db.Entry(order).Collection(o => o.Lines).LoadAsync();
        await db.Entry(order).Reference(o => o.Buyer).LoadAsync();
            return Results.Ok(order.ToDto());
        }
        if (order.Status != OrderStatus.Completed) return Results.BadRequest("Only completed orders can be refunded.");
        var commissions = await db.Commissions.Where(c => c.OrderId == id).ToListAsync();
        foreach (var commission in commissions)
            commission.Status = commission.Status == CommissionStatus.Paid ? CommissionStatus.RecoveryRequired : CommissionStatus.Reversed;
        order.Status = OrderStatus.Refunded; order.RefundedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await db.Entry(order).Collection(o => o.Lines).LoadAsync();
        return Results.Ok(order.ToDto());
    }
}
