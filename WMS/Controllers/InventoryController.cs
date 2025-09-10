using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMS.Attributes;
using WMS.Data;
using WMS.Data.Repositories;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Controllers
{
    /// <summary>
    /// Controller untuk Inventory management dan Putaway operations
    /// </summary>
    [RequirePermission("INVENTORY_MANAGE")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IItemService _itemService;
        private readonly IASNService _asnService;
        private readonly ILogger<InventoryController> _logger;
        private readonly IASNRepository _asnRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ApplicationDbContext _context;

        public InventoryController(
            IInventoryService inventoryService,
            IItemService itemService,
            IASNService asnService,
            ILogger<InventoryController> logger,
            IASNRepository asnRepository,
            ILocationRepository locationRepository,
            ICurrentUserService currentUserService,
            ApplicationDbContext context)
        {
            _inventoryService = inventoryService;
            _itemService = itemService;
            _asnService = asnService;
            _logger = logger;
            _asnRepository = asnRepository;
            _locationRepository = locationRepository;
            _currentUserService = currentUserService;
            _context = context;
        }

        #region Basic CRUD Operations

        /// <summary>
        /// Index - List semua inventory dengan filtering
        /// </summary>
        public async Task<IActionResult> Index(InventoryIndexViewModel? model)
        {
            try
            {
                model ??= new InventoryIndexViewModel();
                
                // Get inventories based on filters
                IEnumerable<Inventory> inventories;
                
                if (!string.IsNullOrEmpty(model.SearchTerm))
                {
                    inventories = await _inventoryService.SearchInventoryAsync(model.SearchTerm);
                }
                else if (!string.IsNullOrEmpty(model.StatusFilter))
                {
                    inventories = await _inventoryService.GetInventoriesByStatusAsync(model.StatusFilter);
                }
                else if (model.ShowLowStockOnly)
                {
                    inventories = await _inventoryService.GetLowStockInventoriesAsync();
                }
                else if (model.ShowEmptyLocations)
                {
                    inventories = await _inventoryService.GetEmptyLocationsAsync();
                }
                else
                {
                    inventories = await _inventoryService.GetAllInventoriesAsync();
                }

                // Convert to ViewModels
                model.Inventories = inventories.Select(inv => new InventoryViewModel
                {
                    Id = inv.Id,
                    ItemId = inv.ItemId,
                    LocationId = inv.LocationId,
                    Quantity = inv.Quantity,
                    LastCostPrice = inv.LastCostPrice,
                    Status = inv.Status,
                    Notes = inv.Notes,
                    SourceReference = inv.SourceReference,
                    LastUpdated = inv.LastUpdated,
                    ItemDisplay = inv.ItemDisplay,
                    LocationDisplay = inv.LocationDisplay,
                    ItemUnit = inv.ItemUnit,
                    TotalValue = inv.TotalValue,
                    Summary = inv.Summary,
                    StatusCssClass = inv.StatusCssClass,
                    StatusIndonesia = inv.StatusIndonesia,
                    QuantityCssClass = inv.QuantityCssClass,
                    StockLevel = inv.StockLevel,
                    IsAvailableForSale = inv.IsAvailableForSale,
                    NeedsReorder = inv.NeedsReorder
                });

                // Get available statuses for filter
                model.AvailableStatuses = new[]
                {
                    Constants.INVENTORY_STATUS_AVAILABLE,
                    Constants.INVENTORY_STATUS_RESERVED,
                    Constants.INVENTORY_STATUS_DAMAGED,
                    Constants.INVENTORY_STATUS_QUARANTINE,
                    Constants.INVENTORY_STATUS_BLOCKED,
                    Constants.INVENTORY_STATUS_EMPTY
                };

                // Get summary statistics
                model.Summary = await GetInventorySummaryAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory index");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data inventory.";
                return View(new InventoryIndexViewModel());
            }
        }

        /// <summary>
        /// Details - Detail inventory per item-location
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(id);
                if (inventory == null)
                {
                    TempData["ErrorMessage"] = "Inventory tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new InventoryViewModel
                {
                    Id = inventory.Id,
                    ItemId = inventory.ItemId,
                    LocationId = inventory.LocationId,
                    Quantity = inventory.Quantity,
                    LastCostPrice = inventory.LastCostPrice,
                    Status = inventory.Status,
                    Notes = inventory.Notes,
                    SourceReference = inventory.SourceReference,
                    LastUpdated = inventory.LastUpdated,
                    ItemDisplay = inventory.ItemDisplay,
                    LocationDisplay = inventory.LocationDisplay,
                    ItemUnit = inventory.ItemUnit,
                    TotalValue = inventory.TotalValue,
                    Summary = inventory.Summary,
                    StatusCssClass = inventory.StatusCssClass,
                    StatusIndonesia = inventory.StatusIndonesia,
                    QuantityCssClass = inventory.QuantityCssClass,
                    StockLevel = inventory.StockLevel,
                    IsAvailableForSale = inventory.IsAvailableForSale,
                    NeedsReorder = inventory.NeedsReorder
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory details for ID {InventoryId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat detail inventory.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Create - Manual inventory creation
        /// </summary>
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = await _inventoryService.GetInventoryViewModelAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create inventory form");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat form create inventory.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var inventory = await _inventoryService.CreateInventoryAsync(viewModel);
                TempData["SuccessMessage"] = $"Inventory berhasil dibuat untuk {inventory.ItemDisplay} di {inventory.LocationDisplay}.";
                
                return RedirectToAction(nameof(Details), new { id = inventory.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inventory");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat membuat inventory.";
                viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        /// <summary>
        /// Edit - Update inventory
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(id);
                if (inventory == null)
                {
                    TempData["ErrorMessage"] = "Inventory tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new InventoryViewModel
                {
                    Id = inventory.Id,
                    ItemId = inventory.ItemId,
                    LocationId = inventory.LocationId,
                    Quantity = inventory.Quantity,
                    LastCostPrice = inventory.LastCostPrice,
                    Status = inventory.Status,
                    Notes = inventory.Notes,
                    SourceReference = inventory.SourceReference,
                    LastUpdated = inventory.LastUpdated,
                    ItemDisplay = inventory.ItemDisplay,
                    LocationDisplay = inventory.LocationDisplay,
                    ItemUnit = inventory.ItemUnit,
                    TotalValue = inventory.TotalValue,
                    Summary = inventory.Summary,
                    StatusCssClass = inventory.StatusCssClass,
                    StatusIndonesia = inventory.StatusIndonesia,
                    QuantityCssClass = inventory.QuantityCssClass,
                    StockLevel = inventory.StockLevel,
                    IsAvailableForSale = inventory.IsAvailableForSale,
                    NeedsReorder = inventory.NeedsReorder
                };

                viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit inventory form for ID {InventoryId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat form edit inventory.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                    return View(viewModel);
                }

                var inventory = await _inventoryService.UpdateInventoryAsync(id, viewModel);
                TempData["SuccessMessage"] = $"Inventory berhasil diupdate untuk {inventory.ItemDisplay} di {inventory.LocationDisplay}.";
                
                return RedirectToAction(nameof(Details), new { id = inventory.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory with ID {InventoryId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mengupdate inventory.";
                viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);
                return View(viewModel);
            }
        }

        /// <summary>
        /// Delete - Delete inventory
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(id);
                if (inventory == null)
                {
                    TempData["ErrorMessage"] = "Inventory tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new InventoryViewModel
                {
                    Id = inventory.Id,
                    ItemId = inventory.ItemId,
                    LocationId = inventory.LocationId,
                    Quantity = inventory.Quantity,
                    LastCostPrice = inventory.LastCostPrice,
                    Status = inventory.Status,
                    Notes = inventory.Notes,
                    SourceReference = inventory.SourceReference,
                    LastUpdated = inventory.LastUpdated,
                    ItemDisplay = inventory.ItemDisplay,
                    LocationDisplay = inventory.LocationDisplay,
                    ItemUnit = inventory.ItemUnit,
                    TotalValue = inventory.TotalValue,
                    Summary = inventory.Summary,
                    StatusCssClass = inventory.StatusCssClass,
                    StatusIndonesia = inventory.StatusIndonesia,
                    QuantityCssClass = inventory.QuantityCssClass,
                    StockLevel = inventory.StockLevel,
                    IsAvailableForSale = inventory.IsAvailableForSale,
                    NeedsReorder = inventory.NeedsReorder
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete inventory form for ID {InventoryId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat form delete inventory.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _inventoryService.DeleteInventoryAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Inventory berhasil dihapus.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Gagal menghapus inventory.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inventory with ID {InventoryId}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat menghapus inventory.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Stock Operations

        /// <summary>
        /// TransferStock - Transfer stock between locations
        /// </summary>
        public async Task<IActionResult> TransferStock(int inventoryId)
        {
            try
            {
                var inventory = await _inventoryService.GetInventoryByIdAsync(inventoryId);
                if (inventory == null)
                {
                    TempData["ErrorMessage"] = "Inventory tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new InventoryViewModel
                {
                    Id = inventory.Id,
                    ItemId = inventory.ItemId,
                    LocationId = inventory.LocationId,
                    Quantity = inventory.Quantity,
                    LastCostPrice = inventory.LastCostPrice,
                    Status = inventory.Status,
                    Notes = inventory.Notes,
                    SourceReference = inventory.SourceReference,
                    LastUpdated = inventory.LastUpdated,
                    ItemDisplay = inventory.ItemDisplay,
                    LocationDisplay = inventory.LocationDisplay,
                    ItemUnit = inventory.ItemUnit,
                    TotalValue = inventory.TotalValue,
                    Summary = inventory.Summary,
                    StatusCssClass = inventory.StatusCssClass,
                    StatusIndonesia = inventory.StatusIndonesia,
                    QuantityCssClass = inventory.QuantityCssClass,
                    StockLevel = inventory.StockLevel,
                    IsAvailableForSale = inventory.IsAvailableForSale,
                    NeedsReorder = inventory.NeedsReorder
                };

                // Get available locations for transfer
                var locations = await _locationRepository.GetAllAsync();
                viewModel.Locations = new SelectList(locations, "Id", "DisplayName", viewModel.LocationId);

                // Create enhanced location dropdown items
                var locationDropdownItems = locations.Select(location => new LocationDropdownItem
                {
                    Id = location.Id,
                    Code = location.Code,
                    Name = location.Name,
                    MaxCapacity = location.MaxCapacity,
                    CurrentCapacity = location.CurrentCapacity,
                    AvailableCapacity = location.AvailableCapacity,
                    DisplayText = location.DropdownDisplayText,
                    CssClass = location.DropdownCssClass,
                    StatusText = location.DropdownStatusText,
                    CanAccommodate = true,
                    IsFull = location.IsFull,
                    CapacityPercentage = location.CapacityPercentage
                }).ToList();

                viewModel.LocationDropdownItems = locationDropdownItems;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading transfer stock form for inventory {InventoryId}", inventoryId);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat form transfer stock.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferStock(int inventoryId, int toLocationId, int quantity)
        {
            try
            {
                var success = await _inventoryService.TransferStockAsync(inventoryId, toLocationId, quantity);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Stock berhasil ditransfer sebanyak {quantity} unit.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Gagal mentransfer stock.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring stock from inventory {InventoryId} to location {LocationId}", inventoryId, toLocationId);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat mentransfer stock.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// AdjustStock - Adjust stock quantity
        /// </summary>
        public async Task<IActionResult> AdjustStock(int inventoryId)
        {
            try
            {
                // Enhanced parameter validation
                if (inventoryId <= 0)
                {
                    _logger.LogWarning("Invalid inventoryId: {InventoryId}", inventoryId);
                    TempData["ErrorMessage"] = "ID inventory tidak valid.";
                    return RedirectToAction(nameof(Index));
                }

                // Enhanced CompanyId debugging
                var companyId = _currentUserService.CompanyId;
                _logger.LogInformation("AdjustStock - CompanyId: {CompanyId}, InventoryId: {InventoryId}", companyId, inventoryId);
                
                if (!companyId.HasValue)
                {
                    _logger.LogError("CompanyId is null for AdjustStock, InventoryId: {InventoryId}", inventoryId);
                    TempData["ErrorMessage"] = "Company ID tidak tersedia. Silakan login ulang.";
                    return RedirectToAction(nameof(Index));
                }

                // Try to get inventory with enhanced debugging
                var inventory = await _inventoryService.GetInventoryByIdAsync(inventoryId);
                
                if (inventory == null)
                {
                    _logger.LogWarning("Inventory not found - CompanyId: {CompanyId}, InventoryId: {InventoryId}", companyId, inventoryId);
                    
                    // Enhanced debugging - try to find inventory without company filtering
                    var debugInventory = await _context.Inventories
                        .Include(inv => inv.Item)
                        .Include(inv => inv.Location)
                        .FirstOrDefaultAsync(inv => inv.Id == inventoryId);
                    
                    if (debugInventory != null)
                    {
                        _logger.LogWarning("Inventory found but different CompanyId - Inventory CompanyId: {InventoryCompanyId}, Current CompanyId: {CurrentCompanyId}", 
                            debugInventory.CompanyId, companyId);
                        TempData["ErrorMessage"] = $"Inventory ditemukan tetapi milik company lain. Inventory CompanyId: {debugInventory.CompanyId}, Current CompanyId: {companyId}";
                    }
                    else
                    {
                        _logger.LogWarning("Inventory not found in database at all - InventoryId: {InventoryId}", inventoryId);
                        TempData["ErrorMessage"] = $"Inventory dengan ID {inventoryId} tidak ditemukan di database.";
                    }
                    
                    return RedirectToAction(nameof(Index));
                }

                // Log successful inventory retrieval
                _logger.LogInformation("Successfully retrieved inventory {InventoryId} for company {CompanyId}", inventoryId, companyId);

                var viewModel = new InventoryViewModel
                {
                    Id = inventory.Id,
                    ItemId = inventory.ItemId,
                    LocationId = inventory.LocationId,
                    Quantity = inventory.Quantity,
                    LastCostPrice = inventory.LastCostPrice,
                    Status = inventory.Status,
                    Notes = inventory.Notes,
                    SourceReference = inventory.SourceReference,
                    LastUpdated = inventory.LastUpdated,
                    ItemDisplay = inventory.ItemDisplay,
                    LocationDisplay = inventory.LocationDisplay,
                    ItemUnit = inventory.ItemUnit,
                    TotalValue = inventory.TotalValue,
                    Summary = inventory.Summary,
                    StatusCssClass = inventory.StatusCssClass,
                    StatusIndonesia = inventory.StatusIndonesia,
                    QuantityCssClass = inventory.QuantityCssClass,
                    StockLevel = inventory.StockLevel,
                    IsAvailableForSale = inventory.IsAvailableForSale,
                    NeedsReorder = inventory.NeedsReorder
                };

                // Populate location dropdown items for capacity validation
                viewModel = await _inventoryService.PopulateInventoryViewModelAsync(viewModel);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading adjust stock form for inventory {InventoryId}", inventoryId);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat form adjust stock.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int inventoryId, int newQuantity, string reason)
        {
            try
            {
                // Enhanced parameter validation with detailed logging
                _logger.LogInformation("AdjustStock POST - Received parameters: inventoryId={InventoryId}, newQuantity={NewQuantity}, reason={Reason}", 
                    inventoryId, newQuantity, reason);
                
                if (inventoryId <= 0)
                {
                    _logger.LogWarning("Invalid inventoryId in POST: {InventoryId}", inventoryId);
                    TempData["ErrorMessage"] = "ID inventory tidak valid.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "Alasan penyesuaian harus diisi.";
                    return RedirectToAction(nameof(AdjustStock), new { inventoryId });
                }

                var inventory = await _inventoryService.GetInventoryByIdAsync(inventoryId);
                if (inventory == null)
                {
                    _logger.LogWarning("Inventory not found in POST - InventoryId: {InventoryId}", inventoryId);
                    TempData["ErrorMessage"] = "Inventory tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate new quantity
                if (newQuantity < 0)
                {
                    TempData["ErrorMessage"] = "Quantity tidak boleh negatif.";
                    return RedirectToAction(nameof(AdjustStock), new { inventoryId });
                }

                // Check if adjustment would exceed location capacity
                var location = await _locationRepository.GetByIdAsync(inventory.LocationId);
                if (location != null)
                {
                    var currentCapacity = await _context.Inventories
                        .Where(inv => inv.LocationId == inventory.LocationId && inv.CompanyId == _currentUserService.CompanyId)
                        .SumAsync(inv => inv.Quantity);
                    
                    var capacityAfterAdjustment = currentCapacity - inventory.Quantity + newQuantity;
                    
                    if (capacityAfterAdjustment > location.MaxCapacity)
                    {
                        TempData["ErrorMessage"] = $"Penyesuaian ini akan melebihi kapasitas maksimal lokasi ({location.MaxCapacity}). Kapasitas setelah penyesuaian: {capacityAfterAdjustment}";
                        return RedirectToAction(nameof(AdjustStock), new { inventoryId });
                    }
                }

                var success = await _inventoryService.UpdateQuantityAsync(inventoryId, newQuantity);
                if (success)
                {
                    var adjustmentAmount = newQuantity - inventory.Quantity;
                    var adjustmentText = adjustmentAmount > 0 ? $"dinaikkan sebanyak {adjustmentAmount}" : 
                                    adjustmentAmount < 0 ? $"diturunkan sebanyak {Math.Abs(adjustmentAmount)}" : 
                                    "tetap sama";
                    
                    TempData["SuccessMessage"] = $"Stock berhasil disesuaikan dari {inventory.Quantity} menjadi {newQuantity} unit ({adjustmentText}). Alasan: {reason}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Gagal menyesuaikan stock.";
                }

                return RedirectToAction(nameof(Details), new { id = inventoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting stock for inventory {InventoryId}", inventoryId);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat menyesuaikan stock.";
                return RedirectToAction(nameof(AdjustStock), new { inventoryId });
            }
        }

        #endregion

        #region Location Status

        /// <summary>
        /// LocationStatus - Status semua location dengan inventory
        /// </summary>
        public async Task<IActionResult> LocationStatus()
        {
            try
            {
                var locations = await _inventoryService.GetAllLocationsAsync();
                var inventories = await _inventoryService.GetAllInventoriesAsync();

                var viewModel = new List<LocationStatusViewModel>();

                foreach (var location in locations)
                {
                    var locationInventories = inventories.Where(inv => inv.LocationId == location.Id).ToList();
                    var currentCapacity = locationInventories.Sum(inv => inv.Quantity);
                    var availableCapacity = location.MaxCapacity - currentCapacity;
                    var capacityPercentage = location.MaxCapacity > 0 ? (double)currentCapacity / location.MaxCapacity * 100 : 0;

                    viewModel.Add(new LocationStatusViewModel
                    {
                        LocationId = location.Id,
                        LocationCode = location.Code,
                        LocationName = location.Name,
                        MaxCapacity = location.MaxCapacity,
                        CurrentCapacity = currentCapacity,
                        AvailableCapacity = availableCapacity,
                        CapacityPercentage = capacityPercentage,
                        IsFull = currentCapacity >= location.MaxCapacity,
                        ItemCount = locationInventories.Count,
                        StatusCssClass = currentCapacity >= location.MaxCapacity ? "text-danger" :
                                       availableCapacity <= 5 ? "text-danger" :
                                       availableCapacity <= 20 ? "text-warning" : "text-success",
                        Items = locationInventories.Select(inv => new InventoryViewModel
                        {
                            Id = inv.Id,
                            ItemId = inv.ItemId,
                            LocationId = inv.LocationId,
                            Quantity = inv.Quantity,
                            LastCostPrice = inv.LastCostPrice,
                            Status = inv.Status,
                            Notes = inv.Notes,
                            SourceReference = inv.SourceReference,
                            LastUpdated = inv.LastUpdated,
                            ItemDisplay = inv.ItemDisplay,
                            LocationDisplay = inv.LocationDisplay,
                            ItemUnit = inv.ItemUnit,
                            TotalValue = inv.TotalValue,
                            Summary = inv.Summary,
                            StatusCssClass = inv.StatusCssClass,
                            StatusIndonesia = inv.StatusIndonesia,
                            QuantityCssClass = inv.QuantityCssClass,
                            StockLevel = inv.StockLevel,
                            IsAvailableForSale = inv.IsAvailableForSale,
                            NeedsReorder = inv.NeedsReorder
                        }).ToList()
                    });
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading location status");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat status lokasi.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Putaway Operations

        /// <summary>
        /// Putaway - List ASN yang ready untuk putaway
        /// </summary>
        public async Task<IActionResult> Putaway()
        {
            try
            {
                var asns = await _inventoryService.GetASNsReadyForPutawayAsync();
                var summary = await GetPutawaySummaryAsync();

                var viewModel = new PutawayIndexViewModel
                {
                    ProcessedASNs = asns.Select(asn => new ASNForPutawayViewModel
                    {
                        ASNId = asn.Id,
                        ASNNumber = asn.ASNNumber,
                        SupplierName = asn.PurchaseOrder?.Supplier?.Name ?? "Unknown",
                        TotalItemTypes = asn.ASNDetails?.Count ?? 0,
                        TotalQuantity = asn.ASNDetails?.Sum(ad => ad.ShippedQuantity) ?? 0,
                        PendingPutawayCount = asn.ASNDetails?.Count(ad => ad.RemainingQuantity > 0) ?? 0,
                        CompletionPercentage = asn.ASNDetails?.Any() == true ? 
                            (double)(asn.ASNDetails.Sum(ad => ad.AlreadyPutAwayQuantity) * 100) / asn.ASNDetails.Sum(ad => ad.ShippedQuantity) : 0,
                        Status = asn.Status,
                        ActualArrivalDate = asn.ActualArrivalDate,
                        StatusIndonesia = asn.Status switch
                        {
                            Constants.ASN_STATUS_PENDING => "Menunggu",
                            Constants.ASN_STATUS_IN_TRANSIT => "Dalam Perjalanan",
                            Constants.ASN_STATUS_ARRIVED => "Telah Sampai",
                            Constants.ASN_STATUS_PROCESSED => "Diproses",
                            Constants.ASN_STATUS_COMPLETED => "Selesai",
                            Constants.ASN_STATUS_CANCELLED => "Dibatalkan",
                            _ => asn.Status
                        },
                        StatusCssClass = asn.Status switch
                        {
                            Constants.ASN_STATUS_PENDING => Constants.BADGE_WARNING,
                            Constants.ASN_STATUS_IN_TRANSIT => Constants.BADGE_INFO,
                            Constants.ASN_STATUS_ARRIVED => Constants.BADGE_INFO,
                            Constants.ASN_STATUS_PROCESSED => Constants.BADGE_INFO,
                            Constants.ASN_STATUS_COMPLETED => Constants.BADGE_SUCCESS,
                            Constants.ASN_STATUS_CANCELLED => Constants.BADGE_DANGER,
                            _ => Constants.BADGE_SECONDARY
                        },
                        CompletionCssClass = asn.Status == Constants.ASN_STATUS_COMPLETED ? Constants.STATUS_SUCCESS : Constants.STATUS_SECONDARY,
                        CanStartPutaway = asn.Status == Constants.ASN_STATUS_PROCESSED,
                        IsCompleted = asn.Status == Constants.ASN_STATUS_COMPLETED,
                        CanProcessAll = asn.Status == Constants.ASN_STATUS_PROCESSED && (asn.ASNDetails?.Any(ad => ad.RemainingQuantity > 0) ?? false),
                        ReadyForPutawayCount = asn.ASNDetails?.Count(ad => ad.RemainingQuantity > 0) ?? 0
                    }).ToList(),
                    Summary = summary
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading putaway index");
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat data putaway.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// ProcessPutaway - Process putaway untuk specific ASN
        /// </summary>
        public async Task<IActionResult> ProcessPutaway(int asnId)
        {
            try
            {
                _logger.LogInformation("Loading ProcessPutaway form for ASN {ASNId}", asnId);
                
                if (asnId <= 0)
                {
                    _logger.LogWarning("Invalid ASN ID: {ASNId}", asnId);
                    TempData["ErrorMessage"] = "ASN ID tidak valid.";
                    return RedirectToAction(nameof(Putaway));
                }

                var viewModel = await _inventoryService.GetPutawayViewModelAsync(asnId);
                
                _logger.LogInformation("Successfully loaded ProcessPutaway form for ASN {ASNId} with {ItemCount} items", 
                    asnId, viewModel.PutawayDetails?.Count ?? 0);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading process putaway form for ASN {ASNId}", asnId);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat form process putaway.";
                return RedirectToAction(nameof(Putaway));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPutaway(int asnDetailId, int quantityToPutaway, int locationId, int asnId, int itemId)
        {
            try
            {
                _logger.LogInformation("ProcessPutaway POST - Received parameters: asnDetailId={ASNDetailId}, quantityToPutaway={QuantityToPutaway}, locationId={LocationId}, asnId={ASNId}, itemId={ItemId}", 
                    asnDetailId, quantityToPutaway, locationId, asnId, itemId);

                // Enhanced parameter validation
                if (asnDetailId <= 0)
                {
                    _logger.LogWarning("Invalid asnDetailId: {ASNDetailId}", asnDetailId);
                    TempData["ErrorMessage"] = "ASN Detail ID tidak valid.";
                    return RedirectToAction(nameof(ProcessPutaway), new { asnId });
                }

                if (quantityToPutaway <= 0)
                {
                    _logger.LogWarning("Invalid quantityToPutaway: {QuantityToPutaway}", quantityToPutaway);
                    TempData["ErrorMessage"] = "Quantity tidak valid.";
                    return RedirectToAction(nameof(ProcessPutaway), new { asnId });
                }

                if (locationId <= 0)
                {
                    _logger.LogWarning("Invalid locationId: {LocationId}", locationId);
                    TempData["ErrorMessage"] = "Location ID tidak valid.";
                    return RedirectToAction(nameof(ProcessPutaway), new { asnId });
                }

                if (itemId <= 0)
                {
                    _logger.LogWarning("Invalid itemId: {ItemId}", itemId);
                    TempData["ErrorMessage"] = "Item ID tidak valid.";
                    return RedirectToAction(nameof(ProcessPutaway), new { asnId });
                }

                if (asnId <= 0)
                {
                    _logger.LogWarning("Invalid asnId: {ASNId}", asnId);
                    TempData["ErrorMessage"] = "ASN ID tidak valid.";
                    return RedirectToAction(nameof(ProcessPutaway), new { asnId });
                }

                // Create PutawayDetailViewModel from parameters
                var putawayDetail = new PutawayDetailViewModel
                {
                    ASNId = asnId,
                    ASNDetailId = asnDetailId,
                    ItemId = itemId,
                    QuantityToPutaway = quantityToPutaway,
                    LocationId = locationId
                };

                _logger.LogInformation("Created PutawayDetailViewModel: ASNId={ASNId}, ASNDetailId={ASNDetailId}, ItemId={ItemId}, QuantityToPutaway={QuantityToPutaway}, LocationId={LocationId}", 
                    putawayDetail.ASNId, putawayDetail.ASNDetailId, putawayDetail.ItemId, putawayDetail.QuantityToPutaway, putawayDetail.LocationId);

                var success = await _inventoryService.ProcessPutawayAsync(putawayDetail);
                if (success)
                {
                    _logger.LogInformation("Putaway processed successfully for ASN Detail {ASNDetailId}", asnDetailId);
                    TempData["SuccessMessage"] = "Putaway berhasil diproses.";
                }
                else
                {
                    _logger.LogWarning("Putaway processing failed for ASN Detail {ASNDetailId}. Check logs for detailed error information.", asnDetailId);
                    TempData["ErrorMessage"] = "Gagal memproses putaway. Silakan periksa log untuk detail error.";
                }

                return RedirectToAction(nameof(ProcessPutaway), new { asnId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing putaway for ASN Detail {ASNDetailId}. Exception: {ExceptionMessage}", asnDetailId, ex.Message);
                TempData["ErrorMessage"] = $"Terjadi kesalahan saat memproses putaway: {ex.Message}";
                return RedirectToAction(nameof(ProcessPutaway), new { asnId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBulkPutaway(int asnId, List<PutawayDetailViewModel> putawayDetails)
        {
            try
            {
                var success = await _inventoryService.ProcessBulkPutawayAsync(putawayDetails);
                if (success)
                {
                    TempData["SuccessMessage"] = "Bulk putaway berhasil diproses.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Gagal memproses bulk putaway.";
                }

                return RedirectToAction(nameof(ProcessPutaway), new { asnId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk putaway for ASN {ASNId}", asnId);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memproses bulk putaway.";
                return RedirectToAction(nameof(ProcessPutaway), new { asnId });
            }
        }

        #endregion

        #region Helper Methods

        private async Task<InventorySummaryViewModel> GetInventorySummaryAsync()
        {
            try
            {
                var inventories = await _inventoryService.GetAllInventoriesAsync();
                var statistics = await _inventoryService.GetInventoryStatisticsAsync();

                return new InventorySummaryViewModel
                {
                    TotalItems = inventories.Sum(inv => inv.Quantity),
                    TotalValue = inventories.Sum(inv => inv.TotalValue),
                    AvailableStock = inventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_AVAILABLE).Sum(inv => inv.Quantity),
                    ReservedStock = inventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_RESERVED).Sum(inv => inv.Quantity),
                    DamagedStock = inventories.Where(inv => inv.Status == Constants.INVENTORY_STATUS_DAMAGED).Sum(inv => inv.Quantity),
                    LowStockCount = inventories.Count(inv => inv.NeedsReorder),
                    EmptyLocationCount = inventories.Count(inv => inv.Quantity == 0),
                    StatusBreakdown = inventories.GroupBy(inv => inv.Status)
                        .ToDictionary(g => g.Key, g => g.Sum(inv => inv.Quantity))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory summary");
                return new InventorySummaryViewModel();
            }
        }

        private async Task<PutawaySummaryViewModel> GetPutawaySummaryAsync()
        {
            try
            {
                var asns = await _inventoryService.GetASNsReadyForPutawayAsync();
                
                return new PutawaySummaryViewModel
                {
                    TotalProcessedASNs = asns.Count(),
                    TotalPendingItems = asns.Sum(asn => asn.ASNDetails?.Count ?? 0),
                    TotalPendingQuantity = asns.Sum(asn => asn.ASNDetails?.Sum(ad => ad.ShippedQuantity) ?? 0),
                    TodayPutawayCount = asns.Count(asn => asn.ActualArrivalDate?.Date == DateTime.Today),
                    OldestPendingASN = asns.OrderBy(asn => asn.ActualArrivalDate).FirstOrDefault()?.ASNNumber,
                    DaysSinceOldest = asns.Any() ? (DateTime.Today - asns.OrderBy(asn => asn.ActualArrivalDate).First().ActualArrivalDate?.Date).Value.Days : null,
                    StatusBreakdown = asns.GroupBy(asn => asn.Status)
                        .ToDictionary(g => g.Key, g => g.Count())
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting putaway summary");
                return new PutawaySummaryViewModel();
            }
        }

        #endregion

        #region AJAX Methods

        /// <summary>
        /// Get location capacity info for AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocationCapacityInfo(int locationId)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(locationId);
                if (location == null)
                {
                    return Json(new { success = false, message = "Location not found" });
                }

                // Get current capacity from inventories
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Json(new { success = false, message = "Company ID not available" });
                }

                var currentCapacity = await _context.Inventories
                    .Where(inv => inv.LocationId == locationId && inv.CompanyId == companyId.Value)
                    .SumAsync(inv => inv.Quantity);

                var availableCapacity = location.MaxCapacity - currentCapacity;
                var capacityPercentage = location.MaxCapacity > 0 ? (double)currentCapacity / location.MaxCapacity * 100 : 0;

                var capacityInfo = new
                {
                    success = true,
                    locationId = location.Id,
                    locationCode = location.Code,
                    locationName = location.Name,
                    maxCapacity = location.MaxCapacity,
                    currentCapacity = currentCapacity,
                    availableCapacity = availableCapacity,
                    capacityPercentage = Math.Round(capacityPercentage, 1),
                    isFull = currentCapacity >= location.MaxCapacity,
                    status = currentCapacity >= location.MaxCapacity ? "PENUH" :
                            availableCapacity <= 5 ? "KRITIS" :
                            availableCapacity <= 20 ? "HAMPIR PENUH" :
                            currentCapacity > 0 ? "TERSEDIA" : "KOSONG",
                    statusClass = currentCapacity >= location.MaxCapacity ? Constants.STATUS_DANGER :
                                availableCapacity <= 5 ? Constants.STATUS_DANGER :
                                availableCapacity <= 20 ? Constants.STATUS_WARNING : Constants.STATUS_SUCCESS
                };

                return Json(capacityInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location capacity info for location {LocationId}", locationId);
                return Json(new { success = false, message = "Error retrieving location capacity info" });
            }
        }

        /// <summary>
        /// Check adjust stock capacity for AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckAdjustStockCapacity(int locationId, int currentInventoryQuantity, int newQuantity)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(locationId);
                if (location == null)
                {
                    return Json(new { success = false, message = "Location not found" });
                }

                // Get current capacity from inventories
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    return Json(new { success = false, message = "Company ID not available" });
                }

                var currentCapacity = await _context.Inventories
                    .Where(inv => inv.LocationId == locationId && inv.CompanyId == companyId.Value)
                    .SumAsync(inv => inv.Quantity);

                var capacityAfterAdjustment = currentCapacity - currentInventoryQuantity + newQuantity;
                var availableCapacity = location.MaxCapacity - currentCapacity;
                var availableCapacityAfterAdjustment = location.MaxCapacity - capacityAfterAdjustment;

                var result = new
                {
                    success = true,
                    locationId = location.Id,
                    locationCode = location.Code,
                    locationName = location.Name,
                    maxCapacity = location.MaxCapacity,
                    currentCapacity = currentCapacity,
                    availableCapacity = availableCapacity,
                    capacityAfterAdjustment = capacityAfterAdjustment,
                    availableCapacityAfterAdjustment = availableCapacityAfterAdjustment,
                    wouldExceedCapacity = capacityAfterAdjustment > location.MaxCapacity,
                    canAccommodate = capacityAfterAdjustment <= location.MaxCapacity,
                    warningMessage = capacityAfterAdjustment > location.MaxCapacity ? 
                        $"Penyesuaian ini akan melebihi kapasitas maksimal lokasi ({location.MaxCapacity}). Kapasitas setelah penyesuaian: {capacityAfterAdjustment}" :
                        availableCapacityAfterAdjustment <= 5 ? 
                            $"Peringatan: Kapasitas tersisa setelah penyesuaian hanya {availableCapacityAfterAdjustment} unit" :
                        availableCapacityAfterAdjustment <= 20 ? 
                            $"Peringatan: Kapasitas tersisa setelah penyesuaian hanya {availableCapacityAfterAdjustment} unit" :
                        null
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking adjust stock capacity for location {LocationId}", locationId);
                return Json(new { success = false, message = "Error checking capacity constraints" });
            }
        }

        #endregion
    }
}