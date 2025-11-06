using Core.Entities.CustomerAggregate;
using Core.Entities.Identity;
using Core.Entities.ProductAggregate;
using Core.Entities.PurchaseOrderAggregate;
using Core.Entities.SalesOrderAggregate;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class IntegrationDbContext : IdentityDbContext<User, Role, Guid>
    {
        public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderLine> SalesOrderLines { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Required for Identity tables (defines PKs for IdentityUserLogin, etc.)
            base.OnModelCreating(builder);

            // Relationships
            builder.Entity<PurchaseOrder>()
                .HasMany(p => p.PurchaseOrderLines)
                .WithOne(l => l.PurchaseOrder)
                .HasForeignKey(l => l.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SalesOrder>()
                .HasMany(p => p.SalesOrderLines)
                .WithOne(l => l.SalesOrder)
                .HasForeignKey(l => l.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal precision fix (see warnings below)
            builder.Entity<Product>(b =>
            {
                b.Property(p => p.Height).HasPrecision(18, 3);
                b.Property(p => p.Length).HasPrecision(18, 3);
                b.Property(p => p.Weight).HasPrecision(18, 3);
                b.Property(p => p.Width).HasPrecision(18, 3);
            });

            builder.Entity<PurchaseOrderLine>(b =>
            {
                b.Property(l => l.Quantity).HasPrecision(18, 3);
            });

            builder.Entity<SalesOrderLine>(b =>
            {
                b.Property(l => l.Quantity).HasPrecision(18, 3);
            });
        }
    }
}
