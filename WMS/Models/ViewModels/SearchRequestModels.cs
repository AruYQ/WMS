using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Centralized search request models for advanced search functionality
    /// </summary>

    public class SupplierSearchRequest
    {
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? CityFilter { get; set; }
        public string? ContactPersonFilter { get; set; }
        public string? SupplierNameFilter { get; set; }
        public string? PhoneFilter { get; set; }
        public string? SupplierCodeFilter { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [Range(1, 1000)]
        public int PageSize { get; set; } = 50;
    }

    public class ItemSearchRequest
    {
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? SupplierFilter { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [Range(1, 1000)]
        public int PageSize { get; set; } = 50;
    }

    public class LocationSearchRequest
    {
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? CapacityFrom { get; set; }
        public int? CapacityTo { get; set; }
        public string? CapacityStatusFilter { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [Range(1, 1000)]
        public int PageSize { get; set; } = 50;
    }

    public class CustomerSearchRequest
    {
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? CityFilter { get; set; }
        public string? CustomerTypeFilter { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [Range(1, 1000)]
        public int PageSize { get; set; } = 50;
    }

    public class PurchaseOrderSearchRequest
    {
        public string? SearchText { get; set; }
        public string? SupplierNameFilter { get; set; }
        public string? PhoneFilter { get; set; }
        public string? PONumberFilter { get; set; }
        public string? POStatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [Range(1, 1000)]
        public int PageSize { get; set; } = 50;
    }

    public class SalesOrderSearchRequest
    {
        public string? SearchText { get; set; }
        public string? CustomerNameFilter { get; set; }
        public string? SONumberFilter { get; set; }
        public string? SOStatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [Range(1, 1000)]
        public int PageSize { get; set; } = 50;
    }

    public class ASNSearchRequest
    {
        public string? SearchText { get; set; }
        public string? ASNNumberFilter { get; set; }
        public string? SupplierNameFilter { get; set; }
        public string? PONumberFilter { get; set; }
        public string? ASNStatusFilter { get; set; }
        public DateTime? ShipmentDateFrom { get; set; }
        public DateTime? ShipmentDateTo { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [Range(1, 1000)]
        public int PageSize { get; set; } = 50;
    }
}


