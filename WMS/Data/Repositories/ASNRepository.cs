using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    public class ASNRepository : Repository<AdvancedShippingNotice>, IASNRepository
    {
        public ASNRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(asnD => asnD.Item)
                .OrderByDescending(asn => asn.CreatedDate)
                .ToListAsync();
        }
        public async Task<AdvancedShippingNotice?> GetByIdAsync(int id)
        {
            return await _context.AdvancedShippingNotices
                .FirstOrDefaultAsync(asn => asn.Id == id);
        }
        public async Task<AdvancedShippingNotice?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(asnD => asnD.Item)
                .FirstOrDefaultAsync(asn => asn.Id == id);
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetByPurchaseOrderAsync(int purchaseOrderId)
        {
            return await _dbSet
                .Include(asn => asn.ASNDetails)
                    .ThenInclude(asnD => asnD.Item)
                .Where(asn => asn.PurchaseOrderId == purchaseOrderId)
                .OrderByDescending(asn => asn.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetByStatusAsync(ASNStatus status)
        {
            return await _dbSet
                .Include(asn => asn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Where(asn => asn.Status == status.ToString().Replace("InTransit", "In Transit"))
                .OrderByDescending(asn => asn.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AdvancedShippingNotice>> GetArrivedASNsAsync()
        {
            return await _dbSet
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
            return await _dbSet.AnyAsync(asn => asn.ASNNumber == asnNumber);
        }

        public async Task<string> GenerateNextASNNumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"ASN-{today:yyyy-MM-dd}-";

            var lastASN = await _dbSet
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

                await _dbSet.AddAsync(asn);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return asn;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task UpdateAsync(AdvancedShippingNotice asn)
        {
            _context.AdvancedShippingNotices.Update(asn);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateStatusAsync(int id, ASNStatus status)
        {
            var asn = await GetByIdAsync(id);
            if (asn != null)
            {
                asn.Status = status.ToString().Replace("InTransit", "In Transit");
                await UpdateAsync(asn);
            }
        }

    }
}
