using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.Attributes;
using WMS.Data;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller for Company management (SuperAdmin only)
    /// Hybrid MVC + API pattern with AJAX
    /// </summary>
    [RequirePermission(Constants.COMPANY_MANAGE)]
    public class CompanyController : Controller
    {
        private readonly ICompanyService _companyService;
        private readonly IAuditTrailService _auditService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(
            ICompanyService companyService,
            IAuditTrailService auditService,
            ApplicationDbContext context,
            ILogger<CompanyController> logger)
        {
            _companyService = companyService;
            _auditService = auditService;
            _context = context;
            _logger = logger;
        }

        #region MVC Actions

        /// <summary>
        /// GET: /Company
        /// Company management index page (SuperAdmin only)
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        #endregion

        #region API Endpoints

        /// <summary>
        /// GET: api/company
        /// Get all companies
        /// </summary>
        [HttpGet("api/company")]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _companyService.GetAllCompaniesAsync();
                return Ok(new { success = true, data = companies });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies");
                return StatusCode(500, new { success = false, message = "Error loading companies" });
            }
        }

        /// <summary>
        /// GET: api/company/{id}
        /// Get company details
        /// </summary>
        [HttpGet("api/company/{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            try
            {
                var company = await _companyService.GetCompanyDetailsAsync(id);
                if (company == null)
                {
                    return NotFound(new { success = false, message = "Company not found" });
                }

                return Ok(new { success = true, data = company });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company {CompanyId}", id);
                return StatusCode(500, new { success = false, message = "Error loading company details" });
            }
        }

        /// <summary>
        /// POST: api/company
        /// Create new company with auto-admin
        /// </summary>
        [HttpPost("api/company")]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyCreateRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var result = await _companyService.CreateCompanyWithAdminAsync(request);

                if (result.Success)
                {
                    await transaction.CommitAsync();
                    
                    // Log audit after transaction is committed
                    try
                    {
                        await _auditService.LogActionAsync("CREATE", "Company", result.Company?.Id, 
                            $"{result.Company?.Code} - {result.Company?.Name}", null, result.Company);
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogWarning(auditEx, "Failed to log audit trail for company creation");
                    }

                    return Ok(new { success = true, message = result.Message, data = result.Company });
                }

                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating company: {Error}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    message = $"Error creating company: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// PUT: api/company/{id}
        /// Update company
        /// </summary>
        [HttpPut("api/company/{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] CompanyUpdateRequest request)
        {
            try
            {
                if (id != request.Id)
                {
                    return BadRequest(new { success = false, message = "ID mismatch" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
                }

                var result = await _companyService.UpdateCompanyAsync(request);

                if (result.Success)
                {
                    await _auditService.LogActionAsync("UPDATE", "Company", id, request.Name);
                    return Ok(new { success = true, message = result.Message });
                }

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                return StatusCode(500, new { success = false, message = "Error updating company" });
            }
        }

        /// <summary>
        /// DELETE: api/company/{id}
        /// Deactivate company (soft delete)
        /// </summary>
        [HttpDelete("api/company/{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            try
            {
                var result = await _companyService.DeactivateCompanyAsync(id);

                if (result.Success)
                {
                    await _auditService.LogActionAsync("DELETE", "Company", id, "Deactivated");
                    return Ok(new { success = true, message = result.Message });
                }

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {CompanyId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting company" });
            }
        }

        /// <summary>
        /// POST: api/company/{id}/activate
        /// Activate company
        /// </summary>
        [HttpPost("api/company/{id}/activate")]
        public async Task<IActionResult> ActivateCompany(int id)
        {
            try
            {
                var result = await _companyService.ActivateCompanyAsync(id);

                if (result.Success)
                {
                    await _auditService.LogActionAsync("UPDATE", "Company", id, "Activated");
                    return Ok(new { success = true, message = result.Message });
                }

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating company {CompanyId}", id);
                return StatusCode(500, new { success = false, message = "Error activating company" });
            }
        }

        #endregion
    }
}

