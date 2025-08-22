using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    public class SalesOrderRepository : Repository<SalesOrder>, ISalesOrderRepository
    {
        public SalesOrderRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SalesOrder>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(so => so.Customer)
                .Include(so => so.SalesOrderDetails)
                    .ThenInclude(sod => sod.Item)
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();
        }

        public async Task<SalesOrder?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(so => so.Customer)
                .Include(so => so.SalesOrderDetails)
                    .ThenInclude(sod => sod.Item)
                .FirstOrDefaultAsync(so => so.Id == id);
        }

        public async Task<IEnumerable<SalesOrder>> GetByCustomerAsync(int customerId)
        {
            return await _dbSet
                .Include(so => so.Customer)
                .Where(so => so.CustomerId == customerId)
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SalesOrder>> GetByStatusAsync(SalesOrderStatus status)
        {
            return await _dbSet
                .Include(so => so.Customer)
                .Where(so => so.Status == status.ToString())
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SalesOrder>> GetConfirmedSalesOrdersAsync()
        {
            return await _dbSet
                .Include(so => so.Customer)
                .Include(so => so.SalesOrderDetails)
                    .ThenInclude(sod => sod.Item)
                .Where(so => so.Status == SalesOrderStatus.Confirmed.ToString())
                .OrderBy(so => so.RequiredDate)
                .ToListAsync();
        }

        public async Task<bool> ExistsBySONumberAsync(string soNumber)
        {
            return await _dbSet.AnyAsync(so => so.SONumber == soNumber);
        }

        public async Task<string> GenerateNextSONumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"SO-{today:yyyy-MM-dd}-";

            var lastSO = await _dbSet
                .Where(so => so.SONumber.StartsWith(prefix))
                .OrderByDescending(so => so.SONumber)
                .FirstOrDefaultAsync();

            if (lastSO != null)
            {
                var lastNumber = lastSO.SONumber.Substring(prefix.Length);
                if (int.TryParse(lastNumber, out int number))
                {
                    return $"{prefix}{(number + 1):D3}";
                }
            }

            return $"{prefix}001";
        }

        public async Task<SalesOrder> CreateWithDetailsAsync(SalesOrder salesOrder)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generate SO Number if not provided
                if (string.IsNullOrEmpty(salesOrder.SONumber))
                {
                    salesOrder.SONumber = await GenerateNextSONumberAsync();
                }

                // Calculate totals
                salesOrder.TotalAmount = salesOrder.SalesOrderDetails.Sum(d => d.TotalPrice);
                salesOrder.TotalWarehouseFee = salesOrder.SalesOrderDetails.Sum(d => d.WarehouseFeeApplied);

                await _dbSet.AddAsync(salesOrder);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return salesOrder;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateStatusAsync(int id, SalesOrderStatus status)
        {
            var salesOrder = await GetByIdAsync(id);
            if (salesOrder != null)
            {
                salesOrder.Status = status.ToString();
                await UpdateAsync(salesOrder);
            }
        }
        public new async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                    return false;

                _dbSet.Remove(entity);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}