// Data/ApplicationDbContext.cs - Updated Version with Complete Entity Configuration
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
        public DbSet<Picking> Pickings { get; set; }
        public DbSet<PickingDetail> PickingDetails { get; set; }

        // DbSet untuk Audit & Logging
        public DbSet<AuditLog> AuditLogs { get; set; }

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

                // Unique constraints - Filtered to exclude soft-deleted records
                entity.HasIndex(e => e.Email).IsUnique()
                    .HasDatabaseName("IX_Users_Email")
                    .HasFilter("[IsDeleted] = 0");
                entity.HasIndex(e => new { e.CompanyId, e.Username }).IsUnique()
                    .HasDatabaseName("IX_Users_CompanyId_Username")
                    .HasFilter("[IsDeleted] = 0");

                // Column configurations
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.HashedPassword).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.EmailVerified).HasDefaultValue(false);
                entity.Property(e => e.ResetPasswordToken).HasMaxLength(200);
                entity.Property(e => e.EmailVerificationToken).HasMaxLength(200);
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

                // Composite unique constraint
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
            // WMS ENTITIES Configuration
            // =============================================

            // SUPPLIER Configuration
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");
                entity.HasKey(e => e.Id);

                // Unique constraints (scoped to company) - Filtered to exclude soft-deleted records
                entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique()
                    .HasDatabaseName("IX_Suppliers_CompanyId_Email")
                    .HasFilter("[IsDeleted] = 0");

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

                // Ensure proper cascade behavior for items
                entity.HasMany(s => s.Items)
                    .WithOne(i => i.Supplier)
                    .HasForeignKey(i => i.SupplierId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Suppliers_CompanyId");
                entity.HasIndex(e => new { e.CompanyId, e.Name }).HasDatabaseName("IX_Suppliers_CompanyId_Name");
            });

            // CUSTOMER Configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(e => e.Id);

                // Unique constraints - Filtered to exclude soft-deleted records
                entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique()
                    .HasDatabaseName("IX_Customers_CompanyId_Email")
                    .HasFilter("[IsDeleted] = 0");

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Customers)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Customers_CompanyId");
            });

            // ITEM Configuration
            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("Items");
                entity.HasKey(e => e.Id);

                // Unique constraints - Filtered to exclude soft-deleted records
                entity.HasIndex(e => new { e.CompanyId, e.ItemCode }).IsUnique()
                    .HasDatabaseName("IX_Items_CompanyId_ItemCode")
                    .HasFilter("[IsDeleted] = 0");

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

                // Supplier relationship
                entity.HasOne(i => i.Supplier)
                    .WithMany(s => s.Items)
                    .HasForeignKey(i => i.SupplierId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_Items_Suppliers");

                // Indexes for performance
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Items_CompanyId");
                entity.HasIndex(i => i.SupplierId).HasDatabaseName("IX_Items_SupplierId");
                entity.HasIndex(i => new { i.CompanyId, i.SupplierId })
                    .HasDatabaseName("IX_Items_CompanyId_SupplierId");
                entity.HasIndex(i => new { i.CompanyId, i.SupplierId, i.IsActive })
                    .HasDatabaseName("IX_Items_CompanyId_SupplierId_IsActive");
            });

            // LOCATION Configuration
            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");
                entity.HasKey(e => e.Id);

                // Unique constraints - Filtered to exclude soft-deleted records
                entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique()
                    .HasDatabaseName("IX_Locations_CompanyId_Code")
                    .HasFilter("[IsDeleted] = 0");

                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Locations)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Locations_CompanyId");
            });

            // PURCHASE ORDER Configuration
            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.ToTable("PurchaseOrders");
                entity.HasKey(e => e.Id);

                // Unique constraints - Filtered to exclude soft-deleted records
                entity.HasIndex(e => new { e.CompanyId, e.PONumber }).IsUnique()
                    .HasDatabaseName("IX_PurchaseOrders_CompanyId_PONumber")
                    .HasFilter("[IsDeleted] = 0");

                entity.Property(e => e.PONumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.PurchaseOrders)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Supplier)
                    .WithMany(s => s.PurchaseOrders)
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_PurchaseOrders_CompanyId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_PurchaseOrders_Status");
            });

            // PURCHASE ORDER DETAIL Configuration
            modelBuilder.Entity<PurchaseOrderDetail>(entity =>
            {
                entity.ToTable("PurchaseOrderDetails");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(200);

                entity.HasOne(e => e.PurchaseOrder)
                    .WithMany(po => po.PurchaseOrderDetails)
                    .HasForeignKey(e => e.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Item)
                    .WithMany(i => i.PurchaseOrderDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ASN Configuration
            modelBuilder.Entity<AdvancedShippingNotice>(entity =>
            {
                entity.ToTable("AdvancedShippingNotices");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.CompanyId, e.ASNNumber })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0")  // Hanya untuk record yang tidak dihapus
                    .HasDatabaseName("IX_ASN_CompanyId_ASNNumber");

                entity.Property(e => e.ASNNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.CarrierName).HasMaxLength(100);
                entity.Property(e => e.TrackingNumber).HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(500);

                // NEW: Configure ActualArrivalDate
                entity.Property(e => e.ActualArrivalDate)
                    .HasColumnType("datetime2")
                    .IsRequired(false); // Nullable

                entity.HasOne(e => e.PurchaseOrder)
                    .WithMany(po => po.AdvancedShippingNotices)
                    .HasForeignKey(e => e.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ASN_PurchaseOrders");

                // Indexes for performance
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_ASN_CompanyId");
                entity.HasIndex(e => e.PurchaseOrderId).HasDatabaseName("IX_ASN_PurchaseOrderId");
                entity.HasIndex(e => e.ActualArrivalDate).HasDatabaseName("IX_ASN_ActualArrivalDate");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_ASN_Status");
                entity.HasIndex(e => new { e.Status, e.ActualArrivalDate }).HasDatabaseName("IX_ASN_Status_ActualArrivalDate");
                entity.HasIndex(e => new { e.CompanyId, e.Status }).HasDatabaseName("IX_ASN_CompanyId_Status");
            });

            // ASN DETAIL Configuration
            modelBuilder.Entity<ASNDetail>(entity =>
            {
                entity.ToTable("ASNDetails");
                entity.HasKey(e => e.Id);

                // Column configurations
                entity.Property(e => e.ActualPricePerItem).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(200);
                
                // Putaway tracking fields
                entity.Property(e => e.RemainingQuantity)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Jumlah yang masih perlu di-putaway");
                entity.Property(e => e.AlreadyPutAwayQuantity)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Jumlah yang sudah di-putaway ke inventory");

                // ASN relationship - CASCADE is OK
                entity.HasOne(e => e.ASN)
                    .WithMany(asn => asn.ASNDetails)
                    .HasForeignKey(e => e.ASNId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ASNDetails_ASN");

                // Item relationship - RESTRICT to avoid cycles
                entity.HasOne(e => e.Item)
                    .WithMany(i => i.ASNDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ASNDetails_Items");

                // Company relationship - RESTRICT to avoid multiple cascade paths
                entity.HasOne(e => e.Company)
                    .WithMany()  // No navigation collection needed in Company
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ASNDetails_Companies");

                // Indexes for performance
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_ASNDetails_CompanyId");
                entity.HasIndex(e => e.ASNId).HasDatabaseName("IX_ASNDetails_ASNId");
                entity.HasIndex(e => e.ItemId).HasDatabaseName("IX_ASNDetails_ItemId");
                entity.HasIndex(e => e.RemainingQuantity).HasDatabaseName("IX_ASNDetails_RemainingQuantity");
                entity.HasIndex(e => e.AlreadyPutAwayQuantity).HasDatabaseName("IX_ASNDetails_AlreadyPutAwayQuantity");
                entity.HasIndex(e => new { e.ASNId, e.RemainingQuantity }).HasDatabaseName("IX_ASNDetails_ASNId_RemainingQuantity");
                entity.HasIndex(e => new { e.ASNId, e.AlreadyPutAwayQuantity }).HasDatabaseName("IX_ASNDetails_ASNId_AlreadyPutAwayQuantity");
            });

            // SALES ORDER Configuration
            modelBuilder.Entity<SalesOrder>(entity =>
            {
                entity.ToTable("SalesOrders");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.CompanyId, e.SONumber }).IsUnique()
                    .HasDatabaseName("IX_SalesOrders_CompanyId_SONumber");

                entity.Property(e => e.SONumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.SalesOrders)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.SalesOrders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.HoldingLocation)
                    .WithMany()
                    .HasForeignKey(e => e.HoldingLocationId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_SalesOrders_CompanyId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_SalesOrders_Status");
                entity.HasIndex(e => e.HoldingLocationId).HasDatabaseName("IX_SalesOrders_HoldingLocationId");
            });

            // SALES ORDER DETAIL Configuration
            modelBuilder.Entity<SalesOrderDetail>(entity =>
            {
                entity.ToTable("SalesOrderDetails");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(200);

                entity.HasOne(e => e.SalesOrder)
                    .WithMany(so => so.SalesOrderDetails)
                    .HasForeignKey(e => e.SalesOrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Item)
                    .WithMany(i => i.SalesOrderDetails)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // INVENTORY Configuration
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("Inventories");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.LocationId }).IsUnique()
                    .HasDatabaseName("IX_Inventories_CompanyId_ItemId_LocationId");

                entity.Property(e => e.LastCostPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(200);

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Inventories)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Item)
                    .WithMany(i => i.Inventories)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.Inventories)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_Inventories_CompanyId");
                entity.HasIndex(e => e.ItemId).HasDatabaseName("IX_Inventories_ItemId");
                entity.HasIndex(e => e.LocationId).HasDatabaseName("IX_Inventories_LocationId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_Inventories_Status");
                entity.HasIndex(e => e.Quantity).HasDatabaseName("IX_Inventories_Quantity");
                entity.HasIndex(e => new { e.CompanyId, e.ItemId }).HasDatabaseName("IX_Inventories_CompanyId_ItemId");
                entity.HasIndex(e => new { e.CompanyId, e.Status}).HasDatabaseName("IX_Inventories_CompanyId_Status");
                entity.HasIndex(e => new { e.ItemId, e.LocationId }).HasDatabaseName("IX_Inventories_ItemId_LocationId");
            });

            // PICKING Configuration
            ConfigurePickingEntity(modelBuilder);

            // PICKING DETAIL Configuration
            ConfigurePickingDetailEntity(modelBuilder);

            // AUDIT LOG Configuration
            ConfigureAuditLogEntity(modelBuilder);

            // =============================================
            // GLOBAL QUERY FILTERS (Soft Delete)
            // =============================================
            ConfigureSoftDeleteFilters(modelBuilder);
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
        /// Configure Picking entity - Document untuk proses picking barang dari warehouse
        /// </summary>
        private void ConfigurePickingEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Picking>(entity =>
            {
                // Table name
                entity.ToTable("Pickings");

                // Primary Key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.PickingNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(e => e.PickingDate)
                    .IsRequired();

                entity.Property(e => e.CompletedDate)
                    .IsRequired(false);

                entity.Property(e => e.Notes)
                    .HasMaxLength(500);

                // Indexes
                entity.HasIndex(e => e.PickingNumber).IsUnique();
                entity.HasIndex(e => e.SalesOrderId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.PickingDate);

                // Relationships
                entity.HasOne(e => e.SalesOrder)
                    .WithMany()
                    .HasForeignKey(e => e.SalesOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.PickingDetails)
                    .WithOne(d => d.Picking)
                    .HasForeignKey(d => d.PickingId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Configure PickingDetail entity - Detail picking per item dan lokasi
        /// </summary>
        private void ConfigurePickingDetailEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PickingDetail>(entity =>
            {
                // Table name
                entity.ToTable("PickingDetails");

                // Primary Key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.QuantityRequired)
                    .IsRequired();

                entity.Property(e => e.QuantityPicked)
                    .HasDefaultValue(0);

                entity.Property(e => e.RemainingQuantity)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(e => e.Notes)
                    .HasMaxLength(200);

                // Indexes
                entity.HasIndex(e => e.PickingId);
                entity.HasIndex(e => e.SalesOrderDetailId);
                entity.HasIndex(e => e.ItemId);
                entity.HasIndex(e => e.LocationId);
                entity.HasIndex(e => e.Status);

                // Relationships
                entity.HasOne(e => e.Picking)
                    .WithMany(p => p.PickingDetails)
                    .HasForeignKey(e => e.PickingId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SalesOrderDetail)
                    .WithMany()
                    .HasForeignKey(e => e.SalesOrderDetailId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Item)
                    .WithMany()
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany()
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        /// <summary>
        /// Configure AuditLog entity - Audit trail for all important actions
        /// </summary>
        private void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.Id);

                // Column configurations
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Module).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntityDescription).HasMaxLength(200);
                entity.Property(e => e.OldValue).HasColumnType("nvarchar(max)");
                entity.Property(e => e.NewValue).HasColumnType("nvarchar(max)");
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsSuccess).HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Relationships
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AuditLogs_Companies");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_AuditLogs_Users");

                // Indexes for performance
                entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_AuditLogs_CompanyId");
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_AuditLogs_UserId");
                entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_AuditLogs_Timestamp");
                entity.HasIndex(e => e.Action).HasDatabaseName("IX_AuditLogs_Action");
                entity.HasIndex(e => e.Module).HasDatabaseName("IX_AuditLogs_Module");
                entity.HasIndex(e => new { e.CompanyId, e.Timestamp }).HasDatabaseName("IX_AuditLogs_CompanyId_Timestamp");
                entity.HasIndex(e => new { e.Module, e.Action }).HasDatabaseName("IX_AuditLogs_Module_Action");
            });
        }

        /// <summary>
        /// Configure global query filters for soft delete
        /// Automatically filter out deleted records from queries
        /// </summary>
        private void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
        {
            // Apply soft delete filter to entities with IsDeleted property
            // Note: IsDeleted property needs to be added to BaseEntity if not exists
            
            // For now, we'll filter based on context - operational entities only
            // Master data (Items, Locations, Customers, Suppliers) use IsActive instead
            
            // Operational entities that should respect soft delete
            modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<AdvancedShippingNotice>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<SalesOrder>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Picking>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Location>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
            
            // Note: To query including deleted items, use:
            // context.PurchaseOrders.IgnoreQueryFilters().Where(...)
        }
    }
}