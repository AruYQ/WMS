using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Data.Repositories;
using WMS.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// Configure Services
// =============================================

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication Configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(
            builder.Configuration.GetValue<int>("AuthenticationSettings:JwtExpirationHours", 8));
        options.SlidingExpiration = true;
        options.Cookie.Name = "WMS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;

        // IMPORTANT: Handle authentication challenges properly
        options.Events.OnRedirectToLogin = context =>
        {
            // For AJAX requests, return 401 instead of redirect
            if (context.Request.Headers.ContainsKey("X-Requested-With") &&
                context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }

            // For regular requests, redirect to login with return URL
            var returnUrl = context.Request.Path + context.Request.QueryString;
            var loginUrl = $"{options.LoginPath}?returnUrl={Uri.EscapeDataString(returnUrl)}";
            context.Response.Redirect(loginUrl);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect(options.AccessDeniedPath);
            return Task.CompletedTask;
        };
    });

// Authorization Configuration - REQUIRE AUTHENTICATION BY DEFAULT
builder.Services.AddAuthorization(options =>
{
    // Set default policy to require authentication
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Define role-based policies
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));

    options.AddPolicy("AllUsers", policy =>
        policy.RequireRole("User", "Manager", "Admin"));
});

// Repository Pattern Registration
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>(); // ADDED THIS LINE
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IASNRepository, ASNRepository>();
builder.Services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();

// Authentication Services - FIXED THE INTERFACE
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>(); // FIXED THIS LINE

// Business Services
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IASNService, ASNService>();
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWarehouseFeeCalculator, WarehouseFeeCalculator>();

// Additional Services
builder.Services.AddHttpContextAccessor();

// Session Configuration (optional, for additional state management)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "WMS.Session";
});

// Memory Cache (useful for caching user permissions, etc.)
builder.Services.AddMemoryCache();

// Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

// =============================================
// Configure Pipeline
// =============================================

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Session middleware (if using sessions)
app.UseSession();

// Routing middleware
app.UseRouting();

// Authentication & Authorization middleware (ORDER IS IMPORTANT!)
app.UseAuthentication();
app.UseAuthorization();

// Route Configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Additional explicit routes for better control
app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "logout",
    pattern: "logout",
    defaults: new { controller = "Account", action = "LogoutGet" });

// Database Initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database initialization...");

        // Ensure database is created
        context.Database.EnsureCreated();

        // Run migrations if needed (uncomment if using migrations)
        // context.Database.Migrate();

        // Initialize seed data with authentication data
        await WMS.Data.DbInitializer.Initialize(context, configuration, logger);

        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database initialization");
        throw; // Re-throw to prevent app startup with invalid DB state
    }
}

app.Run();