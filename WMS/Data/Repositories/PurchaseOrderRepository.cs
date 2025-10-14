using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Utilities;
using WMS.Services;

namespace WMS.Data.Repositories
{
    public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
    {
        public PurchaseOrderRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<Repository<PurchaseOrder>> logger) : base(context, currentUserService, logger)
        {
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllWithDetailsAsync()
        {
            return await GetBaseQuery()
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderDetails)
                    .ThenInclude(pod => pod.Item)
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();
        }

        public async Task<PurchaseOrder?> GetByIdWithDetailsAsync(int id)
        {
            return await GetBaseQuery()
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderDetails)
                    .ThenInclude(pod => pod.Item)
                .Include(po => po.AdvancedShippingNotices)
                .FirstOrDefaultAsync(po => po.Id == id);
        }

        public async Task<IEnumerable<PurchaseOrder>> GetBySupplierAsync(int supplierId)
        {
            return await GetBaseQuery()
                .Include(po => po.Supplier)
                .Where(po => po.SupplierId == supplierId)
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status)
        {
            return await GetBaseQuery()
                .Include(po => po.Supplier)
                .Where(po => po.Status == status.ToString())
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrder>> GetPendingPurchaseOrdersAsync()
        {
            return await GetBaseQuery()
                .Include(po => po.Supplier)
                .Where(po => po.Status == PurchaseOrderStatus.Draft.ToString() ||
                           po.Status == PurchaseOrderStatus.Sent.ToString())
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> ExistsByPONumberAsync(string poNumber)
        {
            return await GetBaseQuery().AnyAsync(po => po.PONumber == poNumber);
        }

        public async Task<string> GenerateNextPONumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"PO-{today:yyyy-MM-dd}-";

            var lastPO = await GetBaseQuery()
                .Where(po => po.PONumber.StartsWith(prefix))
                .OrderByDescending(po => po.PONumber)
                .FirstOrDefaultAsync();

            if (lastPO != null)
            {
                var lastNumber = lastPO.PONumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int number))
                {
                    return $"{prefix}{(number + 1):D3}";
                }
            }

            return $"{prefix}001";
        }

        public async Task<PurchaseOrder> CreateWithDetailsAsync(PurchaseOrder purchaseOrder)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generate PO Number if not provided
                if (string.IsNullOrEmpty(purchaseOrder.PONumber))
                {
                    purchaseOrder.PONumber = await GenerateNextPONumberAsync();
                }

                // Calculate total amount
                purchaseOrder.TotalAmount = purchaseOrder.PurchaseOrderDetails.Sum(d => d.TotalPrice);

                await AddAsync(purchaseOrder);

                await transaction.CommitAsync();
                return purchaseOrder;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateStatusAsync(int id, PurchaseOrderStatus status)
        {
            var purchaseOrder = await GetByIdAsync(id);
            if (purchaseOrder != null)
            {
                purchaseOrder.Status = status.ToString();

                if (status == PurchaseOrderStatus.Sent)
                {
                    purchaseOrder.EmailSent = true;
                    purchaseOrder.EmailSentDate = DateTime.Now;
                }

                await UpdateAsync(purchaseOrder);
            }
        }

        public new async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                    return false;

                return await DeleteAsync(entity);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IEnumerable<PurchaseOrder>> SearchAsync(WMS.Models.ViewModels.PurchaseOrderSearchRequest request)
        {
            try
            {
                var query = GetBaseQuery()
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                    .ThenInclude(pod => pod.Item)
                    .AsQueryable();

                // Filter berdasarkan search text (PONumber atau Supplier Name)
                if (!string.IsNullOrWhiteSpace(request.SearchText))
                {
                    var searchTerm = request.SearchText.ToLower();
                    query = query.Where(po => 
                        po.PONumber.ToLower().Contains(searchTerm) ||
                        po.Supplier.Name.ToLower().Contains(searchTerm));
                }

                // Filter berdasarkan nama supplier
                if (!string.IsNullOrWhiteSpace(request.SupplierNameFilter))
                {
                    var supplierName = request.SupplierNameFilter.ToLower();
                    query = query.Where(po => po.Supplier.Name.ToLower().Contains(supplierName));
                }

                // Filter berdasarkan phone supplier
                if (!string.IsNullOrWhiteSpace(request.PhoneFilter))
                {
                    var phone = request.PhoneFilter.ToLower();
                    query = query.Where(po => po.Supplier.Phone != null && 
                        po.Supplier.Phone.ToLower().Contains(phone));
                }

                // Filter berdasarkan PO Number
                if (!string.IsNullOrWhiteSpace(request.PONumberFilter))
                {
                    var poNumber = request.PONumberFilter.ToLower();
                    query = query.Where(po => po.PONumber.ToLower().Contains(poNumber));
                }

                // Filter berdasarkan status
                if (!string.IsNullOrWhiteSpace(request.POStatusFilter))
                {
                    query = query.Where(po => po.Status == request.POStatusFilter);
                }

                // Filter berdasarkan tanggal
                if (request.DateFrom.HasValue)
                {
                    query = query.Where(po => po.OrderDate >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    query = query.Where(po => po.OrderDate <= request.DateTo.Value);
                }

                // Order by tanggal terbaru
                query = query.OrderByDescending(po => po.OrderDate);

                // Pagination
                if (request.Page > 0 && request.PageSize > 0)
                {
                    query = query.Skip((request.Page - 1) * request.PageSize)
                                .Take(request.PageSize);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching purchase orders");
                throw;
            }
        }

        public async Task<IEnumerable<PurchaseOrder>> QuickSearchAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return await GetBaseQuery()
                        .Include(po => po.Supplier)
                        .OrderByDescending(po => po.OrderDate)
                        .Take(10)
                        .ToListAsync();
                }

                var searchTerm = query.ToLower();
                return await GetBaseQuery()
                    .Include(po => po.Supplier)
                    .Where(po => 
                        po.PONumber.ToLower().Contains(searchTerm) ||
                        po.Supplier.Name.ToLower().Contains(searchTerm))
                    .OrderByDescending(po => po.OrderDate)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick search purchase orders");
                throw;
            }
        }
    }
}