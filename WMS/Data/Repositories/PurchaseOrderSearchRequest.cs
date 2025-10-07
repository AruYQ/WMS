using System.ComponentModel.DataAnnotations;

namespace WMS.Data.Repositories
{
    /// <summary>
    /// Request model untuk advanced search Purchase Order
    /// </summary>
    public class PurchaseOrderSearchRequest
    {
        /// <summary>
        /// Text search untuk PONumber atau Supplier Name
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// Filter berdasarkan nama supplier
        /// </summary>
        public string? SupplierNameFilter { get; set; }

        /// <summary>
        /// Filter berdasarkan phone supplier
        /// </summary>
        public string? PhoneFilter { get; set; }

        /// <summary>
        /// Filter berdasarkan PO Number
        /// </summary>
        public string? PONumberFilter { get; set; }

        /// <summary>
        /// Filter berdasarkan status PO
        /// </summary>
        public string? POStatusFilter { get; set; }

        /// <summary>
        /// Filter berdasarkan tanggal order dari
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Filter berdasarkan tanggal order sampai
        /// </summary>
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Halaman yang diminta (untuk pagination)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Jumlah item per halaman (untuk pagination)
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Validasi custom untuk request
        /// </summary>
        public bool IsValid()
        {
            // Validasi tanggal
            if (DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Mendapatkan daftar error validasi
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (DateFrom.HasValue && DateTo.HasValue && DateFrom > DateTo)
            {
                errors.Add("DateFrom cannot be greater than DateTo");
            }

            return errors;
        }
    }
}
