// Models/Inventory.cs
// Model untuk inventory management - item tracking dan putaway

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk inventory management
    /// "The Stage Design" - mengatur lokasi dan tracking item
    /// Menyimpan informasi stok per item per lokasi
    /// </summary>
    public class Inventory : BaseEntity
    {
        /// <summary>
        /// ID Item yang disimpan
        /// </summary>
        [Required(ErrorMessage = "Item wajib dipilih")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }

        /// <summary>
        /// ID Lokasi tempat item disimpan
        /// </summary>
        [Required(ErrorMessage = "Lokasi wajib dipilih")]
        [Display(Name = "Lokasi")]
        public int LocationId { get; set; }

        /// <summary>
        /// Jumlah stok yang tersedia di lokasi ini
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Quantity tidak boleh negatif")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 0;

        /// <summary>
        /// Harga cost terakhir (dari ASN terakhir)
        /// Digunakan untuk valuasi inventory
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Last Cost Price")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal LastCostPrice { get; set; } = 0;

        /// <summary>
        /// Tanggal terakhir inventory diupdate
        /// </summary>
        [Required]
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// Status inventory (Available, Reserved, Damaged, etc.)
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Available";

        /// <summary>
        /// Catatan untuk inventory ini
        /// </summary>
        [MaxLength(200)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }

        // Navigation Properties
        /// <summary>
        /// Item yang disimpan
        /// </summary>
        public virtual Item Item { get; set; } = null!;

        /// <summary>
        /// Lokasi tempat item disimpan
        /// </summary>
        public virtual Location Location { get; set; } = null!;

        // Computed Properties
        /// <summary>
        /// Display text untuk item (kode + nama)
        /// </summary>
        [NotMapped]
        public string ItemDisplay => Item?.DisplayName ?? string.Empty;

        /// <summary>
        /// Display text untuk lokasi (kode + nama)
        /// </summary>
        [NotMapped]
        public string LocationDisplay => Location?.DisplayName ?? string.Empty;

        /// <summary>
        /// Unit satuan dari item
        /// </summary>
        [NotMapped]
        public string ItemUnit => Item?.Unit ?? string.Empty;

        /// <summary>
        /// Total nilai inventory (Quantity × LastCostPrice)
        /// </summary>
        [NotMapped]
        public decimal TotalValue => Quantity * LastCostPrice;

        /// <summary>
        /// Status dalam bahasa Indonesia
        /// </summary>
        [NotMapped]
        public string StatusIndonesia
        {
            get
            {
                return Status switch
                {
                    "Available" => "Tersedia",
                    "Reserved" => "Dipesan",
                    "Damaged" => "Rusak",
                    "Quarantine" => "Karantina",
                    "Blocked" => "Diblokir",
                    _ => Status
                };
            }
        }

        /// <summary>
        /// CSS class untuk styling status
        /// </summary>
        [NotMapped]
        public string StatusCssClass
        {
            get
            {
                return Status switch
                {
                    "Available" => "badge bg-success",
                    "Reserved" => "badge bg-warning",
                    "Damaged" => "badge bg-danger",
                    "Quarantine" => "badge bg-secondary",
                    "Blocked" => "badge bg-dark",
                    _ => "badge bg-light"
                };
            }
        }

        /// <summary>
        /// CSS class untuk styling quantity berdasarkan level stok
        /// </summary>
        [NotMapped]
        public string QuantityCssClass
        {
            get
            {
                if (Quantity == 0) return "text-danger fw-bold";
                if (Quantity <= 10) return "text-warning fw-bold";
                return "text-success";
            }
        }

        /// <summary>
        /// Status level stok
        /// </summary>
        [NotMapped]
        public string StockLevel
        {
            get
            {
                if (Quantity == 0) return "KOSONG";
                if (Quantity <= 10) return "RENDAH";
                if (Quantity <= 50) return "SEDANG";
                return "TINGGI";
            }
        }

        /// <summary>
        /// Apakah stok tersedia untuk dijual
        /// </summary>
        [NotMapped]
        public bool IsAvailableForSale => Status == "Available" && Quantity > 0;

        /// <summary>
        /// Ringkasan inventory untuk tampilan
        /// Format: "ItemCode - ItemName @ LocationCode (Qty Unit)"
        /// </summary>
        [NotMapped]
        public string Summary => $"{Item?.ItemCode} - {Item?.Name} @ {Location?.Code} ({Quantity} {ItemUnit})";

        /// <summary>
        /// Informasi detail untuk tooltip atau modal
        /// </summary>
        [NotMapped]
        public string DetailInfo => $"Item: {ItemDisplay}\nLokasi: {LocationDisplay}\nStok: {Quantity} {ItemUnit}\nStatus: {StatusIndonesia}\nNilai: {TotalValue:C}\nUpdate: {LastUpdated:dd/MM/yyyy HH:mm}";

        // Methods
        /// <summary>
        /// Menambah stok (saat putaway dari ASN)
        /// </summary>
        /// <param name="quantity">Jumlah yang ditambah</param>
        /// <param name="costPrice">Harga cost dari ASN</param>
        public void AddStock(int quantity, decimal costPrice)
        {
            Quantity += quantity;
            LastCostPrice = costPrice; // Update dengan cost terbaru
            LastUpdated = DateTime.Now;
            Status = "Available";
        }

        /// <summary>
        /// Mengurangi stok (saat sales order)
        /// </summary>
        /// <param name="quantity">Jumlah yang dikurangi</param>
        /// <returns>True jika berhasil, False jika stok tidak cukup</returns>
        public bool ReduceStock(int quantity)
        {
            if (quantity > Quantity || Status != "Available")
            {
                return false;
            }

            Quantity -= quantity;
            LastUpdated = DateTime.Now;

            if (Quantity == 0)
            {
                Status = "Empty";
            }

            return true;
        }

        /// <summary>
        /// Reserve stok untuk sales order
        /// </summary>
        /// <param name="quantity">Jumlah yang direserve</param>
        /// <returns>True jika berhasil</returns>
        public bool ReserveStock(int quantity)
        {
            if (quantity > Quantity || Status != "Available")
            {
                return false;
            }

            // Implementasi reserve bisa dengan mengurangi available quantity
            // atau membuat table terpisah untuk reserved stock
            // Untuk saat ini kita kurangi langsung
            return ReduceStock(quantity);
        }

        /// <summary>
        /// Update status inventory
        /// </summary>
        /// <param name="newStatus">Status baru</param>
        /// <param name="notes">Catatan perubahan</param>
        public void UpdateStatus(string newStatus, string? notes = null)
        {
            Status = newStatus;
            if (!string.IsNullOrEmpty(notes))
            {
                Notes = notes;
            }
            LastUpdated = DateTime.Now;
        }
    }
}