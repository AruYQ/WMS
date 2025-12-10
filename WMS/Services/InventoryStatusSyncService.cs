using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WMS.Data;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Background service untuk auto-sync inventory status berdasarkan quantity
    /// Menjalankan check setiap 5 menit
    /// </summary>
    public class InventoryStatusSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InventoryStatusSyncService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check setiap 5 menit

        public InventoryStatusSyncService(
            IServiceProvider serviceProvider,
            ILogger<InventoryStatusSyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("InventoryStatusSyncService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncInventoryStatusAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in InventoryStatusSyncService");
                    // Wait 1 minute before retry on error
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("InventoryStatusSyncService stopped");
        }

        private async Task SyncInventoryStatusAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Find all inventories yang quantity > 0 tapi status != Available
                var inventoriesToFix = await context.Inventories
                    .Where(i => !i.IsDeleted && 
                               i.Quantity > 0 && 
                               i.Status != Constants.INVENTORY_STATUS_AVAILABLE)
                    .ToListAsync();

                if (inventoriesToFix.Count > 0)
                {
                    _logger.LogInformation($"Found {inventoriesToFix.Count} inventories to fix status to Available");

                    foreach (var inventory in inventoriesToFix)
                    {
                        inventory.Status = Constants.INVENTORY_STATUS_AVAILABLE;
                        inventory.ModifiedDate = DateTime.Now;
                        inventory.LastUpdated = DateTime.Now;
                    }

                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Fixed {inventoriesToFix.Count} inventory statuses to Available");
                }

                // Find all inventories yang quantity = 0 tapi status != Empty
                var emptyInventoriesToFix = await context.Inventories
                    .Where(i => !i.IsDeleted && 
                               i.Quantity == 0 && 
                               i.Status != Constants.INVENTORY_STATUS_EMPTY)
                    .ToListAsync();

                if (emptyInventoriesToFix.Count > 0)
                {
                    _logger.LogInformation($"Found {emptyInventoriesToFix.Count} empty inventories to fix status to Empty");

                    foreach (var inventory in emptyInventoriesToFix)
                    {
                        inventory.Status = Constants.INVENTORY_STATUS_EMPTY;
                        inventory.ModifiedDate = DateTime.Now;
                        inventory.LastUpdated = DateTime.Now;
                    }

                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Fixed {emptyInventoriesToFix.Count} inventory statuses to Empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing inventory status");
                throw;
            }
        }
    }
}

