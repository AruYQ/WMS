using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    public class ASNRepository : Repository<AdvancedShippingNotice>, IASNRepository
    {
        // Fixed constructor - now matches the base Repository constructor
        public ASNRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<Repository<AdvancedShippingNotice>> logger) 
            : base(context, currentUserService, logger)
        {
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetAllWithDetailsAsync()
        {
            return await GetBaseQuery() // Use GetBaseQuery() for automatic company filtering
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(asnD => asnD.Item)
                .OrderByDescending(asn => asn.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetProcessedASNsAsync()
        {
            return await GetBaseQuery()
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(detail => detail.Item)
                .Where(asn => asn.Status == "Processed")
                .OrderByDescending(asn => asn.ShipmentDate)
                .ToListAsync();
        }
        public async Task<AdvancedShippingNotice?> GetByIdWithDetailsAsync(int id)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                _logger.LogWarning("No company ID found for current user, returning null ASN");
                return null;
            }

            _logger.LogInformation("Getting ASN {ASNId} for company {CompanyId}", id, companyId.Value);

            var asn = await GetBaseQuery()
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(detail => detail.Item)
                .FirstOrDefaultAsync(asn => asn.Id == id);

            if (asn == null)
            {
                _logger.LogWarning("ASN {ASNId} not found for company {CompanyId}", id, companyId.Value);
            }
            else
            {
                _logger.LogInformation("Found ASN {ASNId}: {ASNNumber} with {DetailCount} details", 
                    id, asn.ASNNumber, asn.ASNDetails?.Count ?? 0);
            }

            return asn;
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetByPurchaseOrderAsync(int purchaseOrderId)
        {
            return await GetBaseQuery() // Use GetBaseQuery() for automatic company filtering
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(asnD => asnD.Item)
                .Where(asn => asn.PurchaseOrderId == purchaseOrderId)
                .OrderByDescending(asn => asn.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetByStatusAsync(ASNStatus status)
        {
            return await GetBaseQuery() // Use GetBaseQuery() for automatic company filtering
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Where(asn => asn.Status == status.ToString().Replace("InTransit", "In Transit"))
                .OrderByDescending(asn => asn.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetArrivedASNsAsync()
        {
            return await GetBaseQuery() // Use GetBaseQuery() for automatic company filtering
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(asnD => asnD.Item)
                .Where(asn => asn.Status == "Arrived")
                .OrderBy(asn => asn.ExpectedArrivalDate)
                .ToListAsync();
        }

        public async Task<bool> ExistsByASNNumberAsync(string asnNumber)
        {
            return await GetBaseQuery().AnyAsync(asn => asn.ASNNumber == asnNumber);
        }

        public async Task<string> GenerateNextASNNumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"ASN-{today:yyyy-MM-dd}-";

            var lastASN = await GetBaseQuery() // Use GetBaseQuery() for company-specific numbering
                .Where(asn => asn.ASNNumber.StartsWith(prefix))
                .OrderByDescending(asn => asn.ASNNumber)
                .FirstOrDefaultAsync();

            if (lastASN != null)
            {
                var lastNumber = lastASN.ASNNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int number))
                {
                    return $"{prefix}{(number + 1):D3}";
                }
            }

            return $"{prefix}001";
        }

        public async Task<AdvancedShippingNotice> CreateWithDetailsAsync(AdvancedShippingNotice asn)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generate ASN Number if not provided
                if (string.IsNullOrEmpty(asn.ASNNumber))
                {
                    asn.ASNNumber = await GenerateNextASNNumberAsync();
                }

                // Calculate warehouse fee for each detail
                foreach (var detail in asn.ASNDetails)
                {
                    detail.CalculateWarehouseFee();
                }

                // Use the base AddAsync method which handles CompanyId automatically
                var result = await AddAsync(asn);

                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        
        public async Task<ASNDetail?> GetASNDetailByIdAsync(int asnDetailId)
        {
            var companyId = _currentUserService.CompanyId;
            _logger.LogInformation("Getting ASN Detail {ASNDetailId} for CompanyId {CompanyId}", asnDetailId, companyId);
            
            if (!companyId.HasValue)
            {
                _logger.LogWarning("No company ID found for current user, returning null ASN detail");
                return null;
            }

            var asnDetail = await _context.ASNDetails
                .Include(d => d.Item)
                .Include(d => d.ASN)
                .ThenInclude(a => a.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
                .Where(d => d.Id == asnDetailId && d.CompanyId == companyId.Value)
                .FirstOrDefaultAsync();

            if (asnDetail == null)
            {
                _logger.LogWarning("ASN Detail {ASNDetailId} not found for CompanyId {CompanyId}", asnDetailId, companyId);
            }
            else
            {
                _logger.LogInformation("Found ASN Detail {ASNDetailId}: ShippedQuantity={ShippedQuantity}, RemainingQuantity={RemainingQuantity}", 
                    asnDetailId, asnDetail.ShippedQuantity, asnDetail.RemainingQuantity);
            }

            return asnDetail;
        }

        public async Task<int> GetPutAwayQuantityByASNDetailAsync(int asnDetailId)
        {
            var companyId = _currentUserService.CompanyId;
            if (!companyId.HasValue)
            {
                _logger.LogWarning("No company ID found for current user, returning 0 putaway quantity");
                return 0;
            }

            return await _context.Inventories
                .Where(i => i.CompanyId == companyId.Value &&
                           i.SourceReference.Contains($"ASN-") && 
                           i.SourceReference.Contains($"-{asnDetailId}"))
                .SumAsync(i => i.Quantity);
        }
        public async Task<bool> UpdateStatusAsync(int asnId, ASNStatus status)
        {
            var asn = await GetByIdAsync(asnId);
            if (asn == null) return false;

            asn.Status = status.ToString();
            asn.ModifiedDate = DateTime.Now;

            await UpdateAsync(asn);
            return true;
        }
        public async Task<IEnumerable<ASNDetail>> GetASNDetailsForPutawayAsync(int asnId)
        {
            try
            {
                var companyId = _currentUserService.CompanyId;
                if (!companyId.HasValue)
                {
                    _logger.LogWarning("No company ID found for current user, returning empty ASN details");
                    return new List<ASNDetail>();
                }

                var asnDetails = await _context.ASNDetails
                    .Include(ad => ad.Item)
                    .Include(ad => ad.ASN)
                    .Where(ad => ad.ASNId == asnId && 
                               ad.CompanyId == companyId.Value && 
                               ad.ShippedQuantity > 0)
                    .ToListAsync();

                // Calculate remaining quantities
                foreach (var detail in asnDetails)
                {
                    var putAwayQuantity = await _context.Inventories
                        .Where(inv => inv.ItemId == detail.ItemId &&
                                     inv.CompanyId == companyId.Value &&
                                     inv.SourceReference == $"ASN-{detail.ASNId}-{detail.Id}")
                        .SumAsync(inv => inv.Quantity);

                    detail.UpdatePutawayQuantity(putAwayQuantity);
                }

                return asnDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ASN details for putaway, ASN ID: {AsnId}", asnId);
                throw;
            }
        }

    }
}