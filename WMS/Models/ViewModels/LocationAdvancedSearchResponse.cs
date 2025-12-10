using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Response model untuk advanced search Location di ASN
    /// </summary>
    public class LocationAdvancedSearchResponse
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
        public List<LocationSearchResult> Data { get; set; } = new();

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
    /// Model untuk hasil search Location
    /// </summary>
    public class LocationSearchResult
    {
        /// <summary>
        /// ID Location
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nama Location
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Kode Location
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Deskripsi Location
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Kapasitas Maksimum
        /// </summary>
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Kapasitas Saat Ini
        /// </summary>
        public int CurrentCapacity { get; set; }

        /// <summary>
        /// Kapasitas Tersedia
        /// </summary>
        public int AvailableCapacity { get; set; }

        /// <summary>
        /// Status Aktif
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Tanggal Created
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
