// Models/Location.cs
// Model untuk master data lokasi penyimpanan di gudang

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk data lokasi penyimpanan di gudang
    /// Digunakan untuk item tracking dan putaway process
    /// </summary>
    public class Location : BaseEntity
    {
        /// <summary>
        /// Kode lokasi (contoh: A-01-01, B-02-03)
        /// Format: [Area]-[Rak]-[Slot]
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
        /// Kapasitas maksimum lokasi (dalam unit/pieces)
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Kapasitas maksimum harus lebih besar atau sama dengan 0")]
        [Display(Name = "Kapasitas Maksimum")]
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Kapasitas yang sedang terpakai
        /// Dihitung otomatis dari inventory yang ada di lokasi ini
        /// </summary>
        [Display(Name = "Kapasitas Terpakai")]
        public int CurrentCapacity { get; set; } = 0;

        /// <summary>
        /// Status apakah lokasi sudah penuh
        /// Dihitung otomatis: CurrentCapacity >= MaxCapacity
        /// </summary>
        [Display(Name = "Status Penuh")]
        public bool IsFull { get; set; } = false;

        /// <summary>
        /// Status aktif lokasi
        /// </summary>
        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        /// <summary>
        /// Daftar inventory yang disimpan di lokasi ini
        /// </summary>
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

        // Computed Properties
        /// <summary>
        /// Persentase kapasitas terpakai
        /// </summary>
        [NotMapped]
        public double CapacityPercentage
        {
            get
            {
                if (MaxCapacity == 0) return 0;
                return (double)CurrentCapacity / MaxCapacity * 100;
            }
        }

        /// <summary>
        /// Sisa kapasitas yang tersedia
        /// </summary>
        [NotMapped]
        public int AvailableCapacity => MaxCapacity - CurrentCapacity;

        /// <summary>
        /// Status kapasitas dalam bentuk text
        /// </summary>
        [NotMapped]
        public string CapacityStatus
        {
            get
            {
                if (IsFull) return "PENUH";
                if (CapacityPercentage >= 80) return "HAMPIR PENUH";
                if (CapacityPercentage >= 50) return "SETENGAH";
                return "TERSEDIA";
            }
        }

        /// <summary>
        /// CSS class untuk styling berdasarkan status kapasitas
        /// </summary>
        [NotMapped]
        public string CapacityStatusCssClass
        {
            get
            {
                if (IsFull) return "badge bg-danger";
                if (CapacityPercentage >= 80) return "badge bg-warning";
                if (CapacityPercentage >= 50) return "badge bg-info";
                return "badge bg-success";
            }
        }

        /// <summary>
        /// Display name untuk dropdown
        /// </summary>
        [NotMapped]
        public string DisplayName => $"{Code} - {Name} ({AvailableCapacity} tersedia)";

        /// <summary>
        /// Display text untuk dropdown dengan status kapasitas
        /// </summary>
        [NotMapped]
        public string DropdownDisplayText => $"{Code} - {Name} ({AvailableCapacity}/{MaxCapacity})";

        /// <summary>
        /// CSS class untuk dropdown option berdasarkan status kapasitas
        /// </summary>
        [NotMapped]
        public string DropdownCssClass
        {
            get
            {
                if (IsFull) return "text-danger";
                if (AvailableCapacity <= 5) return "text-danger fw-bold";
                if (AvailableCapacity <= 20) return "text-warning fw-bold";
                return "text-success";
            }
        }

        /// <summary>
        /// Status text untuk dropdown
        /// </summary>
        [NotMapped]
        public string DropdownStatusText
        {
            get
            {
                if (IsFull) return "PENUH";
                if (AvailableCapacity <= 5) return "KRITIS";
                if (AvailableCapacity <= 20) return "HAMPIR PENUH";
                return "TERSEDIA";
            }
        }

        /// <summary>
        /// Check if location can accommodate the required quantity
        /// </summary>
        public bool CanAccommodate(int quantity)
        {
            return AvailableCapacity >= quantity;
        }

        /// <summary>
        /// Get capacity status for dropdown
        /// </summary>
        [NotMapped]
        public string CapacityStatusForDropdown
        {
            get
            {
                if (IsFull) return "PENUH";
                if (AvailableCapacity <= 5) return "KRITIS (≤5)";
                if (AvailableCapacity <= 20) return "HAMPIR PENUH (≤20)";
                return $"TERSEDIA ({AvailableCapacity})";
            }
        }
    }
}