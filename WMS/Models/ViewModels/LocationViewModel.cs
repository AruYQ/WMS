using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
#if false // Temporarily disabled: legacy MVC view models not used in current API-first flow. Keep until Razor screens revived.
    /// <summary>
    /// ViewModel untuk Location management
    /// </summary>
    public class LocationViewModel
    {
        public int Id { get; set; }

        /// <summary>
        /// Kode lokasi (contoh: A-01-01, B-02-03)
        /// </summary>
        [Required(ErrorMessage = "Kode lokasi wajib diisi")]
        [MaxLength(20, ErrorMessage = "Kode lokasi maksimal 20 karakter")]
        [Display(Name = "Kode Lokasi")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Nama deskriptif lokasi
        /// </summary>
        [Required(ErrorMessage = "Nama lokasi wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama lokasi maksimal 100 karakter")]
        [Display(Name = "Nama Lokasi")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Deskripsi tambahan lokasi
        /// </summary>
        [MaxLength(200, ErrorMessage = "Deskripsi maksimal 200 karakter")]
        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

        /// <summary>
        /// Kapasitas maksimum lokasi
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Kapasitas maksimum harus lebih besar dari 0")]
        [Display(Name = "Kapasitas Maksimum")]
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Kapasitas yang sedang terpakai
        /// </summary>
        [Display(Name = "Kapasitas Terpakai")]
        public int CurrentCapacity { get; set; } = 0;

        /// <summary>
        /// Sisa kapasitas yang tersedia
        /// </summary>
        [Display(Name = "Kapasitas Tersedia")]
        public int AvailableCapacity { get; set; } = 0;

        /// <summary>
        /// Status apakah lokasi sudah penuh
        /// </summary>
        [Display(Name = "Status Penuh")]
        public bool IsFull { get; set; } = false;

        /// <summary>
        /// Status aktif lokasi
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Persentase kapasitas terpakai
        /// </summary>
        [Display(Name = "Persentase Kapasitas")]
        public double CapacityPercentage { get; set; } = 0;

        /// <summary>
        /// Status kapasitas dalam bentuk text
        /// </summary>
        [Display(Name = "Status Kapasitas")]
        public string CapacityStatus { get; set; } = "TERSEDIA";

        /// <summary>
        /// CSS class untuk styling berdasarkan status kapasitas
        /// </summary>
        public string CapacityStatusCssClass { get; set; } = "badge bg-success";

        /// <summary>
        /// Display name untuk dropdown
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Tanggal dibuat
        /// </summary>
        [Display(Name = "Tanggal Dibuat")]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Tanggal diubah
        /// </summary>
        [Display(Name = "Tanggal Diubah")]
        public DateTime? ModifiedDate { get; set; }
    }

    /// <summary>
    /// ViewModel untuk Location Grid display
    /// </summary>
    public class LocationGridViewModel
    {
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int CurrentCapacity { get; set; }
        public int AvailableCapacity { get; set; }
        public double CapacityPercentage { get; set; }
        public bool IsFull { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; }
    }
#endif
}
