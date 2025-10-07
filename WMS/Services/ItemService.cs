using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service implementation untuk Item management
    /// Mengelola master data barang/item yang disimpan di warehouse
    /// </summary>
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ItemService> _logger;

        public ItemService(
            IItemRepository itemRepository,
            IInventoryRepository inventoryRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            ISalesOrderRepository salesOrderRepository,
            ICurrentUserService currentUserService,
            ILogger<ItemService> logger)
        {
            _itemRepository = itemRepository;
            _inventoryRepository = inventoryRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _salesOrderRepository = salesOrderRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<Item>> GetAllItemsAsync()
        {
            try
            {
                return await _itemRepository.GetAllWithInventoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all items for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<Item?> GetItemByIdAsync(int id)
        {
            try
            {
                return await _itemRepository.GetByIdWithInventoryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item {ItemId} for company {CompanyId}", id, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<Item> CreateItemAsync(Item item)
        {
            try
            {
                if (!await ValidateItemAsync(item))
                    throw new InvalidOperationException("Item validation failed");

                if (!await IsItemCodeUniqueAsync(item.ItemCode))
                    throw new InvalidOperationException($"Item code '{item.ItemCode}' already exists");

                item.CreatedDate = DateTime.Now;
                var result = await _itemRepository.AddAsync(item);

                _logger.LogInformation("Created item {ItemCode} for company {CompanyId}", item.ItemCode, _currentUserService.CompanyId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item {ItemCode} for company {CompanyId}", item.ItemCode, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<Item> UpdateItemAsync(int id, Item item)
        {
            try
            {
                var existingItem = await _itemRepository.GetByIdAsync(id);
                if (existingItem == null)
                    throw new ArgumentException($"Item with ID {id} not found");

                if (!await IsItemCodeUniqueAsync(item.ItemCode, id))
                    throw new InvalidOperationException($"Item code '{item.ItemCode}' already exists");

                if (!await ValidateItemAsync(item))
                    throw new InvalidOperationException("Item validation failed");

                existingItem.ItemCode = item.ItemCode;
                existingItem.Name = item.Name;
                existingItem.Description = item.Description;
                existingItem.Unit = item.Unit;
                existingItem.StandardPrice = item.StandardPrice;
                existingItem.SupplierId = item.SupplierId;
                existingItem.IsActive = item.IsActive;
                existingItem.ModifiedDate = DateTime.Now;

                var result = await _itemRepository.UpdateAsync(existingItem);

                _logger.LogInformation("Updated item {ItemCode} for company {CompanyId}", item.ItemCode, _currentUserService.CompanyId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId} for company {CompanyId}", id, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            try
            {
                if (!await CanDeleteItemAsync(id))
                {
                    _logger.LogWarning("Cannot delete item {ItemId} - item is in use", id);
                    return false;
                }

                var result = await _itemRepository.DeleteAsync(id);

                if (result)
                    _logger.LogInformation("Deleted item {ItemId} for company {CompanyId}", id, _currentUserService.CompanyId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId} for company {CompanyId}", id, _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Query Operations

        public async Task<IEnumerable<Item>> GetActiveItemsAsync()
        {
            try
            {
                return await _itemRepository.GetActiveItemsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active items for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<Item?> GetItemByCodeAsync(string itemCode)
        {
            try
            {
                return await _itemRepository.GetByItemCodeAsync(itemCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item by code {ItemCode} for company {CompanyId}", itemCode, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm)
        {
            try
            {
                return await _itemRepository.SearchItemsAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items with term '{SearchTerm}' for company {CompanyId}", searchTerm, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsWithInventoryAsync()
        {
            try
            {
                return await _itemRepository.GetAllWithInventoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items with inventory for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10)
        {
            try
            {
                return await _itemRepository.GetItemsWithLowStockAsync(threshold);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items with low stock for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Supplier-related Operations

        public async Task<IEnumerable<Item>> GetItemsBySupplierAsync(int supplierId)
        {
            try
            {
                return await _itemRepository.GetBySupplierIdAsync(supplierId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for supplier {SupplierId} for company {CompanyId}", supplierId, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsWithoutSupplierAsync()
        {
            try
            {
                return await _itemRepository.GetItemsWithoutSupplierAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items without supplier for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> SearchItemsBySupplierAsync(string searchTerm, int supplierId)
        {
            try
            {
                return await _itemRepository.SearchItemsBySupplierAsync(searchTerm, supplierId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items by supplier {SupplierId} with term '{SearchTerm}' for company {CompanyId}",
                    supplierId, searchTerm, _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Validation Operations

        public async Task<bool> IsItemCodeUniqueAsync(string itemCode, int? excludeId = null)
        {
            try
            {
                var existingItem = await _itemRepository.GetByItemCodeAsync(itemCode);
                return existingItem == null || (excludeId.HasValue && existingItem.Id == excludeId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking item code uniqueness for {ItemCode} for company {CompanyId}", itemCode, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<bool> ValidateItemAsync(Item item)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(item.ItemCode) ||
                    string.IsNullOrWhiteSpace(item.Name) ||
                    string.IsNullOrWhiteSpace(item.Unit))
                    return false;

                if (item.StandardPrice < 0)
                    return false;

                // Business rules validation
                if (item.ItemCode.Length > 50 || item.Name.Length > 200)
                    return false;

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating item {ItemCode} for company {CompanyId}", item.ItemCode, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<bool> CanDeleteItemAsync(int id)
        {
            try
            {
                // Check if item is used in any purchase orders
                var allPOs = await _purchaseOrderRepository.GetAllWithDetailsAsync();
                var isUsedInPO = allPOs.Any(po => po.PurchaseOrderDetails.Any(d => d.ItemId == id));

                if (isUsedInPO)
                    return false;

                // Check if item is used in any sales orders
                var allSOs = await _salesOrderRepository.GetAllWithDetailsAsync();
                var isUsedInSO = allSOs.Any(so => so.SalesOrderDetails.Any(d => d.ItemId == id));

                if (isUsedInSO)
                    return false;

                // Check if item has inventory
                var inventories = await _inventoryRepository.GetByItemIdAsync(id);
                var hasInventory = inventories.Any(inv => inv.Quantity > 0);

                return !hasInventory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item {ItemId} can be deleted for company {CompanyId}", id, _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Stock Information

        public async Task<Dictionary<int, int>> GetItemStockSummaryAsync()
        {
            try
            {
                return await _itemRepository.GetItemStockSummaryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item stock summary for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<int> GetItemTotalStockAsync(int itemId)
        {
            try
            {
                var inventories = await _inventoryRepository.GetByItemIdAsync(itemId);
                return inventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                                  .Sum(inv => inv.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total stock for item {ItemId} for company {CompanyId}", itemId, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<decimal> GetItemTotalValueAsync(int itemId)
        {
            try
            {
                var inventories = await _inventoryRepository.GetByItemIdAsync(itemId);
                return inventories.Sum(inv => inv.TotalValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total value for item {ItemId} for company {CompanyId}", itemId, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetItemInventoryDetailsAsync(int itemId)
        {
            try
            {
                var inventories = await _inventoryRepository.GetByItemIdAsync(itemId);

                return inventories.Select(inv => new
                {
                    LocationCode = inv.Location.Code,
                    LocationName = inv.Location.Name,
                    Quantity = inv.Quantity,
                    Status = inv.StatusIndonesia,
                    LastCostPrice = inv.LastCostPrice,
                    TotalValue = inv.TotalValue,
                    LastUpdated = inv.LastUpdated,
                    StockLevel = inv.StockLevel
                }).OrderByDescending(x => x.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory details for item {ItemId} for company {CompanyId}", itemId, _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Business Logic Operations

        public async Task<bool> UpdateItemStatusAsync(int id, bool isActive)
        {
            try
            {
                var item = await _itemRepository.GetByIdAsync(id);
                if (item == null)
                    return false;

                item.IsActive = isActive;
                item.ModifiedDate = DateTime.Now;

                await _itemRepository.UpdateAsync(item);

                _logger.LogInformation("Updated item {ItemId} status to {IsActive} for company {CompanyId}", id, isActive, _currentUserService.CompanyId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for item {ItemId} for company {CompanyId}", id, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsForPurchaseOrderAsync()
        {
            try
            {
                return await GetActiveItemsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for purchase order for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsForSalesOrderAsync()
        {
            try
            {
                // Only return items that have available inventory
                var activeItems = await GetActiveItemsAsync();
                var itemsWithStock = new List<Item>();

                foreach (var item in activeItems)
                {
                    var totalStock = await GetItemTotalStockAsync(item.Id);
                    if (totalStock > 0)
                    {
                        itemsWithStock.Add(item);
                    }
                }

                return itemsWithStock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items for sales order for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> GetAvailableItemsAsync()
        {
            try
            {
                return await GetItemsForSalesOrderAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available items for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Reporting Operations

        public async Task<IEnumerable<object>> GetItemUsageReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var allSalesOrders = await _salesOrderRepository.GetAllWithDetailsAsync();

                var filteredOrders = allSalesOrders.AsQueryable();

                if (fromDate.HasValue)
                    filteredOrders = filteredOrders.Where(so => so.OrderDate >= fromDate.Value);

                if (toDate.HasValue)
                    filteredOrders = filteredOrders.Where(so => so.OrderDate <= toDate.Value);

                var completedOrders = filteredOrders
                    .Where(so => so.Status == Constants.SO_STATUS_COMPLETED)
                    .ToList();

                return completedOrders
                    .SelectMany(so => so.SalesOrderDetails)
                    .GroupBy(d => new { d.ItemId, d.Item.ItemCode, d.Item.Name, d.Item.Unit })
                    .Select(g => new
                    {
                        ItemId = g.Key.ItemId,
                        ItemCode = g.Key.ItemCode,
                        ItemName = g.Key.Name,
                        Unit = g.Key.Unit,
                        TotalQuantityUsed = g.Sum(d => d.Quantity),
                        TotalRevenue = g.Sum(d => d.TotalPrice),
                        OrderCount = g.Count(),
                        AverageUnitPrice = g.Average(d => d.UnitPrice),
                        FirstOrderDate = g.Min(d => d.SalesOrder.OrderDate),
                        LastOrderDate = g.Max(d => d.SalesOrder.OrderDate)
                    })
                    .OrderByDescending(x => x.TotalQuantityUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item usage report for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetItemPerformanceReportAsync()
        {
            try
            {
                var allItems = await GetItemsWithInventoryAsync();
                var stockSummary = await GetItemStockSummaryAsync();

                return allItems.Select(item => new
                {
                    ItemId = item.Id,
                    ItemCode = item.ItemCode,
                    ItemName = item.Name,
                    Unit = item.Unit,
                    StandardPrice = item.StandardPrice,
                    IsActive = item.IsActive,
                    TotalStock = stockSummary.ContainsKey(item.Id) ? stockSummary[item.Id] : 0,
                    InventoryLocations = item.Inventories.Count(),
                    TotalInventoryValue = item.Inventories.Sum(inv => inv.TotalValue),
                    AverageCostPrice = item.Inventories.Any() ? item.Inventories.Average(inv => inv.LastCostPrice) : 0,
                    LastUpdated = item.Inventories.Any() ? item.Inventories.Max(inv => inv.LastUpdated) : item.CreatedDate,
                    StockStatus = GetStockStatus(stockSummary.ContainsKey(item.Id) ? stockSummary[item.Id] : 0)
                }).OrderBy(x => x.ItemCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item performance report for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetItemStatisticsAsync()
        {
            try
            {
                var allItems = await GetAllItemsAsync();
                var activeItems = allItems.Where(item => item.IsActive).ToList();
                var stockSummary = await GetItemStockSummaryAsync();

                var stats = new Dictionary<string, object>
                {
                    ["TotalItems"] = allItems.Count(),
                    ["ActiveItems"] = activeItems.Count,
                    ["InactiveItems"] = allItems.Count() - activeItems.Count,
                    ["ItemsWithStock"] = stockSummary.Count(kv => kv.Value > 0),
                    ["ItemsOutOfStock"] = stockSummary.Count(kv => kv.Value == 0),
                    ["ItemsWithLowStock"] = stockSummary.Count(kv => kv.Value > 0 && kv.Value <= Constants.LOW_STOCK_THRESHOLD),
                    ["TotalStockQuantity"] = stockSummary.Sum(kv => kv.Value),
                    ["AverageStockPerItem"] = stockSummary.Any() ? stockSummary.Average(kv => kv.Value) : 0,
                    ["TotalInventoryValue"] = allItems.SelectMany(item => item.Inventories).Sum(inv => inv.TotalValue),
                    ["AverageItemPrice"] = activeItems.Any() ? activeItems.Average(item => item.StandardPrice) : 0
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item statistics for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetTopSellingItemsAsync(int topCount = 10)
        {
            try
            {
                var salesReport = await GetItemUsageReportAsync();
                return salesReport.Take(topCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling items for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetSlowMovingItemsAsync(int daysThreshold = 90)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysThreshold);
                var allItems = await GetItemsWithInventoryAsync();

                return allItems
                    .Where(item => item.Inventories.Any(inv => inv.LastUpdated < cutoffDate && inv.Quantity > 0))
                    .Select(item => new
                    {
                        ItemId = item.Id,
                        ItemCode = item.ItemCode,
                        ItemName = item.Name,
                        Unit = item.Unit,
                        TotalStock = item.Inventories.Sum(inv => inv.Quantity),
                        TotalValue = item.Inventories.Sum(inv => inv.TotalValue),
                        OldestInventoryDate = item.Inventories.Min(inv => inv.LastUpdated),
                        DaysSinceLastMovement = (DateTime.Now - item.Inventories.Min(inv => inv.LastUpdated)).Days,
                        Locations = item.Inventories.Count(),
                        Suggestion = "Consider promotional pricing, bundling, or liquidation"
                    })
                    .OrderBy(x => x.OldestInventoryDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slow moving items for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Price Analysis

        public async Task<object> GetItemPriceHistoryAsync(int itemId)
        {
            try
            {
                // This would require a price history table to track historical prices
                // For now, we'll return current price information from inventories and orders
                var item = await GetItemByIdAsync(itemId);
                if (item == null)
                    return new object();

                var inventories = await _inventoryRepository.GetByItemIdAsync(itemId);
                var allPOs = await _purchaseOrderRepository.GetAllWithDetailsAsync();
                var allSOs = await _salesOrderRepository.GetAllWithDetailsAsync();

                // Create a unified list to hold both purchase and sales history
                var priceHistory = new List<object>();

                // Add purchase history
                var purchaseEntries = allPOs
                    .SelectMany(po => po.PurchaseOrderDetails)
                    .Where(d => d.ItemId == itemId)
                    .Select(d => new
                    {
                        Date = d.PurchaseOrder.OrderDate,
                        Type = "Purchase",
                        Price = d.UnitPrice,
                        Quantity = d.Quantity,
                        PartnerName = d.PurchaseOrder.Supplier.Name,
                        OrderNumber = d.PurchaseOrder.PONumber
                    });

                // Add sales history  
                var salesEntries = allSOs
                    .SelectMany(so => so.SalesOrderDetails)
                    .Where(d => d.ItemId == itemId)
                    .Select(d => new
                    {
                        Date = d.SalesOrder.OrderDate,
                        Type = "Sale",
                        Price = d.UnitPrice,
                        Quantity = d.Quantity,
                        PartnerName = d.SalesOrder.Customer.Name,
                        OrderNumber = d.SalesOrder.SONumber
                    });

                // Combine both into a single list
                priceHistory.AddRange(purchaseEntries);
                priceHistory.AddRange(salesEntries);

                // Sort and take top 50
                var sortedHistory = priceHistory
                    .OrderByDescending(x => ((dynamic)x).Date)
                    .Take(50);

                return new
                {
                    ItemCode = item.ItemCode,
                    ItemName = item.Name,
                    StandardPrice = item.StandardPrice,
                    CurrentAverageCost = inventories.Any() ? inventories.Average(inv => inv.LastCostPrice) : 0,
                    PriceHistory = sortedHistory
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting price history for item {ItemId} for company {CompanyId}", itemId, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<decimal> GetItemAverageCostAsync(int itemId)
        {
            try
            {
                var inventories = await _inventoryRepository.GetByItemIdAsync(itemId);
                return inventories.Any() ? inventories.Average(inv => inv.LastCostPrice) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting average cost for item {ItemId} for company {CompanyId}", itemId, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<decimal> GetItemLastCostAsync(int itemId)
        {
            try
            {
                var inventories = await _inventoryRepository.GetByItemIdAsync(itemId);
                var latestInventory = inventories
                    .OrderByDescending(inv => inv.LastUpdated)
                    .FirstOrDefault();

                return latestInventory?.LastCostPrice ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last cost for item {ItemId} for company {CompanyId}", itemId, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<object>> GetItemPriceVarianceReportAsync()
        {
            try
            {
                var allItems = await GetActiveItemsAsync();
                var report = new List<object>();

                foreach (var item in allItems)
                {
                    var averageCost = await GetItemAverageCostAsync(item.Id);
                    var variance = item.StandardPrice - averageCost;
                    var variancePercentage = averageCost != 0 ? (variance / averageCost) * 100 : 0;

                    report.Add(new
                    {
                        ItemCode = item.ItemCode,
                        ItemName = item.Name,
                        StandardPrice = item.StandardPrice,
                        AverageCost = averageCost,
                        PriceVariance = variance,
                        VariancePercentage = variancePercentage,
                        VarianceStatus = Math.Abs(variancePercentage) > 10 ? "High Variance" :
                                       Math.Abs(variancePercentage) > 5 ? "Medium Variance" : "Low Variance"
                    });
                }

                return report.OrderByDescending(x => Math.Abs(((dynamic)x).VariancePercentage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item price variance report for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Integration Operations

        public async Task<bool> SyncItemWithInventoryAsync(int itemId)
        {
            try
            {
                // Sync item information with its inventory records
                var item = await GetItemByIdAsync(itemId);
                if (item == null)
                    return false;

                // Update any necessary calculations or status
                // This is a placeholder for future sync logic
                _logger.LogInformation("Synced item {ItemId} with inventory for company {CompanyId}", itemId, _currentUserService.CompanyId);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing item {ItemId} with inventory for company {CompanyId}", itemId, _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsNeedingRestockAsync()
        {
            try
            {
                var lowStockItems = await GetItemsWithLowStockAsync();
                var stockSummary = await GetItemStockSummaryAsync();

                return lowStockItems.Where(item =>
                    stockSummary.ContainsKey(item.Id) &&
                    stockSummary[item.Id] <= Constants.CRITICAL_STOCK_THRESHOLD);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items needing restock for company {CompanyId}", _currentUserService.CompanyId);
                throw;
            }
        }

        public async Task<object> GetItemSupplierInfoAsync(int itemId)
        {
            try
            {
                var allPOs = await _purchaseOrderRepository.GetAllWithDetailsAsync();

                var supplierInfo = allPOs
                    .SelectMany(po => po.PurchaseOrderDetails)
                    .Where(d => d.ItemId == itemId)
                    .GroupBy(d => new { d.PurchaseOrder.SupplierId, d.PurchaseOrder.Supplier.Name, d.PurchaseOrder.Supplier.Email })
                    .Select(g => new
                    {
                        SupplierId = g.Key.SupplierId,
                        SupplierName = g.Key.Name,
                        SupplierEmail = g.Key.Email,
                        OrderCount = g.Count(),
                        TotalQuantityOrdered = g.Sum(d => d.Quantity),
                        AverageUnitPrice = g.Average(d => d.UnitPrice),
                        LastOrderDate = g.Max(d => d.PurchaseOrder.OrderDate),
                        LastUnitPrice = g.OrderByDescending(d => d.PurchaseOrder.OrderDate).First().UnitPrice
                    })
                    .OrderByDescending(x => x.LastOrderDate)
                    .ToList();

                return new
                {
                    ItemId = itemId,
                    SupplierCount = supplierInfo.Count,
                    Suppliers = supplierInfo,
                    PreferredSupplier = supplierInfo.FirstOrDefault(), // Most recent
                    LowestPriceSupplier = supplierInfo.OrderBy(s => s.AverageUnitPrice).FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier info for item {ItemId} for company {CompanyId}", itemId, _currentUserService.CompanyId);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private string GetStockStatus(int stockQuantity)
        {
            if (stockQuantity == 0) return "Out of Stock";
            if (stockQuantity <= Constants.CRITICAL_STOCK_THRESHOLD) return "Critical";
            if (stockQuantity <= Constants.LOW_STOCK_THRESHOLD) return "Low Stock";
            if (stockQuantity <= 50) return "Normal";
            return "High Stock";
        }

        #endregion

        #region ViewModel Operations

        public async Task<ItemViewModel> GetItemViewModelAsync(int? id = null)
        {
            try
            {
                var viewModel = new ItemViewModel();

                if (id.HasValue)
                {
                    var item = await _itemRepository.GetByIdWithInventoryAsync(id.Value);
                    if (item != null)
                    {
                        viewModel = new ItemViewModel
                        {
                            Id = item.Id,
                            ItemCode = item.ItemCode,
                            Name = item.Name,
                            Description = item.Description,
                            Unit = item.Unit,
                            StandardPrice = item.StandardPrice,
                            SupplierId = item.SupplierId ?? 0,
                            IsActive = item.IsActive,
                            SupplierName = item.Supplier?.Name ?? "Unknown"
                        };
                    }
                }

                return await PopulateItemViewModelAsync(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item view model for ID {ItemId}", id);
                throw;
            }
        }

        public async Task<ItemViewModel> PopulateItemViewModelAsync(ItemViewModel viewModel)
        {
            try
            {
                // Get supplier options from ItemRepository
                var suppliers = await _itemRepository.GetActiveSuppliersForDropdownAsync();
                viewModel.SupplierOptions = new SelectList(suppliers, "Id", "Name", viewModel.SupplierId);

                // Calculate stock information if item exists
                if (viewModel.Id > 0)
                {
                    var stockSummary = await GetItemStockSummaryAsync();
                    viewModel.TotalStock = stockSummary.ContainsKey(viewModel.Id) ? stockSummary[viewModel.Id] : 0;
                    viewModel.StockLevel = GetStockStatus(viewModel.TotalStock);
                    viewModel.NeedsReorder = viewModel.TotalStock <= Constants.LOW_STOCK_THRESHOLD;
                    viewModel.IsAvailableForSale = viewModel.IsActive && viewModel.TotalStock > 0;
                }

                // Set display properties
                viewModel.ItemDisplay = $"{viewModel.ItemCode} - {viewModel.Name}";
                viewModel.Summary = $"{viewModel.ItemCode} - {viewModel.Name} ({viewModel.Unit})";
                viewModel.StatusCssClass = viewModel.IsActive ? "badge bg-success" : "badge bg-secondary";
                viewModel.StatusText = viewModel.IsActive ? "Aktif" : "Tidak Aktif";

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating item view model");
                throw;
            }
        }

        public async Task<ItemIndexViewModel> GetItemIndexViewModelAsync(ItemIndexViewModel? model = null)
        {
            try
            {
                model ??= new ItemIndexViewModel();

                // Get items based on filters
                IEnumerable<Item> items;
                
                if (!string.IsNullOrEmpty(model.SearchTerm))
                {
                    items = await _itemRepository.SearchItemsAsync(model.SearchTerm);
                }
                else if (model.SupplierId.HasValue)
                {
                    items = await _itemRepository.GetBySupplierIdAsync(model.SupplierId.Value);
                }
                else if (model.IsActive.HasValue)
                {
                    items = model.IsActive.Value 
                        ? await _itemRepository.GetActiveItemsAsync()
                        : (await _itemRepository.GetAllAsync()).Where(i => !i.IsActive);
                }
                else
                {
                    items = await _itemRepository.GetAllWithInventoryAsync();
                }

                // Convert to ViewModels
                var stockSummary = await GetItemStockSummaryAsync();
                model.Items = items.Select(item => new ItemViewModel
                {
                    Id = item.Id,
                    ItemCode = item.ItemCode,
                    Name = item.Name,
                    Description = item.Description,
                    Unit = item.Unit,
                    StandardPrice = item.StandardPrice,
                    SupplierId = item.SupplierId ?? 0,
                    IsActive = item.IsActive,
                    SupplierName = item.Supplier?.Name ?? "Unknown",
                    TotalStock = stockSummary.ContainsKey(item.Id) ? stockSummary[item.Id] : 0,
                    StockLevel = GetStockStatus(stockSummary.ContainsKey(item.Id) ? stockSummary[item.Id] : 0),
                    NeedsReorder = (stockSummary.ContainsKey(item.Id) ? stockSummary[item.Id] : 0) <= Constants.LOW_STOCK_THRESHOLD,
                    IsAvailableForSale = item.IsActive && (stockSummary.ContainsKey(item.Id) ? stockSummary[item.Id] : 0) > 0,
                    ItemDisplay = $"{item.ItemCode} - {item.Name}",
                    Summary = $"{item.ItemCode} - {item.Name} ({item.Unit})",
                    StatusCssClass = item.IsActive ? "badge bg-success" : "badge bg-secondary",
                    StatusText = item.IsActive ? "Aktif" : "Tidak Aktif"
                }).ToList();

                // Get summary statistics
                model.Summary = await GetItemSummaryAsync();

                // Get supplier options for filter
                var allItems = await _itemRepository.GetAllAsync();
                var suppliers = allItems
                    .Where(s => s.SupplierId.HasValue)
                    .Select(s => s.Supplier)
                    .Where(s => s != null)
                    .Distinct()
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Name
                    })
                    .ToList();

                model.SupplierOptions = suppliers;

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item index view model");
                throw;
            }
        }

        public async Task<ItemDetailsViewModel> GetItemDetailsViewModelAsync(int id)
        {
            try
            {
                var item = await _itemRepository.GetByIdWithInventoryAsync(id);
                if (item == null)
                {
                    throw new ArgumentException($"Item with ID {id} not found");
                }

                var viewModel = new ItemDetailsViewModel
                {
                    Item = new ItemViewModel
                    {
                        Id = item.Id,
                        ItemCode = item.ItemCode,
                        Name = item.Name,
                        Description = item.Description,
                        Unit = item.Unit,
                        StandardPrice = item.StandardPrice,
                        SupplierId = item.SupplierId ?? 0,
                        IsActive = item.IsActive,
                        SupplierName = item.Supplier?.Name ?? "Unknown"
                    }
                };

                // Get inventory details
                var inventories = await _itemRepository.GetInventoriesByItemIdAsync(id);
                viewModel.Inventories = inventories.Select(inv => new InventoryViewModel
                {
                    Id = inv.Id,
                    ItemId = inv.ItemId,
                    LocationId = inv.LocationId,
                    Quantity = inv.Quantity,
                    ItemDisplay = $"{item.ItemCode} - {item.Name}",
                    LocationDisplay = inv.Location?.Name ?? "Unknown",
                    LastUpdated = inv.ModifiedDate ?? inv.CreatedDate
                }).ToList();

                // Get related data
                viewModel.PurchaseOrderDetails = (await _itemRepository.GetPurchaseOrderDetailsByItemIdAsync(id)).ToList();
                viewModel.ASNDetails = (await _itemRepository.GetASNDetailsByItemIdAsync(id)).ToList();
                viewModel.SalesOrderDetails = (await _itemRepository.GetSalesOrderDetailsByItemIdAsync(id)).ToList();

                // Calculate totals
                viewModel.TotalQuantity = viewModel.Inventories.Sum(inv => inv.Quantity);
                viewModel.TotalLocations = viewModel.Inventories.Count;
                viewModel.TotalValue = viewModel.TotalQuantity * item.StandardPrice;

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item details view model for ID {ItemId}", id);
                throw;
            }
        }

        public async Task<ItemSummaryViewModel> GetItemSummaryAsync()
        {
            try
            {
                var items = await _itemRepository.GetAllWithInventoryAsync();
                var stockSummary = await GetItemStockSummaryAsync();

                var summary = new ItemSummaryViewModel
                {
                    TotalItems = items.Count(),
                    ActiveItems = items.Count(i => i.IsActive),
                    InactiveItems = items.Count(i => !i.IsActive),
                    ItemsWithStock = items.Count(i => stockSummary.ContainsKey(i.Id) && stockSummary[i.Id] > 0),
                    ItemsOutOfStock = items.Count(i => !stockSummary.ContainsKey(i.Id) || stockSummary[i.Id] == 0),
                    LowStockItems = items.Count(i => stockSummary.ContainsKey(i.Id) && stockSummary[i.Id] <= Constants.LOW_STOCK_THRESHOLD),
                    TotalValue = items.Sum(i => (stockSummary.ContainsKey(i.Id) ? stockSummary[i.Id] : 0) * i.StandardPrice),
                    AveragePrice = items.Any() ? items.Average(i => i.StandardPrice) : 0,
                    StatusBreakdown = new Dictionary<string, int>
                    {
                        ["Active"] = items.Count(i => i.IsActive),
                        ["Inactive"] = items.Count(i => !i.IsActive),
                        ["With Stock"] = items.Count(i => stockSummary.ContainsKey(i.Id) && stockSummary[i.Id] > 0),
                        ["Out of Stock"] = items.Count(i => !stockSummary.ContainsKey(i.Id) || stockSummary[i.Id] == 0)
                    }
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item summary");
                throw;
            }
        }

        #endregion
    }
}