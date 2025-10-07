using WMS.Models;

namespace WMS.Data.Repositories
{
    public interface ILocationRepository : IRepository<Location>
    {
        // Basic location queries
        Task<Location?> GetByCodeAsync(string code);
        Task<IEnumerable<Location>> GetByZoneAsync(string zone);
        Task<IEnumerable<Location>> GetByLocationTypeAsync(string locationType);
        Task<IEnumerable<Location>> GetByStatusAsync(string status);

        // Capacity-based queries
        Task<IEnumerable<Location>> GetAvailableLocationsAsync();
        Task<IEnumerable<Location>> GetLocationsByMinCapacityAsync(int minCapacity);
        Task<Location?> GetBestLocationForItemAsync(int itemId, int requiredCapacity);
        Task<IEnumerable<Location>> GetSuggestedPutawayLocationsAsync(int itemId);

        // Location hierarchy queries
        Task<IEnumerable<Location>> GetByAisleAsync(string aisle);
        Task<IEnumerable<Location>> GetByRackAsync(string aisle, string rack);
        Task<IEnumerable<Location>> GetByLevelAsync(string aisle, string rack, string level);

        // Temperature controlled locations
        Task<IEnumerable<Location>> GetTemperatureControlledLocationsAsync();
        Task<IEnumerable<Location>> GetLocationsByTemperatureRangeAsync(decimal minTemp, decimal maxTemp);

        // Capacity management
        Task<bool> UpdateCapacityAsync(int locationId, int newCurrentCapacity);
        Task<bool> AddCapacityAsync(int locationId, int additionalCapacity);
        Task<bool> RemoveCapacityAsync(int locationId, int capacityToRemove);

        // Location utilization
        Task<IEnumerable<Location>> GetFullLocationsAsync();
        Task<IEnumerable<Location>> GetEmptyLocationsAsync();
        Task<IEnumerable<Location>> GetNearFullLocationsAsync(int threshold = 90);

        // Inventory-related queries
        Task<IEnumerable<Location>> GetLocationsWithInventoryAsync();
        Task<IEnumerable<Location>> GetLocationsByItemAsync(int itemId);
        Task<Location?> GetLocationWithMostAvailableSpaceAsync();

        // Active locations only
        Task<IEnumerable<Location>> GetActiveLocationsAsync();
        Task<IEnumerable<Location>> GetActiveLocationsByCapacityAsync(int minCapacity);

        // Capacity calculation methods
        Task UpdateAllCurrentCapacitiesAsync();
        Task UpdateCurrentCapacityAsync(int locationId);
        Task<bool> CheckCapacityForPutawayAsync(int locationId, int additionalQuantity);
        
        // Advanced Search methods
        Task<IEnumerable<Location>> SearchAsync(LocationSearchRequest request);
        Task<IEnumerable<Location>> QuickSearchAsync(string query);
    }
    
    // Search request model for Location
    public class LocationSearchRequest
    {
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? CapacityFrom { get; set; }
        public int? CapacityTo { get; set; }
        public string? CapacityStatusFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}