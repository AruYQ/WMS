using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Request model untuk advanced search Location di ASN
    /// </summary>
    public class LocationAdvancedSearchRequest
    {
        /// <summary>
        /// Nama Lokasi (partial match)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Kode Lokasi (partial match)
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Tanggal Dibuat dari
        /// </summary>
        public DateTime? CreatedDateFrom { get; set; }

        /// <summary>
        /// Tanggal Dibuat sampai
        /// </summary>
        public DateTime? CreatedDateTo { get; set; }

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
