using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    public class CompanyViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama perusahaan wajib diisi")]
        [MaxLength(100, ErrorMessage = "Nama perusahaan maksimal 100 karakter")]
        [Display(Name = "Nama Perusahaan")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kode perusahaan wajib diisi")]
        [MaxLength(20, ErrorMessage = "Kode perusahaan maksimal 20 karakter")]
        [Display(Name = "Kode Perusahaan")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email perusahaan wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
        [Display(Name = "Nomor Telepon")]
        public string? Phone { get; set; }

        [MaxLength(300, ErrorMessage = "Alamat maksimal 300 karakter")]
        [Display(Name = "Alamat")]
        public string? Address { get; set; }

        [MaxLength(100, ErrorMessage = "Nama kontak maksimal 100 karakter")]
        [Display(Name = "Kontak Person")]
        public string? ContactPerson { get; set; }

        [MaxLength(20, ErrorMessage = "NPWP maksimal 20 karakter")]
        [Display(Name = "NPWP")]
        public string? TaxNumber { get; set; }

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Plan Subscription")]
        public string SubscriptionPlan { get; set; } = "Free";

        [Display(Name = "Maksimal User")]
        public int MaxUsers { get; set; } = 5;

        [Display(Name = "Tanggal Berakhir")]
        public DateTime? SubscriptionEndDate { get; set; }

        // Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}