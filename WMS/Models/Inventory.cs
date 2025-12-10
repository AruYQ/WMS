using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Utilities;

namespace WMS.Models
{
    /// <summary>
    /// Model untuk Inventory - stock item di lokasi tertentu
    /// "The Stage Design" - setiap item punya posisi di panggung warehouse
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
        public string Status { get; set; } = Constants.INVENTORY_STATUS_AVAILABLE;

        /// <summary>
        /// Catatan untuk inventory ini
        /// </summary>
        [MaxLength(200)]
        [Display(Name = "Catatan")]
        public string? Notes { get; set; }

        /// <summary>
        /// Source reference untuk tracking (misalnya dari ASN mana)
        /// </summary>
        [MaxLength(100)]
        [Display(Name = "Source Reference")]
        public string? SourceReference { get; set; } // Format: "ASNDetail:123"

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
        public string ItemDisplay => Item?.DisplayName ?? $"Item {ItemId}";

        /// <summary>
        /// Display text untuk lokasi (kode + nama)
        /// </summary>
        [NotMapped]
        public string LocationDisplay => Location?.DisplayName ?? $"Location {LocationId}";

        /// <summary>
        /// Unit satuan dari item
        /// </summary>
        [NotMapped]
        public string ItemUnit => Item?.Unit ?? "PCS";

        /// <summary>
        /// Total nilai inventory (Quantity × LastCostPrice)
        /// </summary>
        [NotMapped]
        public decimal TotalValue => Quantity * LastCostPrice;

        /// <summary>
        /// Summary untuk display (digunakan di view)
        /// </summary>
        [NotMapped]
        public string Summary => $"{Item?.ItemCode ?? "Unknown"} - {Item?.Name ?? "Unknown"} @ {Location?.Code ?? "Unknown"} ({Quantity} {ItemUnit})";

        // CSS Helper Properties untuk View
        [NotMapped]
        public string StatusCssClass
        {
            get
            {
                return Status switch
                {
                    Constants.INVENTORY_STATUS_AVAILABLE => "badge bg-success",
                    Constants.INVENTORY_STATUS_RESERVED => "badge bg-warning",
                    Constants.INVENTORY_STATUS_DAMAGED => "badge bg-danger",
                    Constants.INVENTORY_STATUS_QUARANTINE => "badge bg-secondary",
                    Constants.INVENTORY_STATUS_BLOCKED => "badge bg-dark",
                    Constants.INVENTORY_STATUS_EMPTY => "badge bg-light text-dark",
                    _ => "badge bg-light text-dark"
                };
            }
        }

        [NotMapped]
        public string StatusIndonesia
        {
            get
            {
                return Status switch
                {
                    Constants.INVENTORY_STATUS_AVAILABLE => "Tersedia",
                    Constants.INVENTORY_STATUS_RESERVED => "Dipesan",
                    Constants.INVENTORY_STATUS_DAMAGED => "Rusak",
                    Constants.INVENTORY_STATUS_QUARANTINE => "Karantina",
                    Constants.INVENTORY_STATUS_BLOCKED => "Diblokir",
                    Constants.INVENTORY_STATUS_EMPTY => "Kosong",
                    _ => Status
                };
            }
        }

        [NotMapped]
        public string QuantityCssClass
        {
            get
            {
                if (Quantity == 0) return "text-danger fw-bold";
                if (Quantity <= Constants.LOW_STOCK_THRESHOLD) return "text-warning fw-bold";
                return "text-success";
            }
        }

        [NotMapped]
        public string StockLevel
        {
            get
            {
                if (Quantity == 0) return "KOSONG";
                if (Quantity <= Constants.CRITICAL_STOCK_THRESHOLD) return "KRITIS";
                if (Quantity <= Constants.LOW_STOCK_THRESHOLD) return "RENDAH";
                if (Quantity <= 50) return "SEDANG";
                return "TINGGI";
            }
        }

        // Business Logic Methods
        /// <summary>
        /// Menambah stok dengan weighted average cost
        /// </summary>
        public void AddStock(int quantity, decimal costPrice)
        {
            if (quantity <= 0) return;

            // Calculate weighted average cost
            var totalValue = (Quantity * LastCostPrice) + (quantity * costPrice);
            var totalQuantity = Quantity + quantity;

            Quantity = totalQuantity;
            LastCostPrice = totalQuantity > 0 ? totalValue / totalQuantity : costPrice;
            LastUpdated = DateTime.Now;
            ModifiedDate = DateTime.Now;
            
            // ✅ FIX: Update status to AVAILABLE when quantity > 0
            if (Quantity > 0 && Status == Constants.INVENTORY_STATUS_EMPTY)
            {
                Status = Constants.INVENTORY_STATUS_AVAILABLE;
            }
        }

        /// <summary>
        /// Mengurangi stok (untuk picking/sales)
        /// </summary>
        public bool ReduceStock(int quantity)
        {
            if (quantity <= 0 || quantity > Quantity)
                return false;

            Quantity -= quantity;
            LastUpdated = DateTime.Now;
            ModifiedDate = DateTime.Now;

            // If quantity becomes 0, update status
            if (Quantity == 0)
            {
                Status = Constants.INVENTORY_STATUS_EMPTY;
            }

            return true;
        }

        /// <summary>
        /// Update status inventory
        /// </summary>
        public void UpdateStatus(string newStatus, string? notes = null)
        {
            Status = newStatus;
            if (!string.IsNullOrEmpty(notes))
            {
                Notes = string.IsNullOrEmpty(Notes) ? notes : $"{Notes}; {notes}";
            }
            LastUpdated = DateTime.Now;
            ModifiedDate = DateTime.Now;
        }

        /// <summary>
        /// Set source reference untuk tracking
        /// </summary>
        public void SetSourceReference(string source, int referenceId)
        {
            SourceReference = $"{source}:{referenceId}";
        }

        /// <summary>
        /// Check apakah inventory ini berasal dari ASN Detail tertentu
        /// </summary>
        public bool IsFromASNDetail(int asnDetailId)
        {
            return SourceReference == $"ASNDetail:{asnDetailId}";
        }

        /// <summary>
        /// Validate business rules
        /// </summary>
        public bool IsValid()
        {
            return ItemId > 0 && LocationId > 0 && Quantity >= 0 && LastCostPrice >= 0;
        }

        /// <summary>
        /// Check apakah bisa dijual
        /// </summary>
        public bool IsAvailableForSale => Status == Constants.INVENTORY_STATUS_AVAILABLE && Quantity > 0;

        /// <summary>
        /// Check apakah perlu reorder
        /// </summary>
        public bool NeedsReorder => Quantity <= Constants.LOW_STOCK_THRESHOLD;
    }
}