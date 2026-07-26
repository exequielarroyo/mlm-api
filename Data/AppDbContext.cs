using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using mlm.Models;

namespace mlm.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<PayoutBatch> PayoutBatches => Set<PayoutBatch>();
    public DbSet<PayoutItem> PayoutItems => Set<PayoutItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(user =>
        {
            user.Property(u => u.ReferralCode).HasMaxLength(16);
            user.HasIndex(u => u.ReferralCode).IsUnique();
        });

        builder.Entity<Product>(product =>
        {
            product.Property(p => p.Name).HasMaxLength(200);
            product.Property(p => p.Price).HasPrecision(18, 2);
            product.Property(p => p.DiscountPercent).HasPrecision(5, 2);
        });
        builder.Entity<Order>(order =>
        {
            order.Property(o => o.ProductSubtotal).HasPrecision(18, 2);
            order.HasIndex(o => new { o.BuyerId, o.Status });
            order.HasMany(o => o.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<OrderLine>(line =>
        {
            line.Property(l => l.ProductName).HasMaxLength(200);
            line.Property(l => l.UnitPrice).HasPrecision(18, 2);
            line.Property(l => l.LineSubtotal).HasPrecision(18, 2);
        });
        builder.Entity<Commission>(commission =>
        {
            commission.Property(c => c.Rate).HasPrecision(5, 4);
            commission.Property(c => c.CommissionableAmount).HasPrecision(18, 2);
            commission.Property(c => c.Amount).HasPrecision(18, 2);
            commission.HasIndex(c => new { c.OrderId, c.RecipientId, c.Level }).IsUnique();
            commission.HasIndex(c => new { c.RecipientId, c.Status });
        });
        builder.Entity<PayoutBatch>(payout => payout.Property(p => p.Amount).HasPrecision(18, 2));
        builder.Entity<PayoutItem>(item => item.HasKey(i => new { i.PayoutBatchId, i.CommissionId }));
    }
}
