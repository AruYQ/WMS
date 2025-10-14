// Data/DbInitializer.cs - Fixed to properly assign suppliers to items
using WMS.Models;
using WMS.Utilities;
using Microsoft.EntityFrameworkCore;

namespace WMS.Data
{
    /// <summary>
    /// Database initializer yang dapat dijalankan dari console package manager
    /// Membuat semua dummy data untuk development dan testing
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Initialize database dengan semua dummy data
        /// Bisa dipanggil dari Package Manager Console dengan: Update-Database
        /// </summary>
        public static void Initialize(ApplicationDbContext context)
        {
            try
            {
                Console.WriteLine("Starting Database Initialization...");

                // Pastikan database dibuat
                context.Database.EnsureCreated();
                Console.WriteLine("Database created/verified");

                // Check apakah sudah ada data
                if (context.Companies.Any())
                {
                    Console.WriteLine("Database already has data, skipping initialization");
                    return;
                }

                // Seed data secara berurutan
                SeedRoles(context);
                SeedCompanies(context);
                SeedUsers(context);
                SeedMasterData(context);

                Console.WriteLine("Database initialization completed successfully!");
                Console.WriteLine("Summary:");
                Console.WriteLine($"   - Companies: {context.Companies.Count()}");
                Console.WriteLine($"   - Users: {context.Users.Count()}");
                Console.WriteLine($"   - Roles: {context.Roles.Count()}");
                Console.WriteLine($"   - Items: {context.Items.Count()}");
                Console.WriteLine($"   - Locations: {context.Locations.Count()}");
                Console.WriteLine($"   - Suppliers: {context.Suppliers.Count()}");

                // Display login credentials
                Console.WriteLine("\n=== LOGIN CREDENTIALS ===");
                var companies = context.Companies.ToList();
                foreach (var company in companies)
                {
                    Console.WriteLine($"Company: {company.Name} ({company.Code})");
                    Console.WriteLine($"  Admin Username: admin{company.Id}");
                    Console.WriteLine($"  Admin Email: admin{company.Id}@{company.Code.ToLower()}.com");
                    Console.WriteLine($"  Password: admin123");
                    Console.WriteLine($"  Other users password: password123");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during initialization: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        #region 1. Seed Roles
        private static void SeedRoles(ApplicationDbContext context)
        {
            Console.WriteLine("Creating roles...");

            var roles = new List<Role>
            {
                new Role
                {
                    Name = "SuperAdmin",
                    Description = "System administrator - Company management only",
                    Permissions = "[\"COMPANY_MANAGE\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-90),
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Admin",
                    Description = "Company administrator - Master data management",
                    Permissions = "[\"ITEM_VIEW\",\"ITEM_MANAGE\",\"LOCATION_VIEW\",\"LOCATION_MANAGE\",\"CUSTOMER_VIEW\",\"CUSTOMER_MANAGE\",\"SUPPLIER_VIEW\",\"SUPPLIER_MANAGE\",\"USER_VIEW\",\"USER_MANAGE\",\"INVENTORY_VIEW\",\"REPORT_CREATE\",\"AUDIT_VIEW\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-90),
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "WarehouseStaff",
                    Description = "Warehouse operations - Daily operational tasks",
                    Permissions = "[\"ITEM_VIEW\",\"LOCATION_VIEW\",\"CUSTOMER_VIEW\",\"SUPPLIER_VIEW\",\"PO_MANAGE\",\"ASN_MANAGE\",\"SO_MANAGE\",\"PICKING_MANAGE\",\"PUTAWAY_MANAGE\",\"INVENTORY_MANAGE\",\"REPORT_VIEW\",\"AUDIT_VIEW\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-90),
                    CreatedBy = "System"
                }
            };

            context.Roles.AddRange(roles);
            context.SaveChanges();
            Console.WriteLine($"   Created {roles.Count} roles");
        }
        #endregion

        #region 2. Seed Companies
        private static void SeedCompanies(ApplicationDbContext context)
        {
            Console.WriteLine("Creating companies...");

            var companies = new List<Company>
            {
                new Company
                {
                    Name = "PT Gudang Utama",
                    Code = "MAIN",
                    Email = "admin@gudangutama.com",
                    Phone = "021-5551234",
                    Address = "Jl. Industri Raya No. 123, Jakarta Utara 14350",
                    ContactPerson = "Budi Santoso",
                    TaxNumber = "01.234.567.8-901.000",
                    IsActive = true,
                    SubscriptionPlan = "Premium",
                    MaxUsers = 100,
                    SubscriptionEndDate = DateTime.Now.AddYears(1),
                    CreatedDate = DateTime.Now.AddDays(-90),
                    CreatedBy = "System"
                },
                new Company
                {
                    Name = "CV Logistik Prima",
                    Code = "PRIMA",
                    Email = "info@logistikprima.com",
                    Phone = "021-5554321",
                    Address = "Jl. Raya Bekasi KM 25, Bekasi 17530",
                    ContactPerson = "Sari Dewi",
                    TaxNumber = "02.345.678.9-012.000",
                    IsActive = true,
                    SubscriptionPlan = "Basic",
                    MaxUsers = 25,
                    SubscriptionEndDate = DateTime.Now.AddMonths(6),
                    CreatedDate = DateTime.Now.AddDays(-60),
                    CreatedBy = "System"
                },
                new Company
                {
                    Name = "PT Warehouse Modern",
                    Code = "MODERN",
                    Email = "contact@warehousemodern.com",
                    Phone = "021-5557890",
                    Address = "Kawasan Industri Cikupa, Tangerang 15710",
                    ContactPerson = "Ahmad Rahman",
                    TaxNumber = "03.456.789.0-123.000",
                    IsActive = true,
                    SubscriptionPlan = "Premium",
                    MaxUsers = 150,
                    SubscriptionEndDate = DateTime.Now.AddYears(2),
                    CreatedDate = DateTime.Now.AddDays(-30),
                    CreatedBy = "System"
                }
            };

            context.Companies.AddRange(companies);
            context.SaveChanges();
            Console.WriteLine($"   Created {companies.Count} companies");
        }
        #endregion

        #region 3. Seed Users (FIXED - menggunakan PasswordHelper)
        private static void SeedUsers(ApplicationDbContext context)
        {
            Console.WriteLine("Creating users with proper password hashing...");

            var companies = context.Companies.ToList();
            var roles = context.Roles.ToList();
            var allUsers = new List<User>();
            var allUserRoles = new List<UserRole>();

            // 0. SUPERADMIN USER (Global, no specific company - password: superadmin123)
            var superAdminUser = new User
            {
                Username = "superadmin",
                Email = "superadmin@wms.com",
                FullName = "Super Administrator",
                HashedPassword = PasswordHelper.HashPassword("superadmin123"),
                CompanyId = null, // SuperAdmin tidak punya CompanyId - global access
                Phone = "08123456789",
                IsActive = true,
                EmailVerified = true,
                LastLoginDate = DateTime.Now.AddDays(-1),
                CreatedDate = DateTime.Now.AddDays(-90),
                CreatedBy = "System"
            };
            allUsers.Add(superAdminUser);

            foreach (var company in companies)
            {
                // 1. AUTO ADMIN - admin{CompanyId}
                var adminUser = new User
                {
                    Username = $"admin{company.Id}",
                    Email = $"admin{company.Id}@{company.Code.ToLower()}.com",
                    FullName = $"Admin {company.Code}",
                    HashedPassword = PasswordHelper.HashPassword("admin123"),
                    CompanyId = company.Id,
                    Phone = $"081{Random.Shared.Next(10000000, 99999999)}",
                    IsActive = true,
                    EmailVerified = true,
                    LastLoginDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 5)),
                    CreatedDate = DateTime.Now.AddDays(-85),
                    CreatedBy = "System"
                };
                allUsers.Add(adminUser);

                // 2. WAREHOUSE STAFF USERS (2 per company)
                var userData = new[]
                {
                    ($"staff1_{company.Code.ToLower()}", $"staff1@{company.Code.ToLower()}.com", "Warehouse Staff 1", "WarehouseStaff"),
                    ($"staff2_{company.Code.ToLower()}", $"staff2@{company.Code.ToLower()}.com", "Warehouse Staff 2", "WarehouseStaff")
                };

                foreach (var (username, email, fullName, roleName) in userData)
                {
                    var user = new User
                    {
                        Username = username,
                        Email = email,
                        FullName = fullName,
                        HashedPassword = PasswordHelper.HashPassword("password123"),
                        CompanyId = company.Id,
                        Phone = $"081{Random.Shared.Next(10000000, 99999999)}",
                        IsActive = true,
                        EmailVerified = true,
                        LastLoginDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 30)),
                        CreatedDate = DateTime.Now.AddDays(-Random.Shared.Next(40, 80)),
                        CreatedBy = "System"
                    };
                    allUsers.Add(user);
                }
            }

