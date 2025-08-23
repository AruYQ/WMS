// Data/ApplicationDbContext.cs - Updated Version
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Services;

namespace WMS.Data
{
    /// <summary>
    /// Database context untuk WMS Application
    /// Mengatur mapping entity ke database dan konfigurasi relationship
    /// Enhanced dengan authentication dan company filtering
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Constructor with current user service for audit trail
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentUserService currentUserService) : base(options)
        {
            _currentUserService = currentUserService;
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
                entity.HasIndex(e => e.Code).IsUnique()
                    .HasDatabaseName("IX_Companies_Code");
                entity.HasIndex(e => e.Email).IsUnique()
                    .HasDatabaseName("IX_Companies_Email");

                // Column configurations
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(300);
                entity.Property(e => e.ContactPerson).HasMaxLength(100);
                entity.Property(e => e.TaxNumber).HasMaxLength(20);
                entity.Property(e => e.SubscriptionPlan).HasMaxLength(20).HasDefaultValue("Free");
                entity.Property(e => e.MaxUsers).HasDefaultValue(5);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Indexes for performance
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Companies_IsActive");
                entity.HasIndex(e => e.SubscriptionEndDate).HasDatabaseName("IX_Companies_SubscriptionEndDate");
            });

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);

                // Unique constraints - Global unique (across all companies for email, scoped for username)
                entity.HasIndex(e => e.Email).IsUnique()
                    .HasDatabaseName("IX_Users_Email");
                entity.HasIndex(e => new { e.CompanyId, e.Username }).IsUnique()
                    .HasDatabaseName("IX_Users_CompanyId_Username");

                // Column configurations
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
                entity.Property(e => e.PasswordSalt).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.EmailVerified).HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Foreign Key to Company
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Users)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Users_Companies");

                // Indexes for performance
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Users_CompanyId");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Users_IsActive");
                entity.HasIndex(e => e.LastLoginDate).HasDatabaseName("IX_Users_LastLoginDate");
                entity.HasIndex(e => e.ResetPasswordToken).HasDatabaseName("IX_Users_ResetPasswordToken");
            });

            // Role Configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.Id);

                // Unique constraints
                entity.HasIndex(e => e.Name).IsUnique()
                    .HasDatabaseName("IX_Roles_Name");

                // Column configurations
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.Permissions).IsRequired().HasColumnType("nvarchar(max)");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Indexes
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Roles_IsActive");
            });

            // UserRole Configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(e => e.Id);

                // Composite unique constraint - One user can only have one instance of each role
                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique()
                    .HasDatabaseName("IX_UserRoles_UserId_RoleId");

                // Column configurations
                entity.Property(e => e.AssignedBy).HasMaxLength(50);
                entity.Property(e => e.AssignedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Relationships
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_UserRoles_Users");

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_UserRoles_Roles");

                // Indexes
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_UserRoles_UserId");
                entity.HasIndex(e => e.RoleId).HasDatabaseName("IX_UserRoles_RoleId");
                entity.HasIndex(e => e.AssignedDate).HasDatabaseName("IX_UserRoles_AssignedDate");
            });

            // =============================================
            // SUPPLIER Configuration
            // =============================================
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique()
                    .HasDatabaseName("IX_Suppliers_CompanyId_Email");

                // Column configurations
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Suppliers)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Suppliers_Companies");

                // Indexes
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Suppliers_CompanyId");
                entity.HasIndex(e => new { e.CompanyId, e.Name }).HasDatabaseName("IX_Suppliers_CompanyId_Name");
            });

            // =============================================
            // CUSTOMER Configuration
            // =============================================
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique()
                    .HasDatabaseName("IX_Customers_CompanyId_Email");

                // Column configurations
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Customers)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Customers_Companies");

                // Indexes
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Customers_CompanyId");
                entity.HasIndex(e => new { e.CompanyId, e.Name }).HasDatabaseName("IX_Customers_CompanyId_Name");
            });

            // =============================================
            // ITEM Configuration
            // =============================================
            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("Items");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.ItemCode }).IsUnique()
                    .HasDatabaseName("IX_Items_CompanyId_ItemCode");

                // Column configurations
                entity.Property(e => e.ItemCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(10);
                entity.Property(e => e.StandardPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Items)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Items_Companies");

                // Indexes
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Items_CompanyId");
                entity.HasIndex(e => new { e.CompanyId, e.Name }).HasDatabaseName("IX_Items_CompanyId_Name");
                entity.HasIndex(e => new { e.CompanyId, e.IsActive }).HasDatabaseName("IX_Items_CompanyId_IsActive");
            });

            // =============================================
            // LOCATION Configuration
            // =============================================
            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique()
                    .HasDatabaseName("IX_Locations_CompanyId_Code");

                // Column configurations
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Locations)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Locations_Companies");

                // Indexes
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Locations_CompanyId");
            });

            // Configure other entities... (keeping the existing configurations but with enhanced indexes)
            ConfigureWMSEntities(modelBuilder);

            // =============================================
            // SEED DATA
            // =============================================
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Configure WMS entities (Purchase Orders, Sales Orders, etc.)
        /// </summary>
        private void ConfigureWMSEntities(ModelBuilder modelBuilder)
        {
            // PURCHASE ORDER Configuration
            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.ToTable("PurchaseOrders");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company)
                entity.HasIndex(e => new { e.CompanyId, e.PONumber }).IsUnique()
                    .HasDatabaseName("IX_PurchaseOrders_CompanyId_PONumber");

                entity.Property(e => e.PONumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Company relationship
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.PurchaseOrders)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PurchaseOrders_Companies");

                // Supplier relationship
                entity.HasOne(e => e.Supplier)
                    .WithMany(s => s.PurchaseOrders)
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_PurchaseOrders_Suppliers");

                // Indexes
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_PurchaseOrders_CompanyId");
                entity.HasIndex(e => e.SupplierId).HasDatabaseName("IX_PurchaseOrders_SupplierId");
                entity.HasIndex(e => new { e.CompanyId, e.Status }).HasDatabaseName("IX_PurchaseOrders_CompanyId_Status");
                entity.HasIndex(e => new { e.CompanyId, e.OrderDate }).HasDatabaseName("IX_PurchaseOrders_CompanyId_PODate");
            });

            // Continue with other entities using similar enhanced patterns...
            // For brevity, I'll show the pattern for one more entity

            // SALES ORDER Configuration  
            modelBuilder.Entity<SalesOrder>(entity =>
            {
                entity.ToTable("SalesOrders");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.CompanyId, e.SONumber }).IsUnique()
                    .HasDatabaseName("IX_SalesOrders_CompanyId_SONumber");

                entity.Property(e => e.SONumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalWarehouseFee).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.SalesOrders)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_SalesOrders_Companies");

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.SalesOrders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_SalesOrders_Customers");

                // Indexes
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_SalesOrders_CompanyId");
                entity.HasIndex(e => e.CustomerId).HasDatabaseName("IX_SalesOrders_CustomerId");
                entity.HasIndex(e => new { e.CompanyId, e.Status }).HasDatabaseName("IX_SalesOrders_CompanyId_Status");
            });

            // Configure other entities (PurchaseOrderDetail, ASN, ASNDetail, SalesOrderDetail, Inventory)
            // following the same enhanced pattern with proper indexes and constraints
        }

        /// <summary>
        /// Enhanced method untuk seed data awal dengan lebih banyak default data
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
                    Address = "Jakarta, Indonesia",
                    ContactPerson = "System Administrator",
                    IsActive = true,
                    SubscriptionPlan = "Premium",
                    MaxUsers = 100,
                    SubscriptionEndDate = new DateTime(2025, 12, 31),
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                }
            );

            // Seed enhanced default roles with proper permissions
            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = 1,
                    Name = "Admin",
                    Description = "Full system access - can manage all aspects of the system including users and company settings",
                    Permissions = "[\"all\", \"create\", \"read\", \"update\", \"delete\", \"manage_users\", \"manage_company\", \"view_reports\", \"export_data\", \"manage_roles\", \"audit_log\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Id = 2,
                    Name = "Manager",
                    Description = "Management access - can view reports, approve transactions, and manage operations",
                    Permissions = "[\"read\", \"update\", \"approve\", \"view_reports\", \"manage_inventory\", \"manage_orders\", \"view_analytics\", \"export_data\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Id = 3,
                    Name = "User",
                    Description = "Standard user access - can perform daily operations and basic data entry",
                    Permissions = "[\"read\", \"create\", \"update\", \"manage_inventory\", \"process_orders\", \"view_own_data\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Id = 4,
                    Name = "Viewer",
                    Description = "Read-only access - can only view data and basic reports",
                    Permissions = "[\"read\", \"view_reports\", \"view_own_data\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                }
            );

            // NOTE: Default admin user will be created via DbInitializer
            // to properly handle password hashing
        }

        /// <summary>
        /// Override SaveChanges untuk auto-update ModifiedDate dan audit trail
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync untuk auto-update ModifiedDate dan audit trail
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Update timestamps dan audit fields untuk semua changes
        /// </summary>
        private void UpdateTimestamps()
        {
            var currentUser = _currentUserService?.Username ?? "System";
            var now = DateTime.Now;

            // Handle BaseEntity (with CompanyId)
            var baseEntries = ChangeTracker.Entries<BaseEntity>();
            foreach (var entry in baseEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = now;
                        entry.Entity.CreatedBy = currentUser;

                        // Auto-set CompanyId if not set and current user has company context
                        if (entry.Entity.CompanyId == 0 && _currentUserService?.CompanyId.HasValue == true)
                        {
                            entry.Entity.CompanyId = _currentUserService.CompanyId.Value;
                        }
                        break;

                    case EntityState.Modified:
                        entry.Entity.ModifiedDate = now;
                        entry.Entity.ModifiedBy = currentUser;

                        // Prevent CompanyId changes
                        entry.Property(e => e.CompanyId).IsModified = false;
                        entry.Property(e => e.CreatedDate).IsModified = false;
                        entry.Property(e => e.CreatedBy).IsModified = false;
                        break;
                }
            }

            // Handle BaseEntityWithoutCompany
            var baseWithoutCompanyEntries = ChangeTracker.Entries<BaseEntityWithoutCompany>();
            foreach (var entry in baseWithoutCompanyEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = now;
                        entry.Entity.CreatedBy = currentUser;
                        break;

                    case EntityState.Modified:
                        entry.Entity.ModifiedDate = now;
                        entry.Entity.ModifiedBy = currentUser;

                        // Prevent audit field changes
                        entry.Property(e => e.CreatedDate).IsModified = false;
                        entry.Property(e => e.CreatedBy).IsModified = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Create query filter untuk automatic company filtering
        /// This can be used untuk global query filters (optional feature)
        /// </summary>
        private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            // Example global filter - uncomment if you want automatic company filtering
            // Note: This might not work well with admin scenarios where you need to see all companies

            /*
            var companyId = _currentUserService?.CompanyId;
            if (companyId.HasValue)
            {
                // Apply global filter untuk all entities dengan CompanyId
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var property = entityType.FindProperty("CompanyId");
                    if (property != null)
                    {
                        var parameter = Expression.Parameter(entityType.ClrType);
                        var body = Expression.Equal(
                            Expression.Property(parameter, "CompanyId"),
                            Expression.Constant(companyId.Value));
                        
                        modelBuilder.Entity(entityType.ClrType)
                            .HasQueryFilter(Expression.Lambda(body, parameter));
                    }
                }
            }
            */
        }
    }
}