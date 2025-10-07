using Microsoft.EntityFrameworkCore;
using WMS.Models;
using WMS.Services;
using WMS.Utilities;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Repository implementation untuk Picking operations
    /// </summary>
    public class PickingRepository : Repository<Picking>, IPickingRepository
    {
        public PickingRepository(
            ApplicationDbContext context,
            ICurrentUserService currentUserService,
            ILogger<Repository<Picking>> logger) 
            : base(context, currentUserService, logger)
        {
        }

        // Basic CRUD with details
        public async Task<Picking?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Pickings
                .Include(p => p.SalesOrder)
                    .ThenInclude(so => so.Customer)
                .Include(p => p.SalesOrder)
                    .ThenInclude(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Item)
                .Include(p => p.PickingDetails)
                    .ThenInclude(pd => pd.Item)
                .Include(p => p.PickingDetails)
                    .ThenInclude(pd => pd.Location)
                .Include(p => p.PickingDetails)
                    .ThenInclude(pd => pd.SalesOrderDetail)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Picking?> GetByPickingNumberAsync(string pickingNumber)
        {
            return await _context.Pickings
                .Include(p => p.SalesOrder)
                    .ThenInclude(so => so.Customer)
                .Include(p => p.PickingDetails)
                .FirstOrDefaultAsync(p => p.PickingNumber == pickingNumber);
        }

        public async Task<IEnumerable<Picking>> GetAllWithDetailsAsync()
        {
            return await _context.Pickings
                .Include(p => p.SalesOrder)
                    .ThenInclude(so => so.Customer)
                .Include(p => p.PickingDetails)
                    .ThenInclude(pd => pd.Item)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<Picking> CreateWithDetailsAsync(Picking picking)
        {
            await _context.Pickings.AddAsync(picking);
            await _context.SaveChangesAsync();
            return picking;
        }

        // Query operations
        public async Task<IEnumerable<Picking>> GetBySalesOrderIdAsync(int salesOrderId)
        {
            return await _context.Pickings
                .Include(p => p.SalesOrder)
                .Include(p => p.PickingDetails)
                    .ThenInclude(pd => pd.Item)
                .Where(p => p.SalesOrderId == salesOrderId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Picking>> GetByStatusAsync(string status)
        {
            return await _context.Pickings
                .Include(p => p.SalesOrder)
                    .ThenInclude(so => so.Customer)
                .Include(p => p.PickingDetails)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Picking>> GetPendingPickingsAsync()
        {
            return await GetByStatusAsync(Constants.PICKING_STATUS_PENDING);
        }

        public async Task<IEnumerable<Picking>> GetInProgressPickingsAsync()
        {
            return await GetByStatusAsync(Constants.PICKING_STATUS_IN_PROGRESS);
        }

        public async Task<IEnumerable<Picking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Pickings
                .Include(p => p.SalesOrder)
                    .ThenInclude(so => so.Customer)
                .Include(p => p.PickingDetails)
                .Where(p => p.PickingDate >= startDate && p.PickingDate <= endDate)
                .OrderByDescending(p => p.PickingDate)
                .ToListAsync();
        }

        // Status operations
        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var picking = await GetByIdAsync(id);
            if (picking == null)
                return false;

            picking.Status = status;
            
            if (status == Constants.PICKING_STATUS_COMPLETED)
            {
                picking.CompletedDate = DateTime.Now;
            }

            picking.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompletePickingAsync(int id)
        {
            return await UpdateStatusAsync(id, Constants.PICKING_STATUS_COMPLETED);
        }

        public async Task<bool> CancelPickingAsync(int id)
        {
            return await UpdateStatusAsync(id, Constants.PICKING_STATUS_CANCELLED);
        }

        // Business logic helpers
        public async Task<bool> ExistsForSalesOrderAsync(int salesOrderId)
        {
            return await _context.Pickings
                .AnyAsync(p => p.SalesOrderId == salesOrderId 
                    && p.Status != Constants.PICKING_STATUS_CANCELLED);
        }

        public async Task<string> GeneratePickingNumberAsync()
        {
            var today = DateTime.Today;
            var count = await GetPickingCountByDateAsync(today);
            var sequence = (count + 1).ToString("D3");
            return $"PKG-{today:yyyy-MM-dd}-{sequence}";
        }

        public async Task<int> GetPickingCountByDateAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            return await _context.Pickings
                .Where(p => p.PickingDate >= startOfDay && p.PickingDate <= endOfDay)
                .CountAsync();
        }
    }
}
