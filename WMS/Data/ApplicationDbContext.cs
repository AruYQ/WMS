// Data/ApplicationDbContext.cs
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using WMS.Models;

namespace WMS.Data
{
    /// <summary>
    /// Database context untuk WMS Application
    /// Mengatur mapping entity ke database dan konfigurasi relationship
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets untuk semua entity
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
        public DbSet<AdvancedShippingNotice> AdvancedShippingNotices { get; set; }
        public DbSet<ASNDetail> ASNDetails { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }
        public DbSet<Inventory> Inventories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =============================================
            // SUPPLIER Configuration
            // =============================================
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.Email).IsUnique();

                // Properties configuration
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                entity.Property(e => e.Address)
                    .HasMaxLength(200);
            });

            // =============================================
            // CUSTOMER Configuration
            // =============================================
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.Email).IsUnique();

                // Properties configuration
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                entity.Property(e => e.Address)
                    .HasMaxLength(200);
            });

            // =============================================
            // ITEM Configuration
            // =============================================
            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("Items");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.ItemCode).IsUnique();

                // Properties configuration
                entity.Property(e => e.ItemCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.StandardPrice)
                    .HasColumnType("decimal(18,2)");
            });

            // =============================================
            // LOCATION Configuration
            // =============================================
            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.Code).IsUnique();

                // Properties configuration
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(200);
            });

            // =============================================
            // PURCHASE ORDER Configuration
            // =============================================
            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.ToTable("PurchaseOrders");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.PONumber).IsUnique();

                // Properties configuration
                entity.Property(e => e.PONumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                // Foreign Key Relationships
                entity.HasOne(e => e.Supplier)
                    .WithMany(s => s.PurchaseOrders)
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // PURCHASE ORDER DETAIL Configuration
            // =============================================
            modelBuilder.Entity<PurchaseOrderDetail>(entity =>
            {
                entity.ToTable("PurchaseOrderDetails");
                entity.HasKey(e => e.Id);

                // Properties configuration
                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.TotalPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Notes)
                    .HasMaxLength(200);

                // Foreign Key Relationships
                entity.HasOne(e => e.PurchaseOrder)
                    .WithMany(po => po.PurchaseOrderDetails)
                    .HasForeignKey(e => e.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Item)
                    .WithMany(i => i.PurchaseOrderDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite unique constraint untuk prevent duplicate item dalam satu PO
                entity.HasIndex(e => new { e.PurchaseOrderId, e.ItemId }).IsUnique();
            });

            // =============================================
            // ADVANCED SHIPPING NOTICE Configuration
            // =============================================
            modelBuilder.Entity<AdvancedShippingNotice>(entity =>
            {
                entity.ToTable("AdvancedShippingNotices");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.ASNNumber).IsUnique();

                // Properties configuration
                entity.Property(e => e.ASNNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.CarrierName)
                    .HasMaxLength(100);

                entity.Property(e => e.TrackingNumber)
                    .HasMaxLength(50);

                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                // Foreign Key Relationships
                entity.HasOne(e => e.PurchaseOrder)
                    .WithMany(po => po.AdvancedShippingNotices)
                    .HasForeignKey(e => e.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // ASN DETAIL Configuration
            // =============================================
            modelBuilder.Entity<ASNDetail>(entity =>
            {
                entity.ToTable("ASNDetails");
                entity.HasKey(e => e.Id);

                // Properties configuration
                entity.Property(e => e.ActualPricePerItem)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.WarehouseFeeRate)
                    .HasColumnType("decimal(5,4)");

                entity.Property(e => e.WarehouseFeeAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Notes)
                    .HasMaxLength(200);

                // Foreign Key Relationships
                entity.HasOne(e => e.ASN)
                    .WithMany(asn => asn.ASNDetails)
                    .HasForeignKey(e => e.ASNId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Item)
                    .WithMany(i => i.ASNDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite unique constraint untuk prevent duplicate item dalam satu ASN
                entity.HasIndex(e => new { e.ASNId, e.ItemId }).IsUnique();
            });

            // =============================================
            // SALES ORDER Configuration
            // =============================================
            modelBuilder.Entity<SalesOrder>(entity =>
            {
                entity.ToTable("SalesOrders");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.SONumber).IsUnique();

                // Properties configuration
                entity.Property(e => e.SONumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.TotalWarehouseFee)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                // Foreign Key Relationships
                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.SalesOrders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // SALES ORDER DETAIL Configuration
            // =============================================
            modelBuilder.Entity<SalesOrderDetail>(entity =>
            {
                entity.ToTable("SalesOrderDetails");
                entity.HasKey(e => e.Id);

                // Properties configuration
                entity.Property(e => e.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.TotalPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.WarehouseFeeApplied)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Notes)
                    .HasMaxLength(200);

                // Foreign Key Relationships
                entity.HasOne(e => e.SalesOrder)
                    .WithMany(so => so.SalesOrderDetails)
                    .HasForeignKey(e => e.SalesOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Item)
                    .WithMany(i => i.SalesOrderDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite unique constraint untuk prevent duplicate item dalam satu SO
                entity.HasIndex(e => new { e.SalesOrderId, e.ItemId }).IsUnique();
            });

            // =============================================
            // INVENTORY Configuration
            // =============================================
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("Inventories");
                entity.HasKey(e => e.Id);

                // Properties configuration
                entity.Property(e => e.LastCostPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Notes)
                    .HasMaxLength(200);

                // Foreign Key Relationships
                entity.HasOne(e => e.Item)
                    .WithMany(i => i.Inventories)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.Inventories)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite unique constraint untuk prevent duplicate item di lokasi yang sama
                entity.HasIndex(e => new { e.ItemId, e.LocationId }).IsUnique();
            });

            // =============================================
            // SEED DATA (Optional)
            // =============================================
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Method untuk seed data awal
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default locations
            modelBuilder.Entity<Location>().HasData(
                new Location { Id = 1, Code = "A-01-01", Name = "Area A Rak 1 Slot 1", MaxCapacity = 100, CreatedDate = DateTime.Now },
                new Location { Id = 2, Code = "A-01-02", Name = "Area A Rak 1 Slot 2", MaxCapacity = 100, CreatedDate = DateTime.Now },
                new Location { Id = 3, Code = "B-01-01", Name = "Area B Rak 1 Slot 1", MaxCapacity = 50, CreatedDate = DateTime.Now },
                new Location { Id = 4, Code = "RECEIVING", Name = "Receiving Area", MaxCapacity = 1000, CreatedDate = DateTime.Now },
                new Location { Id = 5, Code = "SHIPPING", Name = "Shipping Area", MaxCapacity = 1000, CreatedDate = DateTime.Now }
            );

            // Seed sample supplier
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier
                {
                    Id = 1,
                    Name = "PT Supplier Sample",
                    Email = "supplier@example.com",
                    Phone = "021-1234567",
                    Address = "Jakarta",
                    CreatedDate = DateTime.Now
                }
            );

            // Seed sample customer
            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    Id = 1,
                    Name = "PT Customer Sample",
                    Email = "customer@example.com",
                    Phone = "021-7654321",
                    Address = "Jakarta",
                    CreatedDate = DateTime.Now
                }
            );

            // Seed sample items
            modelBuilder.Entity<Item>().HasData(
                new Item
                {
                    Id = 1,
                    ItemCode = "ITM001",
                    Name = "Sample Item 1",
                    Unit = "pcs",
                    StandardPrice = 10000,
                    CreatedDate = DateTime.Now
                },
                new Item
                {
                    Id = 2,
                    ItemCode = "ITM002",
                    Name = "Sample Item 2",
                    Unit = "kg",
                    StandardPrice = 50000,
                    CreatedDate = DateTime.Now
                }
            );
        }

        /// <summary>
        /// Override SaveChanges untuk auto-update ModifiedDate
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync untuk auto-update ModifiedDate
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Update timestamps untuk audit trail
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.Now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.ModifiedDate = DateTime.Now;
                        break;
                }
            }
        }
    }
}