            context.Users.AddRange(allUsers);
            context.SaveChanges();

            // Assign roles
            foreach (var user in allUsers)
            {
                string roleName = user.Username switch
                {
                    "superadmin" => "SuperAdmin",
                    var u when u.StartsWith("admin") => "Admin",
                    var u when u.Contains("staff") => "WarehouseStaff",
                    _ => "WarehouseStaff"
                };

                var role = roles.FirstOrDefault(r => r.Name == roleName);
                if (role != null)
                {
                    allUserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedDate = user.CreatedDate,
                        AssignedBy = "System",
                        CreatedDate = user.CreatedDate,
                        CreatedBy = "System"
                    });
                }
            }

            context.UserRoles.AddRange(allUserRoles);
            context.SaveChanges();
            Console.WriteLine($"   Created {allUsers.Count} users with proper password hashing");
        }
        #endregion

        #region 4. Seed Master Data
        private static void SeedMasterData(ApplicationDbContext context)
        {
            Console.WriteLine("Creating master data...");

            var companies = context.Companies.ToList();

            foreach (var company in companies)
            {
                // Create suppliers first
                SeedSuppliersForCompany(context, company);
                context.SaveChanges(); // Save suppliers so they get IDs

                // Create locations
                SeedLocationsForCompany(context, company);

                // Create items and assign to suppliers
                SeedItemsForCompany(context, company);
            }

            context.SaveChanges();
            Console.WriteLine("   Master data created for all companies");
        }

        private static void SeedSuppliersForCompany(ApplicationDbContext context, Company company)
        {
            var suppliers = new List<Supplier>
            {
                new Supplier
                {
                    Name = "PT Electronics Supplier Jakarta",
                    Code = "ELC001",
                    Email = $"electronics@supplier-{company.Code.ToLower()}.com",
                    Phone = "021-5551111",
                    City = "Jakarta Barat",
                    ContactPerson = "Budi Santoso",
                    Address = null, // Dihilangkan
                    CompanyId = company.Id,
                    CreatedDate = DateTime.Now.AddDays(-85),
                    CreatedBy = $"admin{company.Id}"
                },
                new Supplier
                {
                    Name = "CV Furniture Nusantara",
                    Code = "FUR001",
                    Email = $"furniture@supplier-{company.Code.ToLower()}.com",
                    Phone = "021-5552222",
                    City = "Depok",
                    ContactPerson = "Sari Dewi",
                    Address = null, // Dihilangkan
                    CompanyId = company.Id,
                    CreatedDate = DateTime.Now.AddDays(-80),
                    CreatedBy = $"admin{company.Id}"
                },
                new Supplier
                {
                    Name = "PT Fashion Trends Indonesia",
                    Code = "FAS001",
                    Email = $"fashion@supplier-{company.Code.ToLower()}.com",
                    Phone = "021-5553333",
                    City = "Jakarta Selatan",
                    ContactPerson = "Ahmad Rahman",
                    Address = null, // Dihilangkan
                    CompanyId = company.Id,
                    CreatedDate = DateTime.Now.AddDays(-75),
                    CreatedBy = $"admin{company.Id}"
                }
            };

            context.Suppliers.AddRange(suppliers);
        }

        private static void SeedItemsForCompany(ApplicationDbContext context, Company company)
        {
            // Get existing suppliers for this company
            var suppliers = context.Suppliers.Where(s => s.CompanyId == company.Id).ToList();

            var categories = new[]
            {
                ("ELC", "Electronics", new[] { "Smartphone Samsung Galaxy", "Laptop ASUS", "Tablet iPad", "Headphone Sony", "Speaker JBL" }),
                ("FUR", "Furniture", new[] { "Sofa 3 Seater", "Meja Kantor", "Kursi Gaming", "Lemari Pakaian", "Rak Buku" }),
                ("CLO", "Clothing", new[] { "T-Shirt Cotton", "Jeans Denim", "Jacket Hoodie", "Dress Casual", "Sepatu Sneakers" })
            };

            var units = new[] { "pcs", "box", "pack", "unit", "set" };
            var items = new List<Item>();

            foreach (var (categoryCode, categoryName, itemNames) in categories)
            {
                for (int i = 0; i < itemNames.Length; i++)
                {
                    var itemCode = $"{categoryCode}{(i + 1):D3}";
                    var standardPrice = Random.Shared.Next(50000, 2000000);

                    // Assign supplier based on category
                    var supplier = categoryCode switch
                    {
                        "ELC" => suppliers.FirstOrDefault(s => s.Name.Contains("Electronics")),
                        "FUR" => suppliers.FirstOrDefault(s => s.Name.Contains("Furniture")),
                        "CLO" => suppliers.FirstOrDefault(s => s.Name.Contains("Fashion")),
                        _ => suppliers.FirstOrDefault()
                    };

                    items.Add(new Item
                    {
                        ItemCode = itemCode,
                        Name = itemNames[i],
                        Description = $"Quality {itemNames[i].ToLower()} from {categoryName.ToLower()} category",
                        Unit = units[Random.Shared.Next(units.Length)],
                        StandardPrice = standardPrice,
                        SupplierId = supplier?.Id, // FIXED: Assign supplier to item
                        CompanyId = company.Id,
                        CreatedDate = DateTime.Now.AddDays(-Random.Shared.Next(60, 90)),
                        CreatedBy = $"admin{company.Id}"
                    });
                }
            }

            context.Items.AddRange(items);
        }

        private static void SeedLocationsForCompany(ApplicationDbContext context, Company company)
        {
            var locations = new List<Location>();

            // Storage locations
            var zones = new[] { "A", "B", "C" };
            var racks = Enumerable.Range(1, 3).ToArray();
            var slots = Enumerable.Range(1, 5).ToArray();

            foreach (var zone in zones)
            {
                foreach (var rack in racks)
                {
                    foreach (var slot in slots)
                    {
                        locations.Add(new Location
                        {
                            Code = $"{zone}-{rack:D2}-{slot:D2}",
                            Name = $"Zone {zone} Rack {rack} Slot {slot}",
                            Description = $"Storage location in zone {zone}",
                            MaxCapacity = Random.Shared.Next(100, 500),
                            CurrentCapacity = 0, // Fixed: Always start with empty capacity
                            IsFull = false, // Fixed: Always start as not full
                            CompanyId = company.Id,
                            CreatedDate = DateTime.Now.AddDays(-Random.Shared.Next(60, 90)),
                            CreatedBy = $"admin{company.Id}"
                        });
                    }
                }
            }

            // Special areas
            var specialAreas = new[]
            {
                ("RECEIVING", "Receiving Area", "Temporary storage for incoming goods", 1000),
                ("SHIPPING", "Shipping Area", "Staging area for outbound goods", 1000),
                ("QUARANTINE", "Quarantine Area", "Hold area for damaged items", 200),
                ("RETURNS", "Returns Area", "Area for returned items", 300)
            };

            foreach (var (code, name, desc, capacity) in specialAreas)
            {
                locations.Add(new Location
                {
                    Code = code,
                    Name = name,
                    Description = desc,
                    MaxCapacity = capacity,
                    CurrentCapacity = 0, // Fixed: Always start with empty capacity
                    IsFull = false, // Fixed: Always start as not full
                    CompanyId = company.Id,
                    CreatedDate = DateTime.Now.AddDays(-90),
                    CreatedBy = "System"
                });
            }

            // Note: IsFull status already set to false above for all locations since CurrentCapacity = 0

            context.Locations.AddRange(locations);
        }
        #endregion

        #region Backward Compatibility Methods
        /// <summary>
        /// Async version untuk compatibility
        /// </summary>
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            await Task.Run(() => Initialize(context));
        }

        /// <summary>
        /// Version dengan IConfiguration untuk compatibility
        /// </summary>
        public static void Initialize(ApplicationDbContext context, IConfiguration configuration)
        {
            Initialize(context);
        }

        /// <summary>
        /// Version dengan logging untuk compatibility
        /// </summary>
        public static Task Initialize(ApplicationDbContext context, IConfiguration configuration, ILogger logger)
        {
            logger.LogInformation("Starting database initialization from DbInitializer");
            Initialize(context);
            logger.LogInformation("Database initialization completed from DbInitializer");
            return Task.CompletedTask;
        }
        #endregion
    }
}