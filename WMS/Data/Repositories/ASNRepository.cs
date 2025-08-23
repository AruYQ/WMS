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

        // Remove this method as it conflicts with base class GetByIdAsync
        // public async Task<AdvancedShippingNotice?> GetByIdAsync(int id)

        public async Task<AdvancedShippingNotice?> GetByIdWithDetailsAsync(int id)
        {
            return await GetBaseQuery() // Use GetBaseQuery() for automatic company filtering
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(asnD => asnD.Item)
                .FirstOrDefaultAsync(asn => asn.Id == id);
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

        // Remove this method as it conflicts with base class UpdateAsync
        // Use the base UpdateAsync method instead
        // public async Task UpdateAsync(AdvancedShippingNotice asn)

        public async Task UpdateStatusAsync(int id, ASNStatus status)
        {
            var asn = await GetByIdAsync(id);
            if (asn != null)
            {
                asn.Status = status.ToString().Replace("InTransit", "In Transit");
                await UpdateAsync(asn); // Use base class UpdateAsync
            }
        }
    }
}