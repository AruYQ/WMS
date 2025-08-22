using System.Linq;
using WMS.Data.Repositories;
using WMS.Models;
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

        public ItemService(
            IItemRepository itemRepository,
            IInventoryRepository inventoryRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            ISalesOrderRepository salesOrderRepository)
        {
            _itemRepository = itemRepository;
            _inventoryRepository = inventoryRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _salesOrderRepository = salesOrderRepository;
        }

        #region Basic CRUD Operations

        public async Task<IEnumerable<Item>> GetAllItemsAsync()
        {
            return await _itemRepository.GetAllWithInventoryAsync();
        }

        public async Task<Item?> GetItemByIdAsync(int id)
        {
            return await _itemRepository.GetByIdWithInventoryAsync(id);
        }

        public async Task<Item> CreateItemAsync(Item item)
        {
            if (!await ValidateItemAsync(item))
                throw new InvalidOperationException("Item validation failed");

            if (!await IsItemCodeUniqueAsync(item.ItemCode))
                throw new InvalidOperationException($"Item code '{item.ItemCode}' already exists");

            item.CreatedDate = DateTime.Now;
            return await _itemRepository.AddAsync(item);
        }

        public async Task<Item> UpdateItemAsync(int id, Item item)
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
            existingItem.IsActive = item.IsActive;
            existingItem.ModifiedDate = DateTime.Now;

            await _itemRepository.UpdateAsync(existingItem);
            return existingItem;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            if (!await CanDeleteItemAsync(id))
                return false;

            return await _itemRepository.DeleteAsync(id);
        }

        #endregion

        #region Query Operations

        public async Task<IEnumerable<Item>> GetActiveItemsAsync()
        {
            return await _itemRepository.GetActiveItemsAsync();
        }

        public async Task<Item?> GetItemByCodeAsync(string itemCode)
        {
            return await _itemRepository.GetByItemCodeAsync(itemCode);
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(string searchTerm)
        {
            return await _itemRepository.SearchItemsAsync(searchTerm);
        }

        public async Task<IEnumerable<Item>> GetItemsWithInventoryAsync()
        {
            return await _itemRepository.GetAllWithInventoryAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsWithLowStockAsync(int threshold = 10)
        {
            return await _itemRepository.GetItemsWithLowStockAsync(threshold);
        }

        #endregion

        #region Validation Operations

        public async Task<bool> IsItemCodeUniqueAsync(string itemCode, int? excludeId = null)
        {
            var existingItem = await _itemRepository.GetByItemCodeAsync(itemCode);
            return existingItem == null || (excludeId.HasValue && existingItem.Id == excludeId.Value);
        }

        public async Task<bool> ValidateItemAsync(Item item)
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

        public async Task<bool> CanDeleteItemAsync(int id)
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
            var inventories = await _inventoryRepository.GetByItemAsync(id);
            var hasInventory = inventories.Any(inv => inv.Quantity > 0);

            return !hasInventory;
        }

        #endregion

        #region Stock Information

        public async Task<Dictionary<int, int>> GetItemStockSummaryAsync()
        {
            return await _itemRepository.GetItemStockSummaryAsync();
        }

        public async Task<int> GetItemTotalStockAsync(int itemId)
        {
            var inventories = await _inventoryRepository.GetByItemAsync(itemId);
            return inventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE)
                              .Sum(inv => inv.Quantity);
        }

        public async Task<decimal> GetItemTotalValueAsync(int itemId)
        {
            var inventories = await _inventoryRepository.GetByItemAsync(itemId);
            return inventories.Sum(inv => inv.TotalValue);
        }

        public async Task<IEnumerable<object>> GetItemInventoryDetailsAsync(int itemId)
        {
            var inventories = await _inventoryRepository.GetByItemAsync(itemId);

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

        #endregion

        #region Business Logic Operations

        public async Task<bool> UpdateItemStatusAsync(int id, bool isActive)
        {
            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null)
                return false;

            item.IsActive = isActive;
            item.ModifiedDate = DateTime.Now;

            await _itemRepository.UpdateAsync(item);
            return true;
        }

        public async Task<IEnumerable<Item>> GetItemsForPurchaseOrderAsync()
        {
            return await GetActiveItemsAsync();
        }

        public async Task<IEnumerable<Item>> GetItemsForSalesOrderAsync()
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

        public async Task<IEnumerable<Item>> GetAvailableItemsAsync()
        {
            return await GetItemsForSalesOrderAsync();
        }

        #endregion

        #region Reporting Operations

        public async Task<IEnumerable<object>> GetItemUsageReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
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
                    TotalWarehouseFee = g.Sum(d => d.WarehouseFeeApplied),
                    OrderCount = g.Count(),
                    AverageUnitPrice = g.Average(d => d.UnitPrice),
                    FirstOrderDate = g.Min(d => d.SalesOrder.OrderDate),
                    LastOrderDate = g.Max(d => d.SalesOrder.OrderDate)
                })
                .OrderByDescending(x => x.TotalQuantityUsed);
        }

        public async Task<IEnumerable<object>> GetItemPerformanceReportAsync()
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

        public async Task<Dictionary<string, object>> GetItemStatisticsAsync()
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

        public async Task<IEnumerable<object>> GetTopSellingItemsAsync(int topCount = 10)
        {
            var salesReport = await GetItemUsageReportAsync();
            return salesReport.Take(topCount);
        }

        public async Task<IEnumerable<object>> GetSlowMovingItemsAsync(int daysThreshold = 90)
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

        #endregion

        #region Price Analysis

        public async Task<object> GetItemPriceHistoryAsync(int itemId)
        {
            // This would require a price history table to track historical prices
            // For now, we'll return current price information from inventories and orders
            var item = await GetItemByIdAsync(itemId);
            if (item == null)
                return new object();

            var inventories = await _inventoryRepository.GetByItemAsync(itemId);
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

        public async Task<decimal> GetItemAverageCostAsync(int itemId)
        {
            var inventories = await _inventoryRepository.GetByItemAsync(itemId);
            return inventories.Any() ? inventories.Average(inv => inv.LastCostPrice) : 0;
        }

        public async Task<decimal> GetItemLastCostAsync(int itemId)
        {
            var inventories = await _inventoryRepository.GetByItemAsync(itemId);
            var latestInventory = inventories
                .OrderByDescending(inv => inv.LastUpdated)
                .FirstOrDefault();

            return latestInventory?.LastCostPrice ?? 0;
        }

        public async Task<IEnumerable<object>> GetItemPriceVarianceReportAsync()
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

        #endregion

        #region Integration Operations

        public async Task<bool> SyncItemWithInventoryAsync(int itemId)
        {
            // Sync item information with its inventory records
            var item = await GetItemByIdAsync(itemId);
            if (item == null)
                return false;

            // Update any necessary calculations or status
            // This is a placeholder for future sync logic
            return await Task.FromResult(true);
        }

        public async Task<IEnumerable<Item>> GetItemsNeedingRestockAsync()
        {
            var lowStockItems = await GetItemsWithLowStockAsync();
            var stockSummary = await GetItemStockSummaryAsync();

            return lowStockItems.Where(item =>
                stockSummary.ContainsKey(item.Id) &&
                stockSummary[item.Id] <= Constants.CRITICAL_STOCK_THRESHOLD);
        }

        public async Task<object> GetItemSupplierInfoAsync(int itemId)
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
    }
}