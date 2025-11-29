using CarPartsShop.API.Models;
using CarPartsShop.API.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarPartsShop.API.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Part> Parts => Set<Part>();
        public DbSet<Customer> Customers => Set<Customer>();   
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
        public DbSet<AdminLog> AdminLogs => Set<AdminLog>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IMPORTANT: keep Identity configuration
            base.OnModelCreating(modelBuilder);

            // Category
            modelBuilder.Entity<Category>(e =>
            {
                e.Property(x => x.Name).IsRequired().HasMaxLength(80);
                e.HasIndex(x => x.Name).IsUnique();
            });

            // Part
            modelBuilder.Entity<Part>(e =>
            {
                e.Property(x => x.Name).IsRequired().HasMaxLength(120);
                e.Property(x => x.Sku).IsRequired().HasMaxLength(64);
                e.HasIndex(x => x.Sku).IsUnique();

                e.Property(x => x.ImageUrl).HasMaxLength(1024);
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");

                e.HasOne(p => p.Category)
                    .WithMany(c => c.Parts)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Order
            modelBuilder.Entity<Order>(e =>
            {
                e.Property(x => x.Status)
                    .HasConversion<string>()           
                    .HasMaxLength(32);                 

                e.Property(x => x.Total).HasColumnType("decimal(18,2)");

                e.HasMany(o => o.Items)
                    .WithOne(i => i.Order!)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(o => o.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // OrderItem
            modelBuilder.Entity<OrderItem>(e =>
            {
                e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");

                e.HasOne(i => i.Part)
                    .WithMany()
                    .HasForeignKey(i => i.PartId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.AppUser)
                .WithOne() 
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderStatusHistory

            modelBuilder.Entity<OrderStatusHistory>(e =>
            {
                e.Property(x => x.Status).IsRequired().HasMaxLength(32);
                e.Property(x => x.ChangedAt).IsRequired();

                e.HasOne(h => h.Order)
                 .WithMany(o => o.StatusHistory)
                 .HasForeignKey(h => h.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
