using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Response model untuk advanced search Purchase Order di ASN
    /// </summary>
    public class PurchaseOrderAdvancedSearchResponse
    {
        /// <summary>
        /// Status keberhasilan
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Pesan response
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Data hasil search
        /// </summary>
        public List<PurchaseOrderSearchResult> Data { get; set; } = new();

        /// <summary>
        /// Total jumlah data
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total halaman
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Halaman saat ini
        /// </summary>
        public int CurrentPage { get; set; }
    }

    /// <summary>
    /// Model untuk hasil search Purchase Order
    /// </summary>
    public class PurchaseOrderSearchResult
    {
        /// <summary>
        /// ID Purchase Order
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nomor Purchase Order
        /// </summary>
        public string PONumber { get; set; } = string.Empty;

        /// <summary>
        /// Nama Supplier
        /// </summary>
        public string SupplierName { get; set; } = string.Empty;

        /// <summary>
        /// Email Supplier
        /// </summary>
        public string SupplierEmail { get; set; } = string.Empty;

        /// <summary>
        /// Tanggal Order
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Tanggal Expected Delivery
        /// </summary>
        public DateTime ExpectedDeliveryDate { get; set; }

        /// <summary>
        /// Status Purchase Order
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Total Amount
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Jumlah Item
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Tanggal Created
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
