using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk advanced search Purchase Order di ASN
    /// </summary>
    public class PurchaseOrderAdvancedSearchRequest
    {
        /// <summary>
        /// Nomor Purchase Order (partial match)
        /// </summary>
        public string? PONumber { get; set; }

        /// <summary>
        /// Nama Supplier (partial match)
        /// </summary>
        public string? SupplierName { get; set; }

        /// <summary>
        /// Tanggal Order dari
        /// </summary>
        public DateTime? OrderDateFrom { get; set; }

        /// <summary>
        /// Tanggal Order sampai
        /// </summary>
        public DateTime? OrderDateTo { get; set; }

        /// <summary>
        /// Halaman saat ini
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Ukuran halaman
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }
}
