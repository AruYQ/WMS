using WMS.Models;

namespace WMS.Data.Repositories
{
    public interface ILocationRepository : IRepository<Location>
    {
        Task<IEnumerable<Location>> GetAllWithInventoryAsync();
        Task<Location?> GetByIdWithInventoryAsync(int id);
        Task<Location?> GetByCodeAsync(string code);
        Task<IEnumerable<Location>> GetActiveLocationsAsync();
        Task<IEnumerable<Location>> GetAvailableLocationsAsync();
        Task<bool> ExistsByCodeAsync(string code);
        Task<IEnumerable<Location>> SearchLocationsAsync(string searchTerm);
        Task UpdateCapacityAsync(int locationId);
        Task<Dictionary<string, object>> GetLocationStatisticsAsync();
    }
}