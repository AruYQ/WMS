// Base class untuk semua entity - berisi field yang umum dipakai semua tabel

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace WMS.Models
{
    /// <summary>
    /// Base entity yang berisi field umum untuk audit trail dan multi-tenancy
    /// Semua model lain akan inherit dari class ini
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Primary key untuk semua tabel
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Company ID untuk multi-tenancy - semua data terikat ke company
        /// Nullable untuk SuperAdmin yang tidak terikat ke company tertentu
        /// </summary>
        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        /// <summary>
        /// Tanggal record dibuat
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Tanggal terakhir record dimodifikasi
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// User yang membuat record ini
        /// Bisa diisi dengan username atau user ID
        /// </summary>
        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User yang terakhir memodifikasi record ini
        /// </summary>
        [MaxLength(50)]
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Status soft delete - true jika record sudah dihapus
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Tanggal record dihapus (soft delete)
        /// </summary>
        public DateTime? DeletedDate { get; set; }

        /// <summary>
        /// User yang menghapus record ini (soft delete)
        /// </summary>
        [MaxLength(50)]
        public string? DeletedBy { get; set; }

        // Navigation Property
        /// <summary>
        /// Reference ke Company entity
        /// </summary>
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }
    }

    /// <summary>
    /// Base entity untuk entities yang tidak memerlukan CompanyId (seperti Company itu sendiri)
    /// </summary>
    public abstract class BaseEntityWithoutCompany
    {
        /// <summary>
        /// Primary key untuk semua tabel
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tanggal record dibuat
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Tanggal terakhir record dimodifikasi
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// User yang membuat record ini
        /// </summary>
        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User yang terakhir memodifikasi record ini
        /// </summary>
        [MaxLength(50)]
        public string? ModifiedBy { get; set; }
    }
}