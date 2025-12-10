using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk create Picking
    /// </summary>
    public class CreatePickingRequest
    {
        /// <summary>
        /// ID Sales Order yang akan dipick
        /// </summary>
        [Required(ErrorMessage = "Sales Order wajib dipilih")]
        public int SalesOrderId { get; set; }

        /// <summary>
        /// ID Holding Location untuk menyimpan barang sebelum shipment
        /// </summary>
        [Required(ErrorMessage = "Holding Location wajib dipilih")]
        public int HoldingLocationId { get; set; }

        /// <summary>
        /// Catatan untuk picking
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Detail items yang akan dipick dengan source location
        /// </summary>
        public List<PickingItemRequest> PickingItems { get; set; } = new List<PickingItemRequest>();
    }

    /// <summary>
    /// Request model untuk item dalam picking
    /// </summary>
    public class PickingItemRequest
    {
        /// <summary>
        /// ID Item yang akan dipick
        /// </summary>
        [Required]
        public int ItemId { get; set; }

        /// <summary>
        /// ID Source Location (tempat item akan dipick)
        /// </summary>
        [Required]
        public int SourceLocationId { get; set; }
    }

    /// <summary>
    /// Request model untuk set location pada picking detail
    /// </summary>
    public class SetLocationRequest
    {
        /// <summary>
        /// ID Source Location yang akan diset
        /// </summary>
        [Required(ErrorMessage = "Source Location ID wajib dipilih")]
        public int SourceLocationId { get; set; }
    }

    /// <summary>
    /// Response model untuk Sales Order items dengan available locations
    /// </summary>
    public class SalesOrderItemWithLocationsResponse
    {
        /// <summary>
        /// ID Sales Order Detail
        /// </summary>
        public int SalesOrderDetailId { get; set; }

        /// <summary>
        /// ID Item
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Kode Item
        /// </summary>
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>
        /// Nama Item
        /// </summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// Unit satuan
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Quantity yang dibutuhkan
        /// </summary>
        public int QuantityRequired { get; set; }

        /// <summary>
        /// Lokasi-lokasi yang tersedia untuk item ini
        /// </summary>
        public List<AvailableLocationInfo> AvailableLocations { get; set; } = new List<AvailableLocationInfo>();
    }

    /// <summary>
    /// Info lokasi yang tersedia untuk picking
    /// </summary>
    public class AvailableLocationInfo
    {
        /// <summary>
        /// ID Location
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Kode Location
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Nama Location
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Quantity yang tersedia di location ini
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Max capacity location
        /// </summary>
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Current capacity location
        /// </summary>
        public int CurrentCapacity { get; set; }

        /// <summary>
        /// Available capacity
        /// </summary>
        public int AvailableCapacity => MaxCapacity - CurrentCapacity;
    }

    /// <summary>
    /// Response model untuk Sales Order list
    /// </summary>
    public class SalesOrderForPickingResponse
    {
        /// <summary>
        /// ID Sales Order
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nomor Sales Order
        /// </summary>
        public string SONumber { get; set; } = string.Empty;

        /// <summary>
        /// Nama Customer
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Email Customer
        /// </summary>
        public string CustomerEmail { get; set; } = string.Empty;

        /// <summary>
        /// Tanggal Order
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Status Sales Order
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Total items dalam SO
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Total quantity dalam SO
        /// </summary>
        public int TotalQuantity { get; set; }
    }

    /// <summary>
    /// Response model untuk Holding Location list
    /// </summary>
    public class HoldingLocationResponse
    {
        /// <summary>
        /// ID Location
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Kode Location
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Nama Location
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Deskripsi Location
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Max capacity
        /// </summary>
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Current capacity
        /// </summary>
        public int CurrentCapacity { get; set; }

        /// <summary>
        /// Available capacity
        /// </summary>
        public int AvailableCapacity => MaxCapacity - CurrentCapacity;

        /// <summary>
        /// Status aktif
        /// </summary>
        public bool IsActive { get; set; }
    }
}
