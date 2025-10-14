using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk company management
    /// Includes auto-admin creation logic
    /// </summary>
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public CompanyService(
            ApplicationDbContext context,
            ILogger<CompanyService> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Create new company with auto-generated admin user
        /// AUTO-ADMIN CREATION: Creates Admin user with username pattern: admin{CompanyId}
        /// </summary>
        public async Task<CompanyCreationResult> CreateCompanyWithAdminAsync(CompanyCreateRequest request)
        {
            try
            {
                // Validate company code uniqueness
                if (await IsCompanyCodeExistsAsync(request.Code))
                {
                    return new CompanyCreationResult 
                    { 
                        Success = false, 
                        Message = $"Company code '{request.Code}' already exists" 
                    };
                }

                // Validate company email uniqueness
                if (await IsCompanyEmailExistsAsync(request.Email))
                {
                    return new CompanyCreationResult 
                    { 
                        Success = false, 
                        Message = $"Company email '{request.Email}' already exists" 
                    };
                }

                // Validate admin email uniqueness
                if (await _context.Users.AnyAsync(u => u.Email == request.AdminEmail && !u.IsDeleted))
                {
                    return new CompanyCreationResult 
                    { 
                        Success = false, 
                        Message = $"Admin email '{request.AdminEmail}' already exists" 
                    };
                }

                // 1. Create Company
                var company = new Company
                {
                    Code = request.Code.ToUpper(),
                    Name = request.Name,
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    ContactPerson = request.ContactPerson,
                    TaxNumber = request.TaxNumber,
                    SubscriptionPlan = "Free",
                    MaxUsers = 5,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username ?? "SuperAdmin"
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company created: {CompanyCode} (ID: {CompanyId})", company.Code, company.Id);

                // 2. Create Admin User with pattern: admin{CompanyId}
                var adminUsername = $"admin{company.Id}";
                var tempPassword = GenerateTemporaryPassword();

                var adminUser = new User
                {
                    Username = adminUsername,
                    Email = request.AdminEmail,
                    FullName = request.AdminFullName,
                    HashedPassword = PasswordHelper.HashPassword(tempPassword),
                    Phone = request.AdminPhone,
                    CompanyId = company.Id,
                    IsActive = true,
                    EmailVerified = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = _currentUserService.Username ?? "SuperAdmin"
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin user created: {Username} for company {CompanyCode}", adminUsername, company.Code);

                // 3. Assign Admin Role
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id,
                        AssignedDate = DateTime.Now,
                        AssignedBy = _currentUserService.Username ?? "SuperAdmin",
                        CreatedDate = DateTime.Now,
                        CreatedBy = _currentUserService.Username ?? "SuperAdmin"
                    };

                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Admin role assigned to user {Username}", adminUsername);
                }

                var message = $"Company created successfully. Admin user: {adminUsername}, Temp password: {tempPassword}";
                _logger.LogInformation("Company creation completed: {CompanyCode}", company.Code);

                return new CompanyCreationResult
                {
                    Success = true,
                    Message = message,
                    Company = new CompanyViewModel
                    {
                        Id = company.Id,
                        Code = company.Code,
                        Name = company.Name,
                        Email = company.Email,
                        Phone = company.Phone,
                        Address = company.Address,
                        IsActive = company.IsActive,
                        CreatedDate = company.CreatedDate
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateCompanyWithAdminAsync");
                return new CompanyCreationResult 
                { 
                    Success = false, 
                    Message = $"Error: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// Get all companies (SuperAdmin only)
        /// </summary>
        public async Task<List<CompanyDto>> GetAllCompaniesAsync()
        {
            try
            {
                var companies = await _context.Companies
                    .Include(c => c.Users)
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => new CompanyDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Name = c.Name,
                        Email = c.Email,
                        Phone = c.Phone,
                        ContactPerson = c.ContactPerson,
                        IsActive = c.IsActive,
                        TotalUsers = c.Users.Count(u => u.IsActive),
                        CreatedDate = c.CreatedDate,
                        CreatedBy = c.CreatedBy ?? "System"
                    })
                    .ToListAsync();

                return companies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all companies");
                return new List<CompanyDto>();
            }
        }

        /// <summary>
        /// Get company details with statistics
        /// </summary>
        public async Task<CompanyDetailResponse?> GetCompanyDetailsAsync(int companyId)
        {
            try
            {
                var company = await _context.Companies
                    .Include(c => c.Users)
                    .FirstOrDefaultAsync(c => c.Id == companyId);

                if (company == null)
                    return null;

                var details = new CompanyDetailResponse
                {
                    Id = company.Id,
                    Code = company.Code,
                    Name = company.Name,
                    Email = company.Email,
                    Phone = company.Phone,
                    Address = company.Address,
                    ContactPerson = company.ContactPerson,
                    TaxNumber = company.TaxNumber,
                    SubscriptionPlan = company.SubscriptionPlan,
                    SubscriptionEndDate = company.SubscriptionEndDate,
                    MaxUsers = company.MaxUsers,
                    CurrentUsers = company.Users.Count(u => u.IsActive),
                    IsActive = company.IsActive,
                    CreatedDate = company.CreatedDate,
                    CreatedBy = company.CreatedBy ?? "System",
                    ModifiedDate = company.ModifiedDate,
                    ModifiedBy = company.ModifiedBy,
                    // Statistics
                    TotalItems = await _context.Items.CountAsync(i => i.CompanyId == companyId),
                    TotalLocations = await _context.Locations.CountAsync(l => l.CompanyId == companyId),
                    TotalPurchaseOrders = await _context.PurchaseOrders.CountAsync(po => po.CompanyId == companyId),
                    TotalSalesOrders = await _context.SalesOrders.CountAsync(so => so.CompanyId == companyId)
                };

                return details;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company details for ID: {CompanyId}", companyId);
                return null;
            }
        }

        /// <summary>
        /// Update company information
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateCompanyAsync(CompanyUpdateRequest request)
        {
            try
            {
                var company = await _context.Companies.FindAsync(request.Id);
                if (company == null)
                {
                    return (false, "Company not found");
                }

                // Validate email uniqueness (excluding current company)
                if (await _context.Companies.AnyAsync(c => c.Email == request.Email && c.Id != request.Id))
                {
                    return (false, "Email already exists");
                }

                // Update fields
                company.Name = request.Name;
                company.Email = request.Email;
                company.Phone = request.Phone;
                company.Address = request.Address;
                company.ContactPerson = request.ContactPerson;
                company.TaxNumber = request.TaxNumber;
                company.MaxUsers = request.MaxUsers;
                company.IsActive = request.IsActive;
                company.ModifiedDate = DateTime.Now;
                company.ModifiedBy = _currentUserService.Username ?? "SuperAdmin";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company updated: {CompanyCode}", company.Code);

                return (true, "Company updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company: {CompanyId}", request.Id);
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Deactivate company (soft delete)
        /// </summary>
        public async Task<(bool Success, string Message)> DeactivateCompanyAsync(int companyId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                {
                    return (false, "Company not found");
                }

                company.IsActive = false;
                company.ModifiedDate = DateTime.Now;
                company.ModifiedBy = _currentUserService.Username ?? "SuperAdmin";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company deactivated: {CompanyCode}", company.Code);

                return (true, "Company deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating company: {CompanyId}", companyId);
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Activate company
        /// </summary>
        public async Task<(bool Success, string Message)> ActivateCompanyAsync(int companyId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                {
                    return (false, "Company not found");
                }

                company.IsActive = true;
                company.ModifiedDate = DateTime.Now;
                company.ModifiedBy = _currentUserService.Username ?? "SuperAdmin";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company activated: {CompanyCode}", company.Code);

                return (true, "Company activated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating company: {CompanyId}", companyId);
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if company code exists
        /// </summary>
        public async Task<bool> IsCompanyCodeExistsAsync(string code)
        {
            return await _context.Companies.AnyAsync(c => c.Code == code.ToUpper());
        }

        /// <summary>
        /// Check if company email exists
        /// </summary>
        public async Task<bool> IsCompanyEmailExistsAsync(string email)
        {
            return await _context.Companies.AnyAsync(c => c.Email == email);
        }

        /// <summary>
        /// Get company statistics
        /// </summary>
        public async Task<CompanyDetailResponse?> GetCompanyStatisticsAsync(int companyId)
        {
            return await GetCompanyDetailsAsync(companyId);
        }

        /// <summary>
        /// Generate temporary password for new admin
        /// Pattern: Admin@123 (simple, user should change on first login)
        /// </summary>
        private string GenerateTemporaryPassword()
        {
            return "Admin@123"; // Simple default password - user should change immediately
        }
    }
}

