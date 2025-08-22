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

        // DbSets untuk Auth entities
        public DbSet<Company> Companies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // DbSets untuk WMS entities
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
            // AUTHENTICATION ENTITIES Configuration
            // =============================================

            // Company Configuration
            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("Companies");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(300);
                entity.Property(e => e.ContactPerson).HasMaxLength(100);
                entity.Property(e => e.TaxNumber).HasMaxLength(20);
                entity.Property(e => e.SubscriptionPlan).HasMaxLength(20);
            });

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.PasswordSalt).IsRequired();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);

                // Foreign Key to Company
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Users)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Role Configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.Permissions).IsRequired();
            });

            // UserRole Configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(e => e.Id);

                // Composite unique constraint
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

                entity.Property(e => e.AssignedBy).HasMaxLength(50);

                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // SUPPLIER Configuration
            // =============================================
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique();

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Suppliers)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // CUSTOMER Configuration
            // =============================================
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique();

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Customers)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // ITEM Configuration
            // =============================================
            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("Items");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.ItemCode }).IsUnique();

                entity.Property(e => e.ItemCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(10);
                entity.Property(e => e.StandardPrice).HasColumnType("decimal(18,2)");

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Items)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // LOCATION Configuration
            // =============================================
            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();

                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(200);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Locations)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =============================================
            // PURCHASE ORDER Configuration
            // =============================================
            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.ToTable("PurchaseOrders");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.PONumber }).IsUnique();

                entity.Property(e => e.PONumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.PurchaseOrders)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Supplier relationship
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

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(200);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // PO relationship
                entity.HasOne(e => e.PurchaseOrder)
                    .WithMany(po => po.PurchaseOrderDetails)
                    .HasForeignKey(e => e.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Item relationship
                entity.HasOne(e => e.Item)
                    .WithMany(i => i.PurchaseOrderDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite unique constraint
                entity.HasIndex(e => new { e.PurchaseOrderId, e.ItemId }).IsUnique();
            });

            // =============================================
            // ADVANCED SHIPPING NOTICE Configuration
            // =============================================
            modelBuilder.Entity<AdvancedShippingNotice>(entity =>
            {
                entity.ToTable("AdvancedShippingNotices");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.ASNNumber }).IsUnique();

                entity.Property(e => e.ASNNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CarrierName).HasMaxLength(100);
                entity.Property(e => e.TrackingNumber).HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.AdvancedShippingNotices)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // PO relationship
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

                entity.Property(e => e.ActualPricePerItem).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WarehouseFeeRate).HasColumnType("decimal(5,4)");
                entity.Property(e => e.WarehouseFeeAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(200);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ASN relationship
                entity.HasOne(e => e.ASN)
                    .WithMany(asn => asn.ASNDetails)
                    .HasForeignKey(e => e.ASNId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Item relationship
                entity.HasOne(e => e.Item)
                    .WithMany(i => i.ASNDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite unique constraint
                entity.HasIndex(e => new { e.ASNId, e.ItemId }).IsUnique();
            });

            // =============================================
            // SALES ORDER Configuration
            // =============================================
            modelBuilder.Entity<SalesOrder>(entity =>
            {
                entity.ToTable("SalesOrders");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.SONumber }).IsUnique();

                entity.Property(e => e.SONumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalWarehouseFee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.SalesOrders)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Customer relationship
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

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WarehouseFeeApplied).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(200);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // SO relationship
                entity.HasOne(e => e.SalesOrder)
                    .WithMany(so => so.SalesOrderDetails)
                    .HasForeignKey(e => e.SalesOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Item relationship
                entity.HasOne(e => e.Item)
                    .WithMany(i => i.SalesOrderDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite unique constraint
                entity.HasIndex(e => new { e.SalesOrderId, e.ItemId }).IsUnique();
            });

            // =============================================
            // INVENTORY Configuration
            // =============================================
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("Inventories");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.LastCostPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(200);

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Inventories)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Item relationship
                entity.HasOne(e => e.Item)
                    .WithMany(i => i.Inventories)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Location relationship
                entity.HasOne(e => e.Location)
                    .WithMany(l => l.Inventories)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite unique constraint
                entity.HasIndex(e => new { e.ItemId, e.LocationId }).IsUnique();
            });

            // =============================================
            // SEED DATA
            // =============================================
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Method untuk seed data awal
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default company
            modelBuilder.Entity<Company>().HasData(
                new Company
                {
                    Id = 1,
                    Name = "Default Company",
                    Code = "DEFAULT",
                    Email = "admin@defaultcompany.com",
                    Phone = "021-1234567",
                    Address = "Jakarta",
                    IsActive = true,
                    SubscriptionPlan = "Premium",
                    MaxUsers = 100,
                    CreatedDate = DateTime.Now
                }
            );

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = 1,
                    Name = "Admin",
                    Description = "Full system access",
                    Permissions = "[\"all\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Role
                {
                    Id = 2,
                    Name = "Manager",
                    Description = "Management access",
                    Permissions = "[\"read\",\"write\",\"approve\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                },
                new Role
                {
                    Id = 3,
                    Name = "User",
                    Description = "Standard user access",
                    Permissions = "[\"read\",\"write\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                }
            );

            // NOTE: Default admin user will be created via DbInitializer
            // to properly handle password hashing
        }

        /// <summary>
        /// Override SaveChanges untuk auto-update ModifiedDate dan CompanyId validation
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync untuk auto-update ModifiedDate dan CompanyId validation
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
            var baseEntries = ChangeTracker.Entries<BaseEntity>();
            var baseWithoutCompanyEntries = ChangeTracker.Entries<BaseEntityWithoutCompany>();

            foreach (var entry in baseEntries)
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

            foreach (var entry in baseWithoutCompanyEntries)
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