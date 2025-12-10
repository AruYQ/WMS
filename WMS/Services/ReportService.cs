using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WMS.Data;
using WMS.Models;
using WMS.Models.ViewModels;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service untuk report generation
    /// Creates Inbound, Outbound, and Inventory reports for Admin
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ReportService(
            ApplicationDbContext context,
            ILogger<ReportService> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Generate Inbound report (PO + ASN only) dengan filtering lengkap
        /// </summary>
        public async Task<InboundReportData> GenerateInboundReportAsync(
            InboundReportRequest request,
            int companyId)
        {
            try
            {
                // Determine date ranges - use custom if provided, otherwise use main date range
                var poFromDate = request.POFromDate ?? request.FromDate;
                var poToDate = request.POToDate ?? request.ToDate;
                var asnFromDate = request.ASNFromDate ?? request.FromDate;
                var asnToDate = request.ASNToDate ?? request.ToDate;

                var report = new InboundReportData
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                };

                // === PROCESS PURCHASE ORDERS ===
                if (request.IncludePO)
                {
                    var poQuery = _context.PurchaseOrders
                        .Where(po => po.CompanyId == companyId && 
                                     po.OrderDate >= poFromDate && 
                                     po.OrderDate <= poToDate &&
                                     !po.IsDeleted);

                    // Supplier filters (single or multiple)
                    if (request.SupplierIds != null && request.SupplierIds.Any())
                    {
                        poQuery = poQuery.Where(po => request.SupplierIds.Contains(po.SupplierId));
                    }
                    else if (request.SupplierId.HasValue)
                    {
                        poQuery = poQuery.Where(po => po.SupplierId == request.SupplierId.Value);
                    }

                    // PO Status filters (multi-select)
                    if (request.POStatuses != null && request.POStatuses.Any())
                    {
                        poQuery = poQuery.Where(po => request.POStatuses.Contains(po.Status));
                    }

                    // Cancelled filter
                    if (!request.IncludeCancelled)
                    {
                        poQuery = poQuery.Where(po => po.Status != Constants.PO_STATUS_CANCELLED);
                    }

                    // PO Number filter
                    if (!string.IsNullOrEmpty(request.PONumberFilter))
                    {
                        poQuery = poQuery.Where(po => po.PONumber.Contains(request.PONumberFilter));
                    }

                    // Amount range filters
                    if (request.MinPOAmount.HasValue)
                    {
                        poQuery = poQuery.Where(po => po.TotalAmount >= request.MinPOAmount.Value);
                    }
                    if (request.MaxPOAmount.HasValue)
                    {
                        poQuery = poQuery.Where(po => po.TotalAmount <= request.MaxPOAmount.Value);
                    }

                    var purchaseOrders = await poQuery
                        .Include(po => po.Supplier)
                        .Include(po => po.PurchaseOrderDetails)
                        .ToListAsync();

                    // Item count filter
                    if (request.MinItemsCount.HasValue || request.MaxItemsCount.HasValue)
                    {
                        purchaseOrders = purchaseOrders.Where(po =>
                        {
                            var itemCount = po.PurchaseOrderDetails.Count;
                            return (!request.MinItemsCount.HasValue || itemCount >= request.MinItemsCount.Value) &&
                                   (!request.MaxItemsCount.HasValue || itemCount <= request.MaxItemsCount.Value);
                        }).ToList();
                    }

                    report.TotalPurchaseOrders = purchaseOrders.Count;
                    report.TotalPOValue = purchaseOrders.Sum(po => po.TotalAmount);
                    
                    if ((request.SupplierId.HasValue || (request.SupplierIds != null && request.SupplierIds.Count == 1)) && purchaseOrders.Any())
                    {
                        var supplierId = request.SupplierIds?.FirstOrDefault() ?? request.SupplierId;
                        report.SupplierName = purchaseOrders.FirstOrDefault(po => po.SupplierId == supplierId)?.Supplier?.Name;
                    }

                    // Add PO lines
                    foreach (var po in purchaseOrders)
                    {
                        report.Lines.Add(new InboundReportData.InboundReportLine
                        {
                            Date = po.OrderDate,
                            DocumentNumber = po.PONumber,
                            Type = "PO",
                            SupplierName = po.Supplier?.Name ?? "Unknown",
                            Status = po.Status,
                            PONumber = po.PONumber,
                            TotalItems = po.PurchaseOrderDetails.Count, // Jumlah jenis item
                            TotalQuantity = po.PurchaseOrderDetails.Sum(d => d.Quantity), // Total quantity semua item
                            TotalAmount = po.TotalAmount
                        });
                    }
                }

                // === PROCESS ASNs ===
                if (request.IncludeASN)
                {
                    var asnQuery = _context.AdvancedShippingNotices
                        .Where(asn => asn.CompanyId == companyId &&
                                      !asn.IsDeleted &&
                                      asn.CreatedDate >= asnFromDate &&
                                      asn.CreatedDate <= asnToDate);

                    // Supplier filters
                    if (request.SupplierIds != null && request.SupplierIds.Any())
                    {
                        asnQuery = asnQuery.Where(asn => asn.PurchaseOrder != null && 
                                                          request.SupplierIds.Contains(asn.PurchaseOrder.SupplierId));
                    }
                    else if (request.SupplierId.HasValue)
                    {
                        asnQuery = asnQuery.Where(asn => asn.PurchaseOrder != null && 
                                                          asn.PurchaseOrder.SupplierId == request.SupplierId.Value);
                    }

                    // ASN Status filters (multi-select)
                    if (request.ASNStatuses != null && request.ASNStatuses.Any())
                    {
                        asnQuery = asnQuery.Where(asn => request.ASNStatuses.Contains(asn.Status));
                    }

                    // Cancelled filter
                    if (!request.IncludeCancelled)
                    {
                        asnQuery = asnQuery.Where(asn => asn.Status != Constants.ASN_STATUS_CANCELLED);
                    }

                    // ASN Number filter
                    if (!string.IsNullOrEmpty(request.ASNNumberFilter))
                    {
                        asnQuery = asnQuery.Where(asn => asn.ASNNumber.Contains(request.ASNNumberFilter));
                    }

                    var asns = await asnQuery
                        .Include(asn => asn.PurchaseOrder)
                            .ThenInclude(po => po!.Supplier)
                        .Include(asn => asn.ASNDetails)
                        .ToListAsync();

                    // Item count filter
                    if (request.MinItemsCount.HasValue || request.MaxItemsCount.HasValue)
                    {
                        asns = asns.Where(asn =>
                        {
                            var itemCount = asn.ASNDetails.Count;
                            return (!request.MinItemsCount.HasValue || itemCount >= request.MinItemsCount.Value) &&
                                   (!request.MaxItemsCount.HasValue || itemCount <= request.MaxItemsCount.Value);
                        }).ToList();
                    }

                    // Amount range filters
                    var asnsFiltered = new List<AdvancedShippingNotice>();
                    foreach (var asn in asns)
                    {
                        var asnAmount = asn.ASNDetails.Sum(d => d.ShippedQuantity * d.ActualPricePerItem);
                        if ((!request.MinASNAmount.HasValue || asnAmount >= request.MinASNAmount.Value) &&
                            (!request.MaxASNAmount.HasValue || asnAmount <= request.MaxASNAmount.Value))
                        {
                            asnsFiltered.Add(asn);
                        }
                    }
                    asns = asnsFiltered;

                    report.TotalASN = asns.Count;
                    report.TotalReceived = asns.Count(asn => asn.Status == Constants.ASN_STATUS_COMPLETED || 
                                                              asn.Status == Constants.ASN_STATUS_PROCESSED);
                    report.TotalASNValue = asns.Sum(asn => asn.ASNDetails.Sum(d => d.ShippedQuantity * d.ActualPricePerItem));

                    // Add ASN lines
                    foreach (var asn in asns)
                    {
                        var asnDate = (DateTime?)asn.CreatedDate
                                      ?? request.FromDate;

                        var supplierName = asn.PurchaseOrder?.Supplier?.Name;
                        var poNumber = asn.PurchaseOrder?.PONumber;
                        var asnNumber = asn.ASNNumber;
                        var status = asn.Status;

                        report.Lines.Add(new InboundReportData.InboundReportLine
                        {
                            Date = asnDate,
                            DocumentNumber = string.IsNullOrWhiteSpace(asnNumber) ? "-" : asnNumber,
                            Type = "ASN",
                            SupplierName = string.IsNullOrWhiteSpace(supplierName) ? "-" : supplierName,
                            Status = string.IsNullOrWhiteSpace(status) ? "-" : status,
                            ASNNumber = string.IsNullOrWhiteSpace(asnNumber) ? "-" : asnNumber,
                            PONumberForASN = string.IsNullOrWhiteSpace(poNumber) ? "-" : poNumber,
                            TotalItemsASN = asn.ASNDetails.Count, // Jumlah jenis item
                            TotalQuantityASN = asn.ASNDetails.Sum(d => d.ShippedQuantity), // Total quantity semua item
                            TotalAmountASN = asn.ASNDetails.Sum(d => d.ShippedQuantity * d.ActualPricePerItem)
                        });
                    }
                }

                // === PUTAWAY REMOVED: Inbound Report only includes PO and ASN ===

                // Apply sorting
                var lines = report.Lines.AsEnumerable();
                switch (request.SortBy?.ToLower())
                {
                    case "amount":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.TotalAmount ?? l.TotalAmountASN ?? 0)
                            : lines.OrderBy(l => l.TotalAmount ?? l.TotalAmountASN ?? 0);
                        break;
                    case "supplier":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.SupplierName)
                            : lines.OrderBy(l => l.SupplierName);
                        break;
                    case "documentnumber":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.DocumentNumber)
                            : lines.OrderBy(l => l.DocumentNumber);
                        break;
                    case "status":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.Status)
                            : lines.OrderBy(l => l.Status);
                        break;
                    default: // Date
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.Date)
                            : lines.OrderBy(l => l.Date);
                        break;
                }
                report.Lines = lines.ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inbound report for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Generate Outbound report (SO + Picking)
        /// </summary>
        public async Task<OutboundReportData> GenerateOutboundReportAsync(OutboundReportRequest request, int companyId)
        {
            try
            {
                var report = new OutboundReportData
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                };

                var salesOrderQuery = _context.SalesOrders
                    .Where(so => so.CompanyId == companyId &&
                                 so.OrderDate >= request.FromDate &&
                                 so.OrderDate <= request.ToDate &&
                                 !so.IsDeleted)
                    .Include(so => so.Customer)
                    .Include(so => so.SalesOrderDetails)
                    .AsQueryable();

                if (request.CustomerIds != null && request.CustomerIds.Any())
                {
                    salesOrderQuery = salesOrderQuery.Where(so => request.CustomerIds.Contains(so.CustomerId));
                }
                else if (request.CustomerId.HasValue)
                {
                    salesOrderQuery = salesOrderQuery.Where(so => so.CustomerId == request.CustomerId.Value);
                }

                if (request.Statuses != null && request.Statuses.Any())
                {
                    salesOrderQuery = salesOrderQuery.Where(so => request.Statuses.Contains(so.Status));
                }

                var salesOrders = await salesOrderQuery
                    .OrderBy(so => so.OrderDate)
                    .ToListAsync();

                report.TotalSalesOrders = salesOrders.Count;
                report.TotalValue = salesOrders.Sum(so => so.TotalAmount);
                report.TotalShipped = salesOrders.Count(so =>
                    so.Status == Constants.SO_STATUS_SHIPPED ||
                    so.Status == Constants.SO_STATUS_COMPLETED);

                foreach (var so in salesOrders)
                {
                    report.Lines.Add(new OutboundReportData.OutboundReportLine
                    {
                        Date = so.OrderDate,
                        DocumentNumber = so.SONumber,
                        Type = "SO",
                        CustomerName = so.Customer?.Name ?? "Unknown",
                        Status = so.Status,
                        TotalItems = so.SalesOrderDetails.Count, // Jumlah jenis item
                        TotalQuantity = so.SalesOrderDetails.Sum(d => d.Quantity), // Total quantity semua item
                        TotalAmount = so.TotalAmount
                    });
                }

                var includePickings = request.IncludePickings &&
                    (request.Statuses == null ||
                     !request.Statuses.Any() ||
                     request.Statuses.Contains(Constants.SO_STATUS_PICKING) ||
                     request.Statuses.Contains(Constants.PICKING_STATUS_IN_PROGRESS) ||
                     request.Statuses.Contains(Constants.PICKING_STATUS_COMPLETED));

                if (includePickings)
                {
                    var pickingQuery = _context.Pickings
                        .Where(p => p.CompanyId == companyId &&
                                    p.PickingDate >= request.FromDate &&
                                    p.PickingDate <= request.ToDate &&
                                    !p.IsDeleted)
                        .Include(p => p.SalesOrder)
                            .ThenInclude(so => so!.Customer)
                        .Include(p => p.PickingDetails)
                        .AsQueryable();

                    if (request.CustomerIds != null && request.CustomerIds.Any())
                    {
                        pickingQuery = pickingQuery.Where(p =>
                            p.SalesOrder != null &&
                            request.CustomerIds.Contains(p.SalesOrder.CustomerId));
                    }
                    else if (request.CustomerId.HasValue)
                    {
                        pickingQuery = pickingQuery.Where(p =>
                            p.SalesOrder != null &&
                            p.SalesOrder.CustomerId == request.CustomerId.Value);
                    }

                    var pickings = await pickingQuery
                        .OrderBy(p => p.PickingDate)
                        .ToListAsync();

                report.TotalPickings = pickings.Count;
                }

                report.Lines = report.Lines
                    .OrderBy(l => l.Date)
                    .ThenBy(l => l.DocumentNumber)
                    .ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating outbound report for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Generate Inventory Movement report
        /// </summary>
        public async Task<InventoryMovementReportData> GenerateInventoryMovementReportAsync(InventoryMovementReportRequest request, int companyId)
        {
            try
            {
                var report = new InventoryMovementReportData
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                };

                var movementLines = new List<InventoryMovementReportData.InventoryMovementLine>();
                var distinctItemIds = new HashSet<int>();

                if (request.IncludePutaway)
                {
                    var putawayDetailsQuery = _context.ASNDetails
                        .Where(ad =>
                            ad.ASN.CompanyId == companyId &&
                            !ad.IsDeleted &&
                            !ad.ASN.IsDeleted &&
                            ad.AlreadyPutAwayQuantity > 0 &&
                            ad.ASN.CreatedDate >= request.FromDate &&
                            ad.ASN.CreatedDate <= request.ToDate)
                        .Include(ad => ad.ASN)
                            .ThenInclude(asn => asn.PurchaseOrder)
                                .ThenInclude(po => po!.Supplier)
                        .Include(ad => ad.Item)
                        .AsQueryable();

                    if (request.ItemIds != null && request.ItemIds.Any())
                    {
                        putawayDetailsQuery = putawayDetailsQuery.Where(ad => request.ItemIds.Contains(ad.ItemId));
                    }
                    else if (request.ItemId.HasValue)
                    {
                        putawayDetailsQuery = putawayDetailsQuery.Where(ad => ad.ItemId == request.ItemId.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(request.ItemSearch))
                    {
                        var keyword = request.ItemSearch.Trim();
                        putawayDetailsQuery = putawayDetailsQuery.Where(ad =>
                            ad.Item != null &&
                            (EF.Functions.Like(ad.Item.ItemCode, $"%{keyword}%") ||
                             EF.Functions.Like(ad.Item.Name, $"%{keyword}%")));
                    }

                    var putawayDetails = await putawayDetailsQuery
                        .OrderBy(ad => ad.ASN.CreatedDate)
                        .ToListAsync();

                    var processedPutawayDetails = 0;

                    foreach (var detail in putawayDetails)
                    {
                        var totalPutawayQty = detail.AlreadyPutAwayQuantity;
                        if (totalPutawayQty <= 0)
                        {
                            continue;
                        }

                        processedPutawayDetails++;
                        report.TotalPutawayQuantity += totalPutawayQty;
                        report.TotalMovements++;
                        distinctItemIds.Add(detail.ItemId);

                        movementLines.Add(new InventoryMovementReportData.InventoryMovementLine
                        {
                            Date = detail.ASN.CreatedDate,
                            MovementType = "Putaway",
                            ItemCode = detail.Item?.ItemCode ?? "Unknown",
                            ItemName = detail.Item?.Name ?? "Unknown",
                            Quantity = totalPutawayQty,
                            Reference = detail.ASN.ASNNumber ?? $"ASNDetail:{detail.Id}",
                            SupplierName = detail.ASN.PurchaseOrder?.Supplier?.Name ?? "-"
                        });
                    }

                    report.TotalPutawayTransactions = processedPutawayDetails;
                }

                if (request.IncludePicking)
                {
                    var pickingDetailsQuery = _context.PickingDetails
                        .Where(pd =>
                            pd.Picking.CompanyId == companyId &&
                            !pd.IsDeleted &&
                            !pd.Picking.IsDeleted &&
                            pd.Picking.PickingDate >= request.FromDate &&
                            pd.Picking.PickingDate <= request.ToDate)
                        .Include(pd => pd.Picking)
                            .ThenInclude(pk => pk.SalesOrder)
                                .ThenInclude(so => so!.Customer)
                        .Include(pd => pd.Item)
                        .Include(pd => pd.Location)
                        .AsQueryable();

                    if (request.ItemIds != null && request.ItemIds.Any())
                    {
                        pickingDetailsQuery = pickingDetailsQuery.Where(pd => request.ItemIds.Contains(pd.ItemId));
                    }
                    else if (request.ItemId.HasValue)
                    {
                        pickingDetailsQuery = pickingDetailsQuery.Where(pd => pd.ItemId == request.ItemId.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(request.ItemSearch))
                    {
                        var keyword = request.ItemSearch.Trim();
                        pickingDetailsQuery = pickingDetailsQuery.Where(pd =>
                            EF.Functions.Like(pd.Item.ItemCode, $"%{keyword}%") ||
                            EF.Functions.Like(pd.Item.Name, $"%{keyword}%"));
                    }

                    var pickingDetails = await pickingDetailsQuery
                        .OrderBy(pd => pd.Picking.PickingDate)
                        .ToListAsync();

                    report.TotalPickingTransactions = pickingDetails
                        .Select(pd => pd.PickingId)
                        .Distinct()
                        .Count();

                    foreach (var detail in pickingDetails)
                    {
                        distinctItemIds.Add(detail.ItemId);

                        var quantity = detail.QuantityPicked > 0
                            ? detail.QuantityPicked
                            : detail.QuantityToPick;

                        report.TotalPickingQuantity += quantity;
                        report.TotalMovements++;

                        movementLines.Add(new InventoryMovementReportData.InventoryMovementLine
                        {
                            Date = detail.Picking.CompletedDate ?? detail.Picking.PickingDate,
                            MovementType = "Picking",
                            ItemCode = detail.Item?.ItemCode ?? "Unknown",
                            ItemName = detail.Item?.Name ?? "Unknown",
                            Quantity = quantity,
                            Reference = detail.Picking.PickingNumber,
                            DocumentNumber = detail.Picking.PickingNumber,
                            CustomerName = detail.Picking.SalesOrder?.Customer?.Name
                        });
                    }
                }

                report.TotalItemsInvolved = distinctItemIds.Count;
                report.Lines = movementLines
                    .OrderBy(line => line.Date)
                    .ThenBy(line => line.DocumentNumber ?? line.Reference)
                    .ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory movement report for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<StockReportData> GenerateStockReportAsync(StockReportRequest request, int companyId)
        {
            try
            {
                var report = new StockReportData
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    GeneratedAt = DateTime.Now
                };

                var inventoryQuery = _context.Inventories
                    .Where(inv => inv.CompanyId == companyId && !inv.IsDeleted)
                    .Include(inv => inv.Item)
                        .ThenInclude(item => item!.Supplier)
                    .Include(inv => inv.Location)
                    .AsQueryable();

                if (!request.IncludeZeroStock)
                {
                    inventoryQuery = inventoryQuery.Where(inv => inv.Quantity > 0);
                }

                if (request.SupplierId.HasValue)
                {
                    inventoryQuery = inventoryQuery.Where(inv =>
                        inv.Item != null && inv.Item.SupplierId == request.SupplierId.Value);
                }

                inventoryQuery = inventoryQuery.Where(inv =>
                    inv.Location != null && inv.Location.Category == Constants.LOCATION_CATEGORY_STORAGE);

                if (!string.IsNullOrWhiteSpace(request.ItemSearch))
                {
                    var keyword = request.ItemSearch.Trim();
                    inventoryQuery = inventoryQuery.Where(inv =>
                        inv.Item != null &&
                        (EF.Functions.Like(inv.Item.ItemCode, $"%{keyword}%") ||
                         EF.Functions.Like(inv.Item.Name, $"%{keyword}%")));
                }

                var inventories = await inventoryQuery.ToListAsync();

                if (inventories.Count == 0)
                {
                    return report;
                }

                var grouped = inventories.GroupBy(inv => inv.ItemId);

                foreach (var group in grouped)
                {
                    var sample = group.FirstOrDefault();
                    var item = sample?.Item;
                    var purchasePrice = item?.PurchasePrice ?? 0m;
                    var totalQuantity = group.Sum(inv => inv.Quantity);
                    var averageCost = Math.Round(purchasePrice, 2);
                    var totalValue = Math.Round(totalQuantity * purchasePrice, 2);

                    var locations = group
                        .Where(inv => inv.Location != null)
                        .Select(inv => inv.Location!.Code)
                        .Distinct()
                        .OrderBy(code => code)
                        .ToList();

                    report.Lines.Add(new StockReportData.StockReportLine
                    {
                        ItemId = group.Key,
                        ItemCode = item?.ItemCode ?? "Unknown",
                        ItemName = item?.Name ?? "Unknown",
                        Unit = item?.Unit ?? "PCS",
                        TotalQuantity = totalQuantity,
                        AverageCost = averageCost,
                        TotalValue = Math.Round(totalValue, 2),
                        LocationCount = locations.Count,
                        Locations = locations
                    });
                }

                report.TotalDistinctItems = report.Lines.Count;
                report.TotalQuantity = report.Lines.Sum(line => line.TotalQuantity);
                report.TotalInventoryValue = report.Lines.Sum(line => line.TotalValue);

                report.Lines = report.Lines
                    .OrderByDescending(line => line.TotalQuantity)
                    .ThenBy(line => line.ItemCode)
                    .ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating stock report for company {CompanyId}", companyId);
                throw;
            }
        }

        /// <summary>
        /// Export report to Excel (using ClosedXML)
        /// </summary>
        public async Task<SupplierReportData> GenerateSupplierReportAsync(SupplierReportRequest request, int companyId)
        {
            try
            {
                var report = new SupplierReportData
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    GeneratedAt = DateTime.Now
                };

                var supplierQuery = _context.Suppliers
                    .Where(s => s.CompanyId == companyId && !s.IsDeleted)
                    .Include(s => s.PurchaseOrders.Where(po => !po.IsDeleted))
                    .Include(s => s.Items.Where(i => !i.IsDeleted))
                    .AsQueryable();

                // Filter by active status
                if (request.IsActive.HasValue)
                {
                    supplierQuery = supplierQuery.Where(s => s.IsActive == request.IsActive.Value);
                }

                // Search filter
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var keyword = request.Search.Trim();
                    supplierQuery = supplierQuery.Where(s =>
                        s.Name.Contains(keyword) ||
                        (s.Email != null && s.Email.Contains(keyword)) ||
                        (s.Code != null && s.Code.Contains(keyword)));
                }

                var suppliers = await supplierQuery.ToListAsync();

                report.TotalSuppliers = suppliers.Count;
                report.ActiveSuppliers = suppliers.Count(s => s.IsActive);
                report.InactiveSuppliers = suppliers.Count(s => !s.IsActive);

                foreach (var supplier in suppliers)
                {
                    // Calculate statistics for this supplier within date range
                    var purchaseOrders = supplier.PurchaseOrders
                        .Where(po => po.OrderDate >= request.FromDate && po.OrderDate <= request.ToDate)
                        .ToList();

                    var totalPO = purchaseOrders.Count;
                    var totalItems = supplier.Items.Count;
                    var totalPOValue = purchaseOrders.Sum(po => po.TotalAmount);
                    var lastPODate = purchaseOrders.Any() ? purchaseOrders.Max(po => po.OrderDate) : (DateTime?)null;

                    report.TotalPurchaseOrders += totalPO;
                    report.TotalItems += totalItems;
                    report.TotalPOValue += totalPOValue;

                    report.Lines.Add(new SupplierReportData.SupplierReportLine
                    {
                        SupplierId = supplier.Id,
                        SupplierName = supplier.Name,
                        Code = supplier.Code,
                        Email = supplier.Email,
                        Phone = supplier.Phone,
                        Address = supplier.Address,
                        City = supplier.City,
                        ContactPerson = supplier.ContactPerson,
                        IsActive = supplier.IsActive,
                        TotalPurchaseOrders = totalPO,
                        TotalItems = totalItems,
                        TotalPOValue = totalPOValue,
                        LastPODate = lastPODate
                    });
                }

                // Apply sorting
                var lines = report.Lines.AsEnumerable();
                switch (request.SortBy?.ToLower())
                {
                    case "totalpo":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.TotalPurchaseOrders)
                            : lines.OrderBy(l => l.TotalPurchaseOrders);
                        break;
                    case "totalitems":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.TotalItems)
                            : lines.OrderBy(l => l.TotalItems);
                        break;
                    case "totalvalue":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.TotalPOValue)
                            : lines.OrderBy(l => l.TotalPOValue);
                        break;
                    default: // Name
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.SupplierName)
                            : lines.OrderBy(l => l.SupplierName);
                        break;
                }
                report.Lines = lines.ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating supplier report for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<CustomerReportData> GenerateCustomerReportAsync(CustomerReportRequest request, int companyId)
        {
            try
            {
                var report = new CustomerReportData
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    GeneratedAt = DateTime.Now
                };

                var customerQuery = _context.Customers
                    .Where(c => c.CompanyId == companyId && !c.IsDeleted)
                    .Include(c => c.SalesOrders.Where(so => !so.IsDeleted))
                    .AsQueryable();

                // Filter by active status
                if (request.IsActive.HasValue)
                {
                    customerQuery = customerQuery.Where(c => c.IsActive == request.IsActive.Value);
                }

                // Filter by customer type
                if (!string.IsNullOrWhiteSpace(request.CustomerType))
                {
                    customerQuery = customerQuery.Where(c => c.CustomerType == request.CustomerType);
                }

                // Search filter
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var keyword = request.Search.Trim();
                    customerQuery = customerQuery.Where(c =>
                        c.Name.Contains(keyword) ||
                        (c.Email != null && c.Email.Contains(keyword)) ||
                        (c.Code != null && c.Code.Contains(keyword)));
                }

                var customers = await customerQuery.ToListAsync();

                report.TotalCustomers = customers.Count;
                report.ActiveCustomers = customers.Count(c => c.IsActive);
                report.InactiveCustomers = customers.Count(c => !c.IsActive);

                foreach (var customer in customers)
                {
                    // Calculate statistics for this customer within date range
                    var salesOrders = customer.SalesOrders
                        .Where(so => so.OrderDate >= request.FromDate && so.OrderDate <= request.ToDate)
                        .ToList();

                    var totalSO = salesOrders.Count;
                    var totalSOValue = salesOrders.Sum(so => so.TotalAmount);
                    var lastSODate = salesOrders.Any() ? salesOrders.Max(so => so.OrderDate) : (DateTime?)null;

                    report.TotalSalesOrders += totalSO;
                    report.TotalSOValue += totalSOValue;

                    report.Lines.Add(new CustomerReportData.CustomerReportLine
                    {
                        CustomerId = customer.Id,
                        CustomerName = customer.Name,
                        Code = customer.Code,
                        Email = customer.Email,
                        Phone = customer.Phone,
                        Address = customer.Address,
                        City = customer.City,
                        CustomerType = customer.CustomerType,
                        IsActive = customer.IsActive,
                        TotalSalesOrders = totalSO,
                        TotalSOValue = totalSOValue,
                        LastSODate = lastSODate
                    });
                }

                // Apply sorting
                var lines = report.Lines.AsEnumerable();
                switch (request.SortBy?.ToLower())
                {
                    case "totalso":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.TotalSalesOrders)
                            : lines.OrderBy(l => l.TotalSalesOrders);
                        break;
                    case "totalvalue":
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.TotalSOValue)
                            : lines.OrderBy(l => l.TotalSOValue);
                        break;
                    default: // Name
                        lines = request.SortOrder?.ToUpper() == "DESC"
                            ? lines.OrderByDescending(l => l.CustomerName)
                            : lines.OrderBy(l => l.CustomerName);
                        break;
                }
                report.Lines = lines.ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating customer report for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(ReportExportRequest request, int companyId)
        {
            try
            {
                // TODO: Implement Excel export using ClosedXML
                _logger.LogWarning("Excel export not yet implemented for {ReportType}", request.ReportType);
                await Task.CompletedTask; // Fix async warning
                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                throw;
            }
        }

        /// <summary>
        /// Export report to PDF menggunakan QuestPDF
        /// </summary>
        public async Task<byte[]> ExportToPdfAsync(ReportExportRequest request, int companyId)
        {
            try
            {
                var reportType = request.ReportType.ToLower();
                QuestPDF.Settings.License = LicenseType.Community;

                if (reportType == "inbound")
                {
                    // Scope block untuk memisahkan variabel dari blok lain
                    {
                        // Convert ReportExportRequest to InboundReportRequest
                        var inboundRequest = new InboundReportRequest
                        {
                            FromDate = request.FromDate,
                            ToDate = request.ToDate,
                            IncludePO = request.IncludePO,
                            IncludeASN = request.IncludeASN,
                            IncludePutaway = request.IncludePutaway,
                            SupplierId = request.SupplierId,
                            SupplierIds = request.SupplierIds,
                            POStatuses = request.POStatuses,
                            ASNStatuses = request.ASNStatuses,
                            IncludeCancelled = request.IncludeCancelled,
                            PONumberFilter = request.PONumberFilter,
                            ASNNumberFilter = request.ASNNumberFilter,
                            POFromDate = request.POFromDate,
                            POToDate = request.POToDate,
                            ASNFromDate = request.ASNFromDate,
                            ASNToDate = request.ASNToDate,
                            PutawayFromDate = request.PutawayFromDate,
                            PutawayToDate = request.PutawayToDate,
                            MinPOAmount = request.MinPOAmount,
                            MaxPOAmount = request.MaxPOAmount,
                            MinASNAmount = request.MinASNAmount,
                            MaxASNAmount = request.MaxASNAmount,
                            LocationId = request.LocationId,
                            LocationIds = request.LocationIds,
                            LocationCodeFilter = request.LocationCodeFilter,
                            LocationCategoryFilter = request.LocationCategoryFilter,
                            MinPutawayQuantity = request.MinPutawayQuantity,
                            MaxPutawayQuantity = request.MaxPutawayQuantity,
                            MinItemsCount = request.MinItemsCount,
                            MaxItemsCount = request.MaxItemsCount,
                            SortBy = request.SortBy,
                            SortOrder = request.SortOrder
                        };

                        // Generate report data
                        var reportData = await GenerateInboundReportAsync(inboundRequest, companyId);

                        // Get logo path
                        var logoPath = GetLogoPath();

                        // Generate PDF using QuestPDF
                        var pdfDocument = Document.Create(container =>
                        {
                            container.Page(page =>
                            {
                                page.Size(PageSizes.A4);
                                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

                                // Modern Header with Logo
                                page.Header()
                                    .Background(Colors.Blue.Lighten5)
                                    .Padding(15)
                                    .BorderBottom(2)
                                    .BorderColor(Colors.Blue.Darken2)
                                    .Row(row =>
                                    {
                                        // Logo di kiri
                                        row.ConstantItem(130).AlignCenter().AlignMiddle().Column(logoCol =>
                                        {
                                            var logoContainer = logoCol.Item().Width(120).Height(80)
                                                .AlignCenter()
                                                .AlignMiddle();
                                            
                                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                            {
                                                logoContainer.Image(logoPath, ImageScaling.FitArea);
                                            }
                                            else
                                            {
                                                logoContainer.Column(innerCol =>
                                                {
                                                    innerCol.Item().AlignCenter().AlignMiddle()
                                                        .Text("LOGO")
                                                        .FontSize(10)
                                                        .FontColor(Colors.Grey.Darken1)
                                                        .Bold();
                                                });
                                            }
                                        });
                                        
                                        // Content di kanan
                                        row.RelativeItem().PaddingLeft(10).Column(column =>
                                        {
                                            column.Item().Text("INBOUND REPORT")
                                                .FontSize(24)
                                                .Bold()
                                                .FontColor(Colors.Blue.Darken2);
                                            
                                            column.Item().PaddingTop(3).Text("Jl. Parang Tritis Raya Komplek Indo Ruko Lodan No. 1AB, Jakarta Utara - Indonesia")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            
                                            column.Item().PaddingTop(2).Column(phoneCol =>
                                            {
                                                phoneCol.Item().Text("+6221 698 300 38 (Kantor)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                                phoneCol.Item().Text("+62812 8505 9678 (Info Marketing - WA Only)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                                phoneCol.Item().Text("+62811 1562 085 (CS)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                            });
                                            
                                            column.Item().PaddingTop(5).Row(periodRow =>
                                            {
                                                periodRow.RelativeItem().Text($"Period: {request.FromDate:dd MMMM yyyy} - {request.ToDate:dd MMMM yyyy}")
                                                    .FontSize(11)
                                                    .FontColor(Colors.Grey.Darken1);
                                            });
                                            
                                            if (reportData.SupplierName != null)
                                            {
                                                column.Item().PaddingTop(3).Text($"Supplier: {reportData.SupplierName}")
                                                    .FontSize(11)
                                                    .FontColor(Colors.Grey.Darken1);
                                            }
                                        });
                                    });

                                page.Content().PaddingVertical(10).Column(column =>
                                {
                                    // Summary Section with Card-like Styling
                                    column.Item().PaddingBottom(10)
                                        .Background(Colors.Grey.Lighten4)
                                        .Padding(12)
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .Column(summaryColumn =>
                                        {
                                            summaryColumn.Item().PaddingBottom(8).Text("SUMMARY")
                                                .FontSize(13)
                                                .Bold()
                                                .FontColor(Colors.Blue.Darken2);
                                            
                                            summaryColumn.Item().Row(row =>
                                            {
                                                row.RelativeItem().Text($"Total Purchase Orders: {reportData.TotalPurchaseOrders}")
                                                    .FontSize(10);
                                                row.RelativeItem().Text($"Total ASN: {reportData.TotalASN}")
                                                    .FontSize(10);
                                            });
                                            
                                            summaryColumn.Item().PaddingTop(5).Row(row =>
                                            {
                                                row.RelativeItem().Text($"Total Received: {reportData.TotalReceived}")
                                                    .FontSize(10);
                                                row.RelativeItem().Text($"Total PO Value: {reportData.TotalPOValue:C}")
                                                    .FontSize(10);
                                            });
                                            
                                            summaryColumn.Item().PaddingTop(5).Text($"Total ASN Value: {reportData.TotalASNValue:C}")
                                                .FontSize(10);
                                        });

                                    // Table with Modern Styling - Prevent row splitting
                                    column.Item().PaddingTop(10).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2); // Date
                                            columns.RelativeColumn(3); // Document Number
                                            columns.RelativeColumn(2); // Type
                                            columns.RelativeColumn(3); // Supplier
                                            columns.RelativeColumn(2); // Status
                                            columns.RelativeColumn(2); // QTY
                                            columns.RelativeColumn(2); // Items
                                            columns.RelativeColumn(3); // Amount/Details
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderCellStyle).Text("Date").FontSize(9).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Document").FontSize(9).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Type").FontSize(9).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Supplier").FontSize(9).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Status").FontSize(9).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("QTY").FontSize(9).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Items").FontSize(9).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Amount/Details").FontSize(9).Bold();
                                        });

                                        // Table Rows with Alternating Colors - Prevent row splitting
                                        int rowIndex = 0;
                                        foreach (var line in reportData.Lines)
                                        {
                                            var isEven = rowIndex % 2 == 0;
                                            
                                            // Table cells - QuestPDF automatically handles page breaks
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Date.ToString("dd MMM yyyy")).FontSize(9);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.DocumentNumber).FontSize(9);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Type).FontSize(9);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.SupplierName).FontSize(9);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Status).FontSize(9);

                                            if (line.Type == "PO")
                                            {
                                                table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalQuantity ?? 0}").FontSize(9);
                                                table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalItems ?? 0}").FontSize(9);
                                                table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalAmount:C}").FontSize(9);
                                            }
                                            else // ASN
                                            {
                                                table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalQuantityASN ?? 0}").FontSize(9);
                                                table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalItemsASN ?? 0}").FontSize(9);
                                                table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalAmountASN:C}").FontSize(9);
                                            }
                                            
                                            rowIndex++;
                                        }
                                    });
                                    
                                    // Add spacing before footer
                                    column.Item().PaddingTop(20);
                                });

                                // Footer with Signatures - Use helper method
                                AddSignatureFooter(page);
                            });
                        });

                        return await Task.Run(() => pdfDocument.GeneratePdf());
                    }
                }
                else if (reportType == "outbound")
                {
                    // Scope block untuk memisahkan variabel dari blok lain
                    {
                        // Convert ReportExportRequest to OutboundReportRequest
                        var outboundRequest = new OutboundReportRequest
                        {
                            FromDate = request.FromDate,
                            ToDate = request.ToDate,
                            CustomerId = request.CustomerId,
                            CustomerIds = request.CustomerIds,
                            Statuses = request.Statuses,
                            IncludePickings = request.IncludePickings
                        };

                        var reportData = await GenerateOutboundReportAsync(outboundRequest, companyId);

                        // Get logo path
                        var logoPath = GetLogoPath();

                        var pdfDocument = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

                            // Modern Header with Logo
                            page.Header()
                                .Background(Colors.Green.Lighten5)
                                .Padding(15)
                                .BorderBottom(2)
                                .BorderColor(Colors.Green.Darken2)
                                .Row(row =>
                                {
                                    // Logo di kiri
                                    row.ConstantItem(130).AlignCenter().AlignMiddle().Column(logoCol =>
                                    {
                                        var logoContainer = logoCol.Item().Width(120).Height(80)
                                            .AlignCenter()
                                            .AlignMiddle();
                                        
                                        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                        {
                                            logoContainer.Image(logoPath, ImageScaling.FitArea);
                                        }
                                        else
                                        {
                                            logoContainer.Column(innerCol =>
                                            {
                                                innerCol.Item().AlignCenter().AlignMiddle()
                                                    .Text("LOGO")
                                                    .FontSize(10)
                                                    .FontColor(Colors.Grey.Darken1)
                                                    .Bold();
                                            });
                                        }
                                    });
                                    
                                    // Content di kanan
                                    row.RelativeItem().PaddingLeft(10).Column(column =>
                                    {
                                        column.Item().Text("OUTBOUND REPORT")
                                            .FontSize(24)
                                            .Bold()
                                            .FontColor(Colors.Green.Darken2);
                                        
                                        column.Item().PaddingTop(3).Text("Jl. Parang Tritis Raya Komplek Indo Ruko Lodan No. 1AB, Jakarta Utara - Indonesia")
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Darken1);
                                        
                                        column.Item().PaddingTop(2).Column(phoneCol =>
                                        {
                                            phoneCol.Item().Text("+6221 698 300 38 (Kantor)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            phoneCol.Item().Text("+62812 8505 9678 (Info Marketing - WA Only)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            phoneCol.Item().Text("+62811 1562 085 (CS)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                        });
                                        
                                        column.Item().PaddingTop(5).Text($"Period: {request.FromDate:dd MMMM yyyy} - {request.ToDate:dd MMMM yyyy}")
                                            .FontSize(11)
                                            .FontColor(Colors.Grey.Darken1);
                                    });
                                });

                            page.Content().PaddingVertical(10).Column(column =>
                            {
                                // Summary Section with Card-like Styling
                                column.Item().PaddingBottom(10)
                                    .Background(Colors.Grey.Lighten4)
                                    .Padding(12)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Column(summaryColumn =>
                                    {
                                            summaryColumn.Item().PaddingBottom(8).Text("SUMMARY")
                                                .FontSize(13)
                                                .Bold()
                                                .FontColor(Colors.Green.Darken2);
                                        
                                        summaryColumn.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text($"Total Sales Orders: {reportData.TotalSalesOrders}")
                                                .FontSize(10);
                                            row.RelativeItem().Text($"Total Pickings: {reportData.TotalPickings}")
                                                .FontSize(10);
                                        });
                                        
                                        summaryColumn.Item().PaddingTop(5).Row(row =>
                                        {
                                            row.RelativeItem().Text($"Total Shipped: {reportData.TotalShipped}")
                                                .FontSize(10);
                                            row.RelativeItem().Text($"Total Value: {reportData.TotalValue:C}")
                                                .FontSize(10);
                                        });
                                    });

                                // Table with Modern Styling
                                column.Item().PaddingTop(10).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(3);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCellStyle).Text("Date").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Document").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Type").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Customer").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Status").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("QTY").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Items").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Amount").FontSize(9).Bold();
                                    });

                                    // Table Rows with Alternating Colors
                                    int rowIndex = 0;
                                    foreach (var line in reportData.Lines)
                                    {
                                        var isEven = rowIndex % 2 == 0;
                                        
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Date.ToString("dd MMM yyyy")).FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.DocumentNumber).FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Type).FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.CustomerName).FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Status).FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalQuantity}").FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalItems}").FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalAmount:C}").FontSize(9);
                                        
                                        rowIndex++;
                                    }
                            });

                                    // Add spacing before footer
                                    column.Item().PaddingTop(20);
                            });

                            // Footer with Signatures - Use helper method
                            AddSignatureFooter(page);
                        });
                        });

                        return await Task.Run(() => pdfDocument.GeneratePdf());
                    }
                }
                else if (reportType == "inventory")
                {
                    // Scope block untuk memisahkan variabel dari blok lain
                    {
                        var inventoryRequest = new InventoryMovementReportRequest
                        {
                            FromDate = request.FromDate,
                            ToDate = request.ToDate,
                            IncludePutaway = request.IncludePutawayMovements,
                            IncludePicking = request.IncludePickingMovements,
                            ItemId = request.ItemId,
                            ItemIds = request.ItemIds,
                            ItemSearch = request.ItemSearch
                        };

                        var reportData = await GenerateInventoryMovementReportAsync(inventoryRequest, companyId);

                        // Get logo path
                        var logoPath = GetLogoPath();

                        var pdfDocument = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

                            // Modern Header with Logo
                            page.Header()
                                .Background(Colors.Cyan.Lighten5)
                                .Padding(15)
                                .BorderBottom(2)
                                .BorderColor(Colors.Cyan.Darken2)
                                .Row(row =>
                                {
                                    // Logo di kiri
                                    row.ConstantItem(130).AlignCenter().AlignMiddle().Column(logoCol =>
                                    {
                                        var logoContainer = logoCol.Item().Width(120).Height(80)
                                            .AlignCenter()
                                            .AlignMiddle();
                                        
                                        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                        {
                                            logoContainer.Image(logoPath, ImageScaling.FitArea);
                                        }
                                        else
                                        {
                                            logoContainer.Column(innerCol =>
                                            {
                                                innerCol.Item().AlignCenter().AlignMiddle()
                                                    .Text("LOGO")
                                                    .FontSize(10)
                                                    .FontColor(Colors.Grey.Darken1)
                                                    .Bold();
                                            });
                                        }
                                    });
                                    
                                    // Content di kanan
                                    row.RelativeItem().PaddingLeft(10).Column(column =>
                                    {
                                        column.Item().Text("PUTAWAY & PICKING REPORT")
                                            .FontSize(24)
                                            .Bold()
                                            .FontColor(Colors.Cyan.Darken2);
                                        
                                        column.Item().PaddingTop(3).Text("Jl. Parang Tritis Raya Komplek Indo Ruko Lodan No. 1AB, Jakarta Utara - Indonesia")
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Darken1);
                                        
                                        column.Item().PaddingTop(2).Column(phoneCol =>
                                        {
                                            phoneCol.Item().Text("+6221 698 300 38 (Kantor)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            phoneCol.Item().Text("+62812 8505 9678 (Info Marketing - WA Only)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            phoneCol.Item().Text("+62811 1562 085 (CS)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                        });
                                        
                                        column.Item().PaddingTop(5).Text($"Period: {request.FromDate:dd MMMM yyyy} - {request.ToDate:dd MMMM yyyy}")
                                            .FontSize(11)
                                            .FontColor(Colors.Grey.Darken1);
                                    });
                                });

                            page.Content().PaddingVertical(10).Column(column =>
                            {
                                // Summary Section with Card-like Styling
                                column.Item().PaddingBottom(10)
                                    .Background(Colors.Grey.Lighten4)
                                    .Padding(12)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Column(summaryColumn =>
                                    {
                                        summaryColumn.Item().PaddingBottom(8).Text("SUMMARY")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(Colors.Cyan.Darken2);
                                        
                                        summaryColumn.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text($"Total Movements: {reportData.TotalMovements}")
                                                .FontSize(10);
                                            row.RelativeItem().Text($"Total Putaway Qty: {reportData.TotalPutawayQuantity}")
                                                .FontSize(10);
                                        });
                                        
                                        summaryColumn.Item().PaddingTop(5).Row(row =>
                                        {
                                            row.RelativeItem().Text($"Total Picking Qty: {reportData.TotalPickingQuantity}")
                                                .FontSize(10);
                                            row.RelativeItem().Text($"Distinct Items: {reportData.TotalItemsInvolved}")
                                                .FontSize(10);
                                        });
                                    });

                                // Table with Modern Styling
                                column.Item().PaddingTop(10).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(3);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCellStyle).Text("Date").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Movement").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Item").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Quantity").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Reference").FontSize(9).Bold();
                                    });

                                    // Table Rows with Alternating Colors
                                    int rowIndex = 0;
                                    foreach (var line in reportData.Lines)
                                    {
                                        var isEven = rowIndex % 2 == 0;
                                        
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Date.ToString("dd MMM yyyy HH:mm")).FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.MovementType).FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.ItemCode} - {line.ItemName}").FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.Quantity}").FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Reference).FontSize(9);
                                        
                                        rowIndex++;
                                    }
                                });
                            });

                            // Footer with Signatures
                            AddSignatureFooter(page);
                        });
                        });

                        return await Task.Run(() => pdfDocument.GeneratePdf());
                    }
                }
                else if (reportType == "stock")
                {
                    // Scope block untuk memisahkan variabel dari blok lain
                    {
                        var stockRequest = new StockReportRequest
                        {
                            FromDate = request.FromDate,
                            ToDate = request.ToDate,
                            SupplierId = request.SupplierId,
                            Category = request.Category,
                            ItemSearch = request.ItemSearch,
                            IncludeZeroStock = request.IncludeZeroStock
                        };

                        var reportData = await GenerateStockReportAsync(stockRequest, companyId);

                        // Get logo path
                        var logoPath = GetLogoPath();

                        var pdfDocument = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

                            // Modern Header with Logo
                            page.Header()
                                .Background(Colors.Orange.Lighten5)
                                .Padding(15)
                                .BorderBottom(2)
                                .BorderColor(Colors.Orange.Darken2)
                                .Row(row =>
                                {
                                    // Logo di kiri
                                    row.ConstantItem(130).AlignCenter().AlignMiddle().Column(logoCol =>
                                    {
                                        var logoContainer = logoCol.Item().Width(120).Height(80)
                                            .AlignCenter()
                                            .AlignMiddle();
                                        
                                        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                        {
                                            logoContainer.Image(logoPath, ImageScaling.FitArea);
                                        }
                                        else
                                        {
                                            logoContainer.Column(innerCol =>
                                            {
                                                innerCol.Item().AlignCenter().AlignMiddle()
                                                    .Text("LOGO")
                                                    .FontSize(10)
                                                    .FontColor(Colors.Grey.Darken1)
                                                    .Bold();
                                            });
                                        }
                                    });
                                    
                                    // Content di kanan
                                    row.RelativeItem().PaddingLeft(10).Column(column =>
                                    {
                                        column.Item().Text("STOCK REPORT")
                                            .FontSize(24)
                                            .Bold()
                                            .FontColor(Colors.Orange.Darken2);
                                        
                                        column.Item().PaddingTop(3).Text("Jl. Parang Tritis Raya Komplek Indo Ruko Lodan No. 1AB, Jakarta Utara - Indonesia")
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Darken1);
                                        
                                        column.Item().PaddingTop(2).Column(phoneCol =>
                                        {
                                            phoneCol.Item().Text("+6221 698 300 38 (Kantor)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            phoneCol.Item().Text("+62812 8505 9678 (Info Marketing - WA Only)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            phoneCol.Item().Text("+62811 1562 085 (CS)")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                        });
                                        
                                        column.Item().PaddingTop(5).Text($"Period: {request.FromDate:dd MMMM yyyy} - {request.ToDate:dd MMMM yyyy}")
                                            .FontSize(11)
                                            .FontColor(Colors.Grey.Darken1);
                                    });
                                });

                            page.Content().PaddingVertical(10).Column(column =>
                            {
                                // Summary Section with Card-like Styling
                                column.Item().PaddingBottom(10)
                                    .Background(Colors.Grey.Lighten4)
                                    .Padding(12)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Column(summaryColumn =>
                                    {
                                        summaryColumn.Item().PaddingBottom(8).Text("SUMMARY")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(Colors.Orange.Darken2);
                                        
                                        summaryColumn.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text($"Total Items: {reportData.TotalDistinctItems}")
                                                .FontSize(10);
                                            row.RelativeItem().Text($"Total Quantity: {reportData.TotalQuantity}")
                                                .FontSize(10);
                                        });
                                        
                                        summaryColumn.Item().PaddingTop(5).Text($"Total Inventory Value: {reportData.TotalInventoryValue:C}")
                                            .FontSize(10);
                                    });

                                // Table with Modern Styling
                                column.Item().PaddingTop(10).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCellStyle).Text("Item").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Unit").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Total Qty").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Avg Cost").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Total Value").FontSize(9).Bold();
                                        header.Cell().Element(HeaderCellStyle).Text("Locations").FontSize(9).Bold();
                                    });

                                    // Table Rows with Alternating Colors
                                    int rowIndex = 0;
                                    foreach (var line in reportData.Lines)
                                    {
                                        var isEven = rowIndex % 2 == 0;
                                        
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.ItemCode} - {line.ItemName}").FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Unit).FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalQuantity}").FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.AverageCost:C}").FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalValue:C}").FontSize(9);
                                        table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.LocationCount}").FontSize(9);
                                        
                                        rowIndex++;
                                    }
                                });
                            });

                            // Footer with Signatures
                            AddSignatureFooter(page);
                        });
                        });

                        return await Task.Run(() => pdfDocument.GeneratePdf());
                    }
                }
                else if (reportType == "supplier")
                {
                    // Scope block untuk memisahkan variabel dari blok lain
                    {
                        var supplierRequest = new SupplierReportRequest
                        {
                            FromDate = request.FromDate,
                            ToDate = request.ToDate,
                            IsActive = request.SupplierIsActive,
                            Search = request.SupplierSearch,
                            SortBy = request.SupplierSortBy,
                            SortOrder = request.SupplierSortOrder
                        };

                        var reportData = await GenerateSupplierReportAsync(supplierRequest, companyId);

                        // Get logo path
                        var logoPath = GetLogoPath();

                        var pdfDocument = Document.Create(container =>
                        {
                            container.Page(page =>
                            {
                                page.Size(PageSizes.A4);
                                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

                                // Modern Header with Logo
                                page.Header()
                                    .Background(Colors.Red.Lighten5)
                                    .Padding(15)
                                    .BorderBottom(2)
                                    .BorderColor(Colors.Red.Darken2)
                                    .Row(row =>
                                    {
                                        // Logo di kiri
                                        row.ConstantItem(130).AlignCenter().AlignMiddle().Column(logoCol =>
                                        {
                                            var logoContainer = logoCol.Item().Width(120).Height(80)
                                                .AlignCenter()
                                                .AlignMiddle();
                                            
                                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                            {
                                                logoContainer.Image(logoPath, ImageScaling.FitArea);
                                            }
                                            else
                                            {
                                                logoContainer.Column(innerCol =>
                                                {
                                                    innerCol.Item().AlignCenter().AlignMiddle()
                                                        .Text("LOGO")
                                                        .FontSize(10)
                                                        .FontColor(Colors.Grey.Darken1)
                                                        .Bold();
                                                });
                                            }
                                        });
                                        
                                        // Content di kanan
                                        row.RelativeItem().PaddingLeft(10).Column(column =>
                                        {
                                            column.Item().Text("SUPPLIER REPORT")
                                                .FontSize(24)
                                                .Bold()
                                                .FontColor(Colors.Red.Darken2);
                                            
                                            column.Item().PaddingTop(3).Text("Jl. Parang Tritis Raya Komplek Indo Ruko Lodan No. 1AB, Jakarta Utara - Indonesia")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            
                                            column.Item().PaddingTop(2).Column(phoneCol =>
                                            {
                                                phoneCol.Item().Text("+6221 698 300 38 (Kantor)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                                phoneCol.Item().Text("+62812 8505 9678 (Info Marketing - WA Only)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                                phoneCol.Item().Text("+62811 1562 085 (CS)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                            });
                                            
                                            column.Item().PaddingTop(5).Text($"Period: {request.FromDate:dd MMMM yyyy} - {request.ToDate:dd MMMM yyyy}")
                                                .FontSize(11)
                                                .FontColor(Colors.Grey.Darken1);
                                        });
                                    });

                                page.Content().PaddingVertical(10).Column(column =>
                                {
                                    // Summary Section with Card-like Styling
                                    column.Item().PaddingBottom(10)
                                        .Background(Colors.Grey.Lighten4)
                                        .Padding(12)
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .Column(summaryColumn =>
                                        {
                                            summaryColumn.Item().PaddingBottom(8).Text("SUMMARY")
                                                .FontSize(13)
                                                .Bold()
                                                .FontColor(Colors.Red.Darken2);
                                            
                                            summaryColumn.Item().Row(row =>
                                            {
                                                row.RelativeItem().Text($"Total Suppliers: {reportData.TotalSuppliers}")
                                                    .FontSize(10);
                                                row.RelativeItem().Text($"Active: {reportData.ActiveSuppliers}")
                                                    .FontSize(10);
                                            });
                                            
                                            summaryColumn.Item().PaddingTop(5).Row(row =>
                                            {
                                                row.RelativeItem().Text($"Inactive: {reportData.InactiveSuppliers}")
                                                    .FontSize(10);
                                                row.RelativeItem().Text($"Total PO: {reportData.TotalPurchaseOrders}")
                                                    .FontSize(10);
                                            });
                                            
                                            summaryColumn.Item().PaddingTop(5).Row(row =>
                                            {
                                                row.RelativeItem().Text($"Total Items: {reportData.TotalItems}")
                                                    .FontSize(10);
                                                row.RelativeItem().Text($"Total PO Value: {reportData.TotalPOValue:C}")
                                                    .FontSize(10);
                                            });
                                        });

                                    // Table with Modern Styling
                                    column.Item().PaddingTop(10).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(3);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(1);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderCellStyle).Text("Supplier Name").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Code").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Email").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Phone").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("City").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Status").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Total PO").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Items").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("PO Value").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Last PO").FontSize(8).Bold();
                                        });

                                        // Table Rows with Alternating Colors
                                        int rowIndex = 0;
                                        foreach (var line in reportData.Lines)
                                        {
                                            var isEven = rowIndex % 2 == 0;
                                            
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.SupplierName).FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Code ?? "-").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Email).FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Phone ?? "-").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.City ?? "-").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.IsActive ? "Active" : "Inactive").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalPurchaseOrders}").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalItems}").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalPOValue:C}").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.LastPODate?.ToString("dd MMM yyyy") ?? "-").FontSize(8);
                                            
                                            rowIndex++;
                                        }
                                    });
                                });

                                // Footer with Signatures
                                AddSignatureFooter(page);
                            });
                        });

                        return await Task.Run(() => pdfDocument.GeneratePdf());
                    }
                }
                else if (reportType == "customer")
                {
                    // Scope block untuk memisahkan variabel dari blok lain
                    {
                        var customerRequest = new CustomerReportRequest
                        {
                            FromDate = request.FromDate,
                            ToDate = request.ToDate,
                            IsActive = request.CustomerIsActive,
                            Search = request.CustomerSearch,
                            CustomerType = request.CustomerType,
                            SortBy = request.CustomerSortBy,
                            SortOrder = request.CustomerSortOrder
                        };

                        var reportData = await GenerateCustomerReportAsync(customerRequest, companyId);

                        // Get logo path
                        var logoPath = GetLogoPath();

                        var pdfDocument = Document.Create(container =>
                        {
                            container.Page(page =>
                            {
                                page.Size(PageSizes.A4);
                                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);

                                // Modern Header with Logo
                                page.Header()
                                    .Background(Colors.Purple.Lighten5)
                                    .Padding(15)
                                    .BorderBottom(2)
                                    .BorderColor(Colors.Purple.Darken2)
                                    .Row(row =>
                                    {
                                        // Logo di kiri
                                        row.ConstantItem(130).AlignCenter().AlignMiddle().Column(logoCol =>
                                        {
                                            var logoContainer = logoCol.Item().Width(120).Height(80)
                                                .AlignCenter()
                                                .AlignMiddle();
                                            
                                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                            {
                                                logoContainer.Image(logoPath, ImageScaling.FitArea);
                                            }
                                            else
                                            {
                                                logoContainer.Column(innerCol =>
                                                {
                                                    innerCol.Item().AlignCenter().AlignMiddle()
                                                        .Text("LOGO")
                                                        .FontSize(10)
                                                        .FontColor(Colors.Grey.Darken1)
                                                        .Bold();
                                                });
                                            }
                                        });
                                        
                                        // Content di kanan
                                        row.RelativeItem().PaddingLeft(10).Column(column =>
                                        {
                                            column.Item().Text("CUSTOMER REPORT")
                                                .FontSize(24)
                                                .Bold()
                                                .FontColor(Colors.Purple.Darken2);
                                            
                                            column.Item().PaddingTop(3).Text("Jl. Parang Tritis Raya Komplek Indo Ruko Lodan No. 1AB, Jakarta Utara - Indonesia")
                                                .FontSize(9)
                                                .FontColor(Colors.Grey.Darken1);
                                            
                                            column.Item().PaddingTop(2).Column(phoneCol =>
                                            {
                                                phoneCol.Item().Text("+6221 698 300 38 (Kantor)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                                phoneCol.Item().Text("+62812 8505 9678 (Info Marketing - WA Only)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                                phoneCol.Item().Text("+62811 1562 085 (CS)")
                                                    .FontSize(9)
                                                    .FontColor(Colors.Grey.Darken1);
                                            });
                                            
                                            column.Item().PaddingTop(5).Text($"Period: {request.FromDate:dd MMMM yyyy} - {request.ToDate:dd MMMM yyyy}")
                                                .FontSize(11)
                                                .FontColor(Colors.Grey.Darken1);
                                        });
                                    });

                                page.Content().PaddingVertical(10).Column(column =>
                                {
                                    // Summary Section with Card-like Styling
                                    column.Item().PaddingBottom(10)
                                        .Background(Colors.Grey.Lighten4)
                                        .Padding(12)
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .Column(summaryColumn =>
                                        {
                                            summaryColumn.Item().PaddingBottom(8).Text("SUMMARY")
                                                .FontSize(13)
                                                .Bold()
                                                .FontColor(Colors.Purple.Darken2);
                                            
                                            summaryColumn.Item().Row(row =>
                                            {
                                                row.RelativeItem().Text($"Total Customers: {reportData.TotalCustomers}")
                                                    .FontSize(10);
                                                row.RelativeItem().Text($"Active: {reportData.ActiveCustomers}")
                                                    .FontSize(10);
                                            });
                                            
                                            summaryColumn.Item().PaddingTop(5).Row(row =>
                                            {
                                                row.RelativeItem().Text($"Inactive: {reportData.InactiveCustomers}")
                                                    .FontSize(10);
                                                row.RelativeItem().Text($"Total SO: {reportData.TotalSalesOrders}")
                                                    .FontSize(10);
                                            });
                                            
                                            summaryColumn.Item().PaddingTop(5).Text($"Total SO Value: {reportData.TotalSOValue:C}")
                                                .FontSize(10);
                                        });

                                    // Table with Modern Styling
                                    column.Item().PaddingTop(10).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(3);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(1);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(1);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderCellStyle).Text("Customer Name").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Code").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Email").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Phone").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("City").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Type").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Status").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Total SO").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("SO Value").FontSize(8).Bold();
                                            header.Cell().Element(HeaderCellStyle).Text("Last SO").FontSize(8).Bold();
                                        });

                                        // Table Rows with Alternating Colors
                                        int rowIndex = 0;
                                        foreach (var line in reportData.Lines)
                                        {
                                            var isEven = rowIndex % 2 == 0;
                                            
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.CustomerName).FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Code ?? "-").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Email).FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.Phone ?? "-").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.City ?? "-").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.CustomerType ?? "-").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.IsActive ? "Active" : "Inactive").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalSalesOrders}").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text($"{line.TotalSOValue:C}").FontSize(8);
                                            table.Cell().Element(isEven ? BodyCellStyle : AlternatingCellStyle).Text(line.LastSODate?.ToString("dd MMM yyyy") ?? "-").FontSize(8);
                                            
                                            rowIndex++;
                                        }
                                    });
                                });

                                // Footer with Signatures
                                AddSignatureFooter(page);
                            });
                        });

                        return await Task.Run(() => pdfDocument.GeneratePdf());
                    }
                }
                else
                {
                    _logger.LogWarning("PDF export not supported for report type: {ReportType}", request.ReportType);
                    return Array.Empty<byte>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to PDF");
                throw;
            }
        }

        /// <summary>
        /// Helper method for PDF cell styling - Header cells
        /// </summary>
        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Blue.Darken2)
                .Background(Colors.Blue.Lighten4)
                .Padding(8)
                .AlignCenter();
        }

        /// <summary>
        /// Helper method for PDF cell styling - Body cells
        /// </summary>
        private static IContainer BodyCellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Padding(6)
                .AlignLeft();
        }

        /// <summary>
        /// Helper method for PDF cell styling - Alternating row cells
        /// </summary>
        private static IContainer AlternatingCellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(Colors.Grey.Lighten5)
                .Padding(6)
                .AlignLeft();
        }

        /// <summary>
        /// Helper method for PDF cell styling - Legacy support
        /// </summary>
        private static IContainer CellStyle(IContainer container)
        {
            return BodyCellStyle(container);
        }

        /// <summary>
        /// Helper method to get logo image path
        /// </summary>
        private string GetLogoPath()
        {
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "css", "easygo.png");
            return File.Exists(logoPath) ? logoPath : null;
        }


        /// <summary>
        /// Helper method to add signature footer to PDF pages
        /// Uses MinimalBox to prevent footer from taking too much space and splitting content
        /// </summary>
        private static void AddSignatureFooter(PageDescriptor page)
        {
            page.Footer()
                .MinimalBox()
                .PaddingTop(15)
                .Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        // Left Signature - tetap di kiri
                        row.ConstantItem(200).Column(sigCol =>
                        {
                            sigCol.Item().Text("Dibuat Oleh,")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                            sigCol.Item().PaddingTop(40);
                            // Garis tidak full width, hanya sepanjang nama
                            sigCol.Item().AlignLeft()
                                .Width(150)
                                .BorderBottom(1)
                                .BorderColor(Colors.Black)
                                .PaddingBottom(2);
                            sigCol.Item().PaddingTop(5)
                                .Text("(Nama Pembuat)")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2)
                                .Italic();
                        });
                        
                        // Right Signature - di ujung kanan (menggunakan RelativeItem untuk mengambil sisa space)
                        row.RelativeItem().AlignRight().Column(sigCol =>
                        {
                            sigCol.Item().AlignLeft().Text("Disetujui Oleh,")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                            sigCol.Item().PaddingTop(40);
                            // Garis tidak full width, hanya sepanjang nama, align left
                            sigCol.Item().AlignLeft()
                                .Width(150)
                                .BorderBottom(1)
                                .BorderColor(Colors.Black)
                                .PaddingBottom(2);
                            sigCol.Item().PaddingTop(5)
                                .AlignLeft()
                                .Text("(Nama Penyetuju)")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2)
                                .Italic();
                        });
                    });
                    
                    column.Item().PaddingTop(10)
                        .AlignCenter()
                        .Text($"Generated on {DateTime.Now:dd MMMM yyyy HH:mm}")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });
        }
    }
}

