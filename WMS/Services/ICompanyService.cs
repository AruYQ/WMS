using WMS.Models;
using WMS.Models.ViewModels;

namespace WMS.Services
{
    /// <summary>
    /// Interface untuk Company Service
    /// Handles company management and auto-admin creation (SuperAdmin only)
    /// </summary>
    public interface ICompanyService
    {
        /// <summary>
        /// Create new company with auto-generated admin user
        /// </summary>
        Task<CompanyCreationResult> CreateCompanyWithAdminAsync(CompanyCreateRequest request);

        /// <summary>
        /// Get all companies (for SuperAdmin)
        /// </summary>
        Task<List<CompanyDto>> GetAllCompaniesAsync();

        /// <summary>
        /// Get company by ID with details
        /// </summary>
        Task<CompanyDetailResponse?> GetCompanyDetailsAsync(int companyId);

        /// <summary>
        /// Update company information
        /// </summary>
        Task<(bool Success, string Message)> UpdateCompanyAsync(CompanyUpdateRequest request);

        /// <summary>
        /// Deactivate company (soft delete)
        /// </summary>
        Task<(bool Success, string Message)> DeactivateCompanyAsync(int companyId);

        /// <summary>
        /// Activate company
        /// </summary>
        Task<(bool Success, string Message)> ActivateCompanyAsync(int companyId);

        /// <summary>
        /// Check if company code already exists
        /// </summary>
        Task<bool> IsCompanyCodeExistsAsync(string code);

        /// <summary>
        /// Check if company email already exists
        /// </summary>
        Task<bool> IsCompanyEmailExistsAsync(string email);

        /// <summary>
        /// Get company statistics
        /// </summary>
        Task<CompanyDetailResponse?> GetCompanyStatisticsAsync(int companyId);
    }
}

