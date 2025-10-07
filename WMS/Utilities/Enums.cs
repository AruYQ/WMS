
// Enumerasi untuk status dan pilihan terbatas

namespace WMS.Utilities
{
    /// <summary>
    /// Status untuk Purchase Order
    /// </summary>
    public enum PurchaseOrderStatus
    {
        Draft,      // Masih draft, bisa diedit
        Sent,       // Sudah dikirim ke supplier via email
        Closed,
        Received,   // Sudah diterima barangnya
        Cancelled   // Dibatalkan
    }

    /// <summary>
    /// Status untuk Advanced Shipping Notice
    /// </summary>
    public enum ASNStatus
    {
        InTransit,  // Dalam perjalanan
        Arrived,    // Sudah sampai di gudang
        Processed,  // Sudah diproses/diterima (ready for putaway)
        PutAway,    // Sudah di-putaway ke lokasi
        Completed,  // Selesai total (semua item sudah di-putaway)
        Cancelled   // Dibatalkan
    }

    /// <summary>
    /// Status untuk Sales Order
    /// </summary>
    public enum SalesOrderStatus
    {
        Draft,          // Masih draft, bisa diedit
        Confirmed,      // Sudah dikonfirmasi, ready to pick
        Picking,        // Sedang proses picking
        ReadyToShip,    // Sudah dipick, siap dikirim
        Shipped,        // Sudah dikirim ke customer
        Completed,      // Transaksi selesai
        Cancelled       // Dibatalkan
    }

    /// <summary>
    /// Status untuk Picking
    /// </summary>
    public enum PickingStatus
    {
        Pending,        // Menunggu proses picking
        InProgress,     // Sedang proses picking
        Completed,      // Picking selesai
        Cancelled       // Dibatalkan
    }

    /// <summary>
    /// Status untuk Picking Detail
    /// </summary>
    public enum PickingDetailStatus
    {
        Pending,        // Belum dipick
        Picked,         // Sudah dipick lengkap
        Short           // Dipick tapi kurang dari required
    }

    /// <summary>
    /// Status inventory
    /// </summary>
    public enum InventoryStatus
    {
        Available,  // Tersedia untuk dijual
        Reserved,   // Direserve untuk sales order
        Damaged,    // Rusak, tidak bisa dijual
        Quarantine, // Karantina, perlu pengecekan
        Blocked,    // Diblokir karena masalah tertentu
        Empty       // Kosong, quantity = 0
    }

    /// <summary>
    /// Tier warehouse fee berdasarkan harga
    /// </summary>
    public enum WarehouseFeeTier
    {
        Low,        // ≤ 1,000,000 IDR - 5%
        Medium,     // 1,000,000 - 10,000,000 IDR - 3%
        High        // > 10,000,000 IDR - 1%
    }

    /// <summary>
    /// Tipe transaksi inventory
    /// </summary>
    public enum InventoryTransactionType
    {
        Inbound,    // Masuk dari ASN/Putaway
        Outbound,   // Keluar untuk Sales Order
        Adjustment, // Penyesuaian stok
        Transfer,   // Pindah lokasi
        Damage,     // Rusak/hilang
        Return      // Retur dari customer
    }

    /// <summary>
    /// Level prioritas untuk notifikasi
    /// </summary>
    public enum NotificationLevel
    {
        Info,       // Informasi biasa
        Warning,    // Peringatan (stok rendah, dll)
        Error,      // Error yang perlu perhatian
        Critical    // Critical, harus segera ditangani
    }
}