using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Data.Repositories;
using WMS.Services;
using WMS.Middleware;
using WMS.Configuration;
using WMS.Utilities;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// Configure Services
// =============================================

// Add controllers with views
builder.Services.AddControllersWithViews();

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =============================================
// Configuration Settings Registration
// =============================================

// Bind AuthenticationSettings from appsettings.json
builder.Services.Configure<AuthenticationSettings>(
    builder.Configuration.GetSection("AuthenticationSettings"));

// Create and register JwtSettings (mapped from AuthenticationSettings)
builder.Services.AddSingleton<JwtSettings>(serviceProvider =>
{
    var config = builder.Configuration.GetSection("AuthenticationSettings");
    return new JwtSettings
    {
        SecretKey = config["JwtSecretKey"] ?? "default-secret-key",
        Issuer = config["JwtIssuer"] ?? "WMS-Application",
        Audience = config["JwtAudience"] ?? "WMS-Users",
        ExpirationHours = config.GetValue<int>("JwtExpirationHours", 8),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

// Register TokenHelper - THIS WAS MISSING
builder.Services.AddScoped<TokenHelper>();

// Authentication Configuration
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(
            builder.Configuration.GetValue<int>("AuthenticationSettings:CookieExpirationHours", 8));
        options.SlidingExpiration = builder.Configuration.GetValue<bool>("AuthenticationSettings:SlidingExpiration", true);
        options.Cookie.Name = builder.Configuration.GetValue<string>("AuthenticationSettings:CookieName", "WMS.Auth");
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;

        // Handle authentication challenges properly
        options.Events.OnRedirectToLogin = context =>
        {
            // For AJAX requests, return 401 instead of redirect
            if (context.Request.Headers.ContainsKey("X-Requested-With") &&
                context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }

            // For API calls, return 401
            if (context.Request.Path.StartsWithSegments("/api"))
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
            // For AJAX/API requests, return 403
            if (context.Request.Headers.ContainsKey("X-Requested-With") ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }

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
        policy.RequireRole("Admin", "SuperAdmin"));

    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin", "SuperAdmin"));

    options.AddPolicy("AllUsers", policy =>
        policy.RequireRole("User", "Operator", "Supervisor", "Manager", "Admin", "SuperAdmin"));

    // Company-specific policies (optional for now)
    options.AddPolicy("RequireCompany", policy =>
        policy.RequireAuthenticatedUser());
});

// Repository Pattern Registration
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IASNRepository, ASNRepository>();
builder.Services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();

// Core Authentication Services - FIXED INTERFACE NAMES
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<WMSIAuthenticationService, WMSAuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();

// Business Services
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IASNService, ASNService>();
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWarehouseFeeCalculator, WarehouseFeeCalculator>();

// Infrastructure Services
builder.Services.AddHttpContextAccessor();

// Session Configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(
        builder.Configuration.GetValue<int>("AuthenticationSettings:SessionTimeoutHours", 2));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "WMS.Session";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Memory Cache
builder.Services.AddMemoryCache();

// Logging Configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

var app = builder.Build();

// =============================================
// Configure Pipeline
// =============================================

// Exception handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Custom middleware order is IMPORTANT!
app.UseHttpsRedirection();
app.UseStaticFiles();

// Custom middleware (will be enabled when implemented)
// app.UseMiddleware<ExceptionHandlingMiddleware>();
// app.UseMiddleware<RequestLoggingMiddleware>();

// Session middleware
app.UseSession();

// Routing middleware
app.UseRouting();

// Authentication & Authorization middleware (ORDER IS CRITICAL!)
app.UseAuthentication();
// app.UseMiddleware<CompanyContextMiddleware>(); // Enable when implemented
app.UseAuthorization();

// Route Configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Authentication routes
app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "logout",
    pattern: "logout",
    defaults: new { controller = "Account", action = "Logout" });

app.MapControllerRoute(
    name: "forgot-password",
    pattern: "forgot-password",
    defaults: new { controller = "Account", action = "ForgotPassword" });

// User management routes
app.MapControllerRoute(
    name: "user-profile",
    pattern: "profile",
    defaults: new { controller = "User", action = "Profile" });

// Database Initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Ensure database is created and up-to-date
        context.Database.EnsureCreated();
        // Or use: context.Database.Migrate();

        // Seed dummy data
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database");
    }
}

app.Run();