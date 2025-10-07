using Microsoft.AspNetCore.Mvc.Rendering;
using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;

namespace WMS.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerRepository customerRepository,
            ICurrentUserService currentUserService,
            ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        #region Basic CRUD Operations

        public async Task<Customer?> GetByIdAsync(int id)
        {
            try
            {
                return await _customerRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            try
            {
                return await _customerRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                throw;
            }
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            try
            {
                // Biarkan ApplicationDbContext handle CompanyId auto-set
                return await _customerRepository.AddAsync(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                throw;
            }
        }

        public async Task<Customer> UpdateAsync(Customer customer)
        {
            try
            {
                return await _customerRepository.UpdateAsync(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {CustomerId}", customer.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                return await _customerRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {CustomerId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _customerRepository.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking customer existence: {CustomerId}", id);
                throw;
            }
        }

        #endregion

        #region Search and Filter Operations

        public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
        {
            try
            {
                return await _customerRepository.SearchCustomersAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
        {
            try
            {
                return await _customerRepository.GetActiveCustomersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active customers");
                throw;
            }
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithSalesOrdersAsync()
        {
            try
            {
                return await _customerRepository.GetAllWithSalesOrdersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers with sales orders");
                throw;
            }
        }

        #endregion

        #region ViewModel Operations

        public async Task<CustomerViewModel> GetCustomerViewModelAsync(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    return new CustomerViewModel();
                }

                return new CustomerViewModel
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    IsActive = customer.IsActive,
                    CreatedBy = customer.CreatedBy,
                    CreatedDate = customer.CreatedDate,
                    ModifiedBy = customer.ModifiedBy,
                    ModifiedDate = customer.ModifiedDate,
                    SalesOrderCount = customer.SalesOrders?.Count ?? 0,
                    TotalOrderValue = customer.SalesOrders?.Sum(so => so.GrandTotal) ?? 0,
                    LastOrderDate = customer.SalesOrders?.Any() == true ? customer.SalesOrders.Max(so => so.OrderDate) : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer view model: {CustomerId}", id);
                throw;
            }
        }

        public async Task<CustomerViewModel> PopulateCustomerViewModelAsync(CustomerViewModel viewModel)
        {
            try
            {
                // No additional data needed for customer view model
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating customer view model");
                throw;
            }
        }

        public async Task<CustomerIndexViewModel> GetCustomerIndexViewModelAsync(string? searchTerm = null, bool? isActive = null)
        {
            try
            {
                _logger.LogInformation("=== CUSTOMER SERVICE DEBUG START ===");
                _logger.LogInformation("GetCustomerIndexViewModelAsync - SearchTerm: {SearchTerm}, IsActive: {IsActive}", searchTerm, isActive);
                
                var companyId = _currentUserService.CompanyId;
                _logger.LogInformation("CustomerService - Current CompanyId: {CompanyId}", companyId);

                IEnumerable<Customer> customers;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    _logger.LogInformation("Searching customers with term: {SearchTerm}", searchTerm);
                    customers = await _customerRepository.SearchCustomersAsync(searchTerm);
                }
                else if (isActive.HasValue)
                {
                    _logger.LogInformation("Filtering customers by active status: {IsActive}", isActive);
                    if (isActive.Value)
                        customers = await _customerRepository.GetActiveCustomersAsync();
                    else
                        customers = (await _customerRepository.GetAllAsync()).Where(c => !c.IsActive);
                }
                else
                {
                    _logger.LogInformation("Getting all customers for company: {CompanyId}", companyId);
                    customers = await _customerRepository.GetAllAsync();
                }

                _logger.LogInformation("Found {Count} customers from repository", customers.Count());

                var customerViewModels = customers.Select(c => new CustomerViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    Address = c.Address,
                    IsActive = c.IsActive,
                    CreatedBy = c.CreatedBy,
                    CreatedDate = c.CreatedDate,
                    ModifiedBy = c.ModifiedBy,
                    ModifiedDate = c.ModifiedDate,
                    SalesOrderCount = c.SalesOrders?.Count ?? 0,
                    TotalOrderValue = c.SalesOrders?.Sum(so => so.GrandTotal) ?? 0,
                    LastOrderDate = c.SalesOrders?.Any() == true ? c.SalesOrders.Max(so => so.OrderDate) : null
                }).OrderBy(c => c.Name).ToList();

                _logger.LogInformation("Converted to {Count} customer view models", customerViewModels.Count);

                // Get statistics
                _logger.LogInformation("Getting statistics...");
                var allCustomers = await _customerRepository.GetAllAsync();
                var activeCustomers = await _customerRepository.GetActiveCustomersAsync();
                var customersWithOrders = await _customerRepository.GetAllWithSalesOrdersAsync();

                _logger.LogInformation("Statistics - AllCustomers: {AllCount}, ActiveCustomers: {ActiveCount}, WithOrders: {WithOrdersCount}", 
                    allCustomers.Count(), activeCustomers.Count(), customersWithOrders.Count(c => c.SalesOrders.Any()));

                var result = new CustomerIndexViewModel
                {
                    Customers = customerViewModels,
                    SearchTerm = searchTerm,
                    IsActive = isActive,
                    TotalCustomers = allCustomers.Count(),
                    ActiveCustomers = activeCustomers.Count(),
                    InactiveCustomers = allCustomers.Count() - activeCustomers.Count(),
                    CustomersWithOrders = customersWithOrders.Count(c => c.SalesOrders.Any())
                };

                _logger.LogInformation("=== CUSTOMER SERVICE DEBUG END ===");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer index view model - Exception: {Message}", ex.Message);
                _logger.LogError(ex, "Stack Trace: {StackTrace}", ex.StackTrace);
                throw;
            }
        }

        public async Task<CustomerDetailsViewModel> GetCustomerDetailsViewModelAsync(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    return new CustomerDetailsViewModel();
                }

                var salesOrderSummaries = customer.SalesOrders?.Select(so => new SalesOrderSummary
                {
                    Id = so.Id,
                    SONumber = so.SONumber,
                    OrderDate = so.OrderDate,
                    RequiredDate = so.RequiredDate,
                    Status = so.Status,
                    TotalAmount = so.TotalAmount,
                    GrandTotal = so.GrandTotal,
                    TotalQuantity = so.SalesOrderDetails?.Sum(sod => sod.Quantity) ?? 0,
                    TotalItemTypes = so.SalesOrderDetails?.Count ?? 0
                }).ToList() ?? new List<SalesOrderSummary>();

                return new CustomerDetailsViewModel
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    IsActive = customer.IsActive,
                    CreatedBy = customer.CreatedBy,
                    CreatedDate = customer.CreatedDate,
                    ModifiedBy = customer.ModifiedBy,
                    ModifiedDate = customer.ModifiedDate,
                    SalesOrders = salesOrderSummaries,
                    SalesOrderCount = salesOrderSummaries.Count,
                    TotalOrderValue = salesOrderSummaries.Sum(so => so.GrandTotal),
                    LastOrderDate = salesOrderSummaries.Any() ? salesOrderSummaries.Max(so => (DateTime?)so.OrderDate) : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer details view model: {CustomerId}", id);
                throw;
            }
        }

        public async Task<CustomerSummaryViewModel> GetCustomerSummaryAsync()
        {
            try
            {
                var allCustomers = await _customerRepository.GetAllAsync();
                var activeCustomers = await _customerRepository.GetActiveCustomersAsync();
                var customersWithOrders = await _customerRepository.GetAllWithSalesOrdersAsync();

                return new CustomerSummaryViewModel
                {
                    TotalCustomers = allCustomers.Count(),
                    ActiveCustomers = activeCustomers.Count(),
                    InactiveCustomers = allCustomers.Count() - activeCustomers.Count(),
                    CustomersWithOrders = customersWithOrders.Count(c => c.SalesOrders.Any()),
                    TotalOrderValue = customersWithOrders.Sum(c => c.SalesOrders?.Sum(so => so.GrandTotal) ?? 0),
                    AverageOrderValue = customersWithOrders.Any() 
                        ? customersWithOrders.Average(c => c.SalesOrders?.Sum(so => so.GrandTotal) ?? 0)
                        : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer summary");
                throw;
            }
        }

        #endregion

        #region Validation Operations

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
        {
            try
            {
                return await _customerRepository.ExistsByEmailAsync(email, excludeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null)
        {
            try
            {
                return await _customerRepository.ExistsByPhoneAsync(phone, excludeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking phone existence: {Phone}", phone);
                throw;
            }
        }

        #endregion

        #region Statistics Operations

        public async Task<Dictionary<string, object>> GetCustomerStatisticsAsync()
        {
            try
            {
                var allCustomers = await _customerRepository.GetAllAsync();
                var activeCustomers = await _customerRepository.GetActiveCustomersAsync();
                var customersWithOrders = await _customerRepository.GetAllWithSalesOrdersAsync();

                return new Dictionary<string, object>
                {
                    ["TotalCustomers"] = allCustomers.Count(),
                    ["ActiveCustomers"] = activeCustomers.Count(),
                    ["InactiveCustomers"] = allCustomers.Count() - activeCustomers.Count(),
                    ["CustomersWithOrders"] = customersWithOrders.Count(c => c.SalesOrders.Any()),
                    ["TotalOrderValue"] = customersWithOrders.Sum(c => c.SalesOrders?.Sum(so => so.GrandTotal) ?? 0),
                    ["AverageOrderValue"] = customersWithOrders.Any() 
                        ? customersWithOrders.Average(c => c.SalesOrders?.Sum(so => so.GrandTotal) ?? 0)
                        : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer statistics");
                throw;
            }
        }

        #endregion

        #region AJAX Operations

        public async Task<bool> CheckEmailExistsAsync(string email, int? excludeId = null)
        {
            try
            {
                return await _customerRepository.ExistsByEmailAsync(email, excludeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email existence: {Email}", email);
                throw;
            }
        }

        public async Task<IEnumerable<object>> SearchCustomersForAjaxAsync(string searchTerm)
        {
            try
            {
                var customers = await _customerRepository.SearchCustomersAsync(searchTerm);
                return customers.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    email = c.Email,
                    phone = c.Phone,
                    isActive = c.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers for AJAX: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<object> GetCustomerDetailsForAjaxAsync(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdWithSalesOrdersAsync(id);
                if (customer == null)
                {
                    return new { error = "Customer not found" };
                }

                return new
                {
                    id = customer.Id,
                    name = customer.Name,
                    email = customer.Email,
                    phone = customer.Phone,
                    address = customer.Address,
                    isActive = customer.IsActive,
                    salesOrderCount = customer.SalesOrders?.Count ?? 0,
                    totalOrderValue = customer.SalesOrders?.Sum(so => so.GrandTotal) ?? 0,
                    lastOrderDate = customer.SalesOrders?.Any() == true ? customer.SalesOrders.Max(so => (DateTime?)so.OrderDate) : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer details for AJAX: {CustomerId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CustomerPerformanceViewModel>> GetPerformanceDataAsync()
        {
            try
            {
                var customersWithOrders = await _customerRepository.GetAllWithSalesOrdersAsync();

                return customersWithOrders.Select(c => new CustomerPerformanceViewModel
                {
                    CustomerId = c.Id,
                    CustomerName = c.Name,
                    CustomerEmail = c.Email,
                    TotalOrders = c.SalesOrders?.Count ?? 0,
                    TotalValue = c.SalesOrders?.Sum(so => so.GrandTotal) ?? 0,
                    AverageOrderValue = c.SalesOrders?.Any() == true 
                        ? c.SalesOrders.Average(so => so.GrandTotal) 
                        : 0,
                    LastOrderDate = c.SalesOrders?.Any() == true ? c.SalesOrders.Max(so => (DateTime?)so.OrderDate) : null,
                    FirstOrderDate = c.SalesOrders?.Min(so => (DateTime?)so.OrderDate),
                    DaysSinceLastOrder = c.SalesOrders?.Any() == true 
                        ? (DateTime.Now - c.SalesOrders.Max(so => so.OrderDate)).Days 
                        : 0,
                    IsActive = c.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance data");
                throw;
            }
        }

        public async Task<CustomerPerformanceReportViewModel> GetPerformanceReportViewModelAsync()
        {
            try
            {
                var performanceData = await GetPerformanceDataAsync();
                var orderedData = performanceData.OrderByDescending(c => c.TotalValue).ToList();

                return new CustomerPerformanceReportViewModel
                {
                    Customers = orderedData,
                    TotalCustomers = orderedData.Count,
                    TotalRevenue = orderedData.Sum(c => c.TotalValue),
                    AverageRevenuePerCustomer = orderedData.Any() ? orderedData.Average(c => c.TotalValue) : 0,
                    TotalOrders = orderedData.Sum(c => c.TotalOrders),
                    AverageOrderValue = orderedData.Any() ? orderedData.Average(c => c.AverageOrderValue) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance report view model");
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetCustomersForExportAsync()
        {
            try
            {
                var customers = await _customerRepository.GetAllWithSalesOrdersAsync();

                return customers.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    email = c.Email,
                    phone = c.Phone,
                    address = c.Address,
                    isActive = c.IsActive,
                    createdDate = c.CreatedDate,
                    modifiedDate = c.ModifiedDate,
                    salesOrderCount = c.SalesOrders?.Count ?? 0,
                    totalOrderValue = c.SalesOrders?.Sum(so => so.GrandTotal) ?? 0,
                    lastOrderDate = c.SalesOrders?.Any() == true ? c.SalesOrders.Max(so => (DateTime?)so.OrderDate) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers for export");
                throw;
            }
        }

        #endregion
    }
}
