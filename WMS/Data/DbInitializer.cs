// Data/DbInitializer.cs
using WMS.Models;
using Microsoft.EntityFrameworkCore;
using WMS.Utilities;

namespace WMS.Data
{
    /// <summary>
    /// Helper class untuk initialize database dengan sample data dan authentication data
    /// </summary>
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, IConfiguration configuration, ILogger logger)
        {
            try
            {
                // Pastikan database sudah dibuat
                await context.Database.EnsureCreatedAsync();

                // Initialize dalam transaction
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    await SeedCompaniesAsync(context, configuration, logger);
                    await SeedRolesAsync(context, logger);
                    await SeedUsersAsync(context, configuration, logger);
                    await SeedSampleDataAsync(context, logger);

                    await transaction.CommitAsync();
                    logger.LogInformation("Database initialization completed successfully");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Error during database initialization, transaction rolled back");
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error during database initialization");
                throw;
            }
        }

        /// <summary>
        /// Seed companies data
        /// </summary>
        private static async Task SeedCompaniesAsync(ApplicationDbContext context, IConfiguration configuration, ILogger logger)
        {
            if (await context.Companies.AnyAsync())
            {
                logger.LogInformation("Companies already exist, skipping company seeding");
                return;
            }

            logger.LogInformation("Seeding default company...");

            var defaultCompany = new Company
            {
                Name = configuration.GetValue<string>("WMSSettings:DefaultCompany:Name", "Default Company")!,
                Code = configuration.GetValue<string>("WMSSettings:DefaultCompany:Code", "DEFAULT")!,
                Email = configuration.GetValue<string>("WMSSettings:DefaultCompany:Email", "admin@defaultcompany.com")!,
                Phone = "021-1234567",
                Address = "Jakarta, Indonesia",
                ContactPerson = "System Administrator",
                IsActive = true,
                SubscriptionPlan = "Premium",
                MaxUsers = 100,
                SubscriptionEndDate = DateTime.Now.AddYears(1),
                CreatedDate = DateTime.Now,
                CreatedBy = "System"
            };

            context.Companies.Add(defaultCompany);
            await context.SaveChangesAsync();

            logger.LogInformation("Default company '{CompanyName}' created with ID {CompanyId}",
                defaultCompany.Name, defaultCompany.Id);
        }

        /// <summary>
        /// Seed roles data
        /// </summary>
        private static async Task SeedRolesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Roles.AnyAsync())
            {
                logger.LogInformation("Roles already exist, skipping role seeding");
                return;
            }

            logger.LogInformation("Seeding default roles...");

            var roles = new List<Role>
            {
                new Role
                {
                    Name = "Admin",
                    Description = "Full system access - can manage all aspects of the system",
                    Permissions = "[\"all\", \"create\", \"read\", \"update\", \"delete\", \"manage_users\", \"manage_company\", \"view_reports\", \"export_data\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Manager",
                    Description = "Management access - can view reports and approve transactions",
                    Permissions = "[\"read\", \"update\", \"approve\", \"view_reports\", \"manage_inventory\", \"manage_orders\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "User",
                    Description = "Standard user access - can perform daily operations",
                    Permissions = "[\"read\", \"create\", \"update\", \"manage_inventory\", \"process_orders\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Role
                {
                    Name = "Viewer",
                    Description = "Read-only access - can only view data",
                    Permissions = "[\"read\", \"view_reports\"]",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();

            logger.LogInformation("Created {RoleCount} default roles", roles.Count);
        }

        /// <summary>
        /// Seed users data
        /// </summary>
        private static async Task SeedUsersAsync(ApplicationDbContext context, IConfiguration configuration, ILogger logger)
        {
            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Users already exist, skipping user seeding");
                return;
            }

            logger.LogInformation("Seeding default admin user...");

            // Get default company
            var defaultCompany = await context.Companies.FirstAsync(c => c.Code == "DEFAULT");

            // Create default admin user
            var adminPassword = configuration.GetValue<string>("WMSSettings:DefaultAdmin:Password", "admin123")!;
            var salt = PasswordHelper.GenerateSalt();
            var hashedPassword = PasswordHelper.HashPassword(adminPassword, salt);

            var adminUser = new User
            {
                Username = configuration.GetValue<string>("WMSSettings:DefaultAdmin:Username", "admin")!,
                Email = configuration.GetValue<string>("WMSSettings:DefaultAdmin:Email", "admin@defaultcompany.com")!,
                FullName = configuration.GetValue<string>("WMSSettings:DefaultAdmin:FullName", "System Administrator")!,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                CompanyId = defaultCompany.Id,
                Phone = "021-1234567",
                IsActive = true,
                EmailVerified = true,
                CreatedDate = DateTime.Now,
                CreatedBy = "System"
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            // Assign Admin role to admin user
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var userRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedDate = DateTime.Now,
                AssignedBy = "System",
                CreatedDate = DateTime.Now,
                CreatedBy = "System"
            };

            context.UserRoles.Add(userRole);
            await context.SaveChangesAsync();

            logger.LogInformation("Default admin user '{Username}' created with Admin role", adminUser.Username);

            // Create demo users (optional - only in development)
            if (configuration.GetValue<bool>("WMSSettings:CreateDemoUsers", false))
            {
                await SeedDemoUsersAsync(context, defaultCompany.Id, logger);
            }
        }

        /// <summary>
        /// Seed demo users (optional)
        /// </summary>
        private static async Task SeedDemoUsersAsync(ApplicationDbContext context, int companyId, ILogger logger)
        {
            logger.LogInformation("Creating demo users...");

            var demoUsers = new List<(string username, string email, string fullName, string roleName)>
            {
                ("manager", "manager@defaultcompany.com", "Demo Manager", "Manager"),
                ("user1", "user1@defaultcompany.com", "Demo User 1", "User"),
                ("viewer", "viewer@defaultcompany.com", "Demo Viewer", "Viewer")
            };

            foreach (var (username, email, fullName, roleName) in demoUsers)
            {
                var password = "demo123";
                var salt = PasswordHelper.GenerateSalt();
                var hashedPassword = PasswordHelper.HashPassword(password, salt);

                var user = new User
                {
                    Username = username,
                    Email = email,
                    FullName = fullName,
                    PasswordHash = hashedPassword,
                    PasswordSalt = salt,
                    CompanyId = companyId,
                    IsActive = true,
                    EmailVerified = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                };

                context.Users.Add(user);
                await context.SaveChangesAsync();

                // Assign role
                var role = await context.Roles.FirstAsync(r => r.Name == roleName);
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedDate = DateTime.Now,
                    AssignedBy = "System",
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                };

                context.UserRoles.Add(userRole);
                await context.SaveChangesAsync();
            }

            logger.LogInformation("Created {UserCount} demo users", demoUsers.Count);
        }

        /// <summary>
        /// Seed sample business data
        /// </summary>
        private static async Task SeedSampleDataAsync(ApplicationDbContext context, ILogger logger)
        {
            // Only seed if no data exists
            if (await context.Items.AnyAsync())
            {
                logger.LogInformation("Sample data already exists, skipping sample data seeding");
                return;
            }

            logger.LogInformation("Seeding sample business data...");

            var defaultCompany = await context.Companies.FirstAsync(c => c.Code == "DEFAULT");

            // Seed sample locations
            var locations = new List<Location>
            {
                new Location { Code = "A-01-01", Name = "Area A Rak 1 Slot 1", MaxCapacity = 100, CompanyId = defaultCompany.Id, CreatedDate = DateTime.Now, CreatedBy = "System" },
                new Location { Code = "A-01-02", Name = "Area A Rak 1 Slot 2", MaxCapacity = 100, CompanyId = defaultCompany.Id, CreatedDate = DateTime.Now, CreatedBy = "System" },
                new Location { Code = "B-01-01", Name = "Area B Rak 1 Slot 1", MaxCapacity = 50, CompanyId = defaultCompany.Id, CreatedDate = DateTime.Now, CreatedBy = "System" },
                new Location { Code = "RECEIVING", Name = "Receiving Area", MaxCapacity = 1000, CompanyId = defaultCompany.Id, CreatedDate = DateTime.Now, CreatedBy = "System" },
                new Location { Code = "SHIPPING", Name = "Shipping Area", MaxCapacity = 1000, CompanyId = defaultCompany.Id, CreatedDate = DateTime.Now, CreatedBy = "System" }
            };
            context.Locations.AddRange(locations);

            // Seed sample supplier
            var supplier = new Supplier
            {
                Name = "PT Supplier Sample",
                Email = "supplier@example.com",
                Phone = "021-1234567",
                Address = "Jakarta",
                CompanyId = defaultCompany.Id,
                CreatedDate = DateTime.Now,
                CreatedBy = "System"
            };
            context.Suppliers.Add(supplier);

            // Seed sample customer
            var customer = new Customer
            {
                Name = "PT Customer Sample",
                Email = "customer@example.com",
                Phone = "021-7654321",
                Address = "Jakarta",
                CompanyId = defaultCompany.Id,
                CreatedDate = DateTime.Now,
                CreatedBy = "System"
            };
            context.Customers.Add(customer);

            // Seed sample items
            var items = new List<Item>
            {
                new Item
                {
                    ItemCode = "ITM001",
                    Name = "Sample Item 1",
                    Unit = "pcs",
                    StandardPrice = 10000,
                    CompanyId = defaultCompany.Id,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                },
                new Item
                {
                    ItemCode = "ITM002",
                    Name = "Sample Item 2",
                    Unit = "kg",
                    StandardPrice = 50000,
                    CompanyId = defaultCompany.Id,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                }
            };
            context.Items.AddRange(items);

            await context.SaveChangesAsync();
            logger.LogInformation("Sample business data seeded successfully");
        }

        /// <summary>
        /// Simple initialization (backward compatibility)
        /// </summary>
        public static void Initialize(ApplicationDbContext context)
        {
            // For backward compatibility - simple synchronous initialization
            context.Database.EnsureCreated();
        }
    }
}