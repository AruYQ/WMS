// Konstanta yang digunakan di seluruh aplikasi

namespace WMS.Utilities
{
    /// <summary>
    /// Konstanta untuk aplikasi WMS
    /// </summary>
    public static class Constants
    {
        #region Authentication & Authorization

        /// <summary>
        /// Role names
        /// </summary>
        public static class Roles
        {
            public const string SuperAdmin = "SuperAdmin";
            public const string Admin = "Admin";
            public const string Manager = "Manager";
            public const string Supervisor = "Supervisor";
            public const string Operator = "Operator";
            public const string User = "User";
            public const string Viewer = "Viewer";
        }

        /// <summary>
        /// Claim types
        /// </summary>
        public static class ClaimTypes
        {
            public const string CompanyId = "CompanyId";
            public const string CompanyCode = "CompanyCode";
            public const string CompanyName = "CompanyName";
            public const string UserId = "UserId";
            public const string FullName = "FullName";
            public const string Permissions = "Permissions";
        }

        /// <summary>
        /// Policy names
        /// </summary>
        public static class Policies
        {
            public const string AdminOnly = "AdminOnly";
            public const string ManagerOrAdmin = "ManagerOrAdmin";
            public const string SupervisorOrAbove = "SupervisorOrAbove";
            public const string AllUsers = "AllUsers";
            public const string RequireCompany = "RequireCompany";
        }

        /// <summary>
        /// Session keys
        /// </summary>
        public static class SessionKeys
        {
            public const string UserId = "UserId";
            public const string CompanyId = "CompanyId";
            public const string UserRoles = "UserRoles";
            public const string LastActivity = "LastActivity";
            public const string ReturnUrl = "ReturnUrl";
        }

        #endregion
        // Format nomor dokumen
        public const string PO_NUMBER_FORMAT = "PO-{0:yyyyMMdd}-{1:D3}";
        public const string ASN_NUMBER_FORMAT = "ASN-{0:yyyyMMdd}-{1:D3}";
        public const string SO_NUMBER_FORMAT = "SO-{0:yyyyMMdd}-{1:D3}";

        // Status Purchase Order
        public const string PO_STATUS_DRAFT = "Draft";
        public const string PO_STATUS_SENT = "Sent";
        public const string PO_STATUS_CLOSED = "Closed";
        public const string PO_STATUS_RECEIVED = "Received";
        public const string PO_STATUS_CANCELLED = "Cancelled";

        // Status ASN
        public const string ASN_STATUS_IN_TRANSIT = "In Transit";
        public const string ASN_STATUS_ARRIVED = "Arrived";
        public const string ASN_STATUS_PROCESSED = "Processed";
        public const string ASN_STATUS_CANCELLED = "Cancelled";

        // Status Sales Order
        public const string SO_STATUS_DRAFT = "Draft";
        public const string SO_STATUS_CONFIRMED = "Confirmed";
        public const string SO_STATUS_SHIPPED = "Shipped";
        public const string SO_STATUS_COMPLETED = "Completed";
        public const string SO_STATUS_CANCELLED = "Cancelled";

        // Status Inventory
        public const string INVENTORY_STATUS_AVAILABLE = "Available";
        public const string INVENTORY_STATUS_RESERVED = "Reserved";
        public const string INVENTORY_STATUS_DAMAGED = "Damaged";
        public const string INVENTORY_STATUS_QUARANTINE = "Quarantine";
        public const string INVENTORY_STATUS_BLOCKED = "Blocked";
        public const string INVENTORY_STATUS_EMPTY = "Empty";

        public const decimal WAREHOUSE_FEE_LOW = 0.03m;      // FIXED: 3% untuk harga ≤ 1 juta (was 0.05m)
        public const decimal WAREHOUSE_FEE_MEDIUM = 0.02m;   // FIXED: 2% untuk harga 1-10 juta (was 0.03m)
        public const decimal WAREHOUSE_FEE_HIGH = 0.01m;     // 1% untuk harga > 10 juta (unchanged)

        // Warehouse Fee Thresholds (dalam IDR) - unchanged
        public const decimal WAREHOUSE_FEE_THRESHOLD_LOW = 1_000_000m;   // 1 juta
        public const decimal WAREHOUSE_FEE_THRESHOLD_HIGH = 10_000_000m; // 10 juta

        // Default values
        public const int DEFAULT_PAGE_SIZE = 10;
        public const int MAX_PAGE_SIZE = 100;
        public const string DEFAULT_SORT_FIELD = "CreatedDate";
        public const string DEFAULT_SORT_ORDER = "desc";

        // Email Templates
        public const string EMAIL_TEMPLATE_PO = "PurchaseOrderTemplate";
        public const string EMAIL_TEMPLATE_SO_CONFIRMATION = "SalesOrderConfirmation";
        public const string EMAIL_TEMPLATE_NOTIFICATION = "NotificationTemplate";

        // System Users (untuk audit trail)
        public const string SYSTEM_USER = "SYSTEM";
        public const string ADMIN_USER = "ADMIN";

        // Location Patterns
        public const string LOCATION_CODE_PATTERN = @"^[A-Z]-\d{2}-\d{2}$"; // Format: A-01-01

        // File Upload
        public const int MAX_FILE_SIZE_MB = 5;
        public const string ALLOWED_FILE_EXTENSIONS = ".pdf,.jpg,.jpeg,.png,.xlsx,.xls";

        // Cache Keys
        public const string CACHE_KEY_SUPPLIERS = "suppliers_active";
        public const string CACHE_KEY_CUSTOMERS = "customers_active";
        public const string CACHE_KEY_ITEMS = "items_active";
        public const string CACHE_KEY_LOCATIONS = "locations_active";
        public const int CACHE_EXPIRY_MINUTES = 30;

        // Validation Messages
        public const string VALIDATION_REQUIRED = "Field ini wajib diisi.";
        public const string VALIDATION_EMAIL = "Format email tidak valid.";
        public const string VALIDATION_PHONE = "Format nomor telepon tidak valid.";
        public const string VALIDATION_POSITIVE_NUMBER = "Nilai harus lebih besar dari 0.";
        public const string VALIDATION_MAX_LENGTH = "Maksimal {0} karakter.";

        // Business Rules
        public const int LOW_STOCK_THRESHOLD = 10;        // Stok dianggap rendah jika ≤ 10
        public const int CRITICAL_STOCK_THRESHOLD = 5;    // Stok kritis jika ≤ 5
        public const double LOCATION_FULL_THRESHOLD = 0.8; // Lokasi dianggap hampir penuh jika ≥ 80%

        // DateTime Formats
        public const string DATE_FORMAT = "dd/MM/yyyy";
        public const string DATETIME_FORMAT = "dd/MM/yyyy HH:mm";
        public const string TIME_FORMAT = "HH:mm";

        // Currency
        public const string CURRENCY_SYMBOL = "Rp";
        public const string CURRENCY_FORMAT = "C0"; // Format: Rp 1.000.000

        // Dashboard Refresh Interval (in seconds)
        public const int DASHBOARD_REFRESH_INTERVAL = 300; // 5 menit

        // Export File Names
        public const string EXPORT_PO_FILENAME = "PurchaseOrders_{0:yyyyMMdd}.xlsx";
        public const string EXPORT_SO_FILENAME = "SalesOrders_{0:yyyyMMdd}.xlsx";
        public const string EXPORT_INVENTORY_FILENAME = "Inventory_{0:yyyyMMdd}.xlsx";

        // Success Messages
        public const string SUCCESS_CREATE = "Data berhasil dibuat.";
        public const string SUCCESS_UPDATE = "Data berhasil diperbarui.";
        public const string SUCCESS_DELETE = "Data berhasil dihapus.";
        public const string SUCCESS_SEND_EMAIL = "Email berhasil dikirim.";

        // Error Messages
        public const string ERROR_CREATE = "Gagal membuat data.";
        public const string ERROR_UPDATE = "Gagal memperbarui data.";
        public const string ERROR_DELETE = "Gagal menghapus data.";
        public const string ERROR_NOT_FOUND = "Data tidak ditemukan.";
        public const string ERROR_INSUFFICIENT_STOCK = "Stok tidak mencukupi.";
        public const string ERROR_SEND_EMAIL = "Gagal mengirim email.";
        public const string ERROR_INVALID_OPERATION = "Operasi tidak valid untuk status saat ini.";
    }

    /// <summary>
    /// Helper class untuk warehouse fee calculation
    /// </summary>
    public static class WarehouseFeeHelper
    {
        /// <summary>
        /// Menentukan tier warehouse fee berdasarkan harga
        /// </summary>
        /// <param name="price">Harga item</param>
        /// <returns>Tier warehouse fee</returns>
        public static WarehouseFeeTier GetWarehouseFeeTier(decimal price)
        {
            if (price <= Constants.WAREHOUSE_FEE_THRESHOLD_LOW)
                return WarehouseFeeTier.Low;

            if (price <= Constants.WAREHOUSE_FEE_THRESHOLD_HIGH)
                return WarehouseFeeTier.Medium;

            return WarehouseFeeTier.High;
        }

        // <summary>
        /// Mendapatkan rate warehouse fee berdasarkan harga - FIXED
        /// </summary>
        /// <param name="price">Harga item</param>
        /// <returns>Rate warehouse fee (decimal)</returns>
        public static decimal GetWarehouseFeeRate(decimal price)
        {
            var tier = GetWarehouseFeeTier(price);
            return tier switch
            {
                WarehouseFeeTier.Low => Constants.WAREHOUSE_FEE_LOW,       // 3%
                WarehouseFeeTier.Medium => Constants.WAREHOUSE_FEE_MEDIUM, // 2%
                WarehouseFeeTier.High => Constants.WAREHOUSE_FEE_HIGH,     // 1%
                _ => Constants.WAREHOUSE_FEE_LOW
            };
        }

        /// <summary>
        /// Menghitung warehouse fee amount
        /// </summary>
        /// <param name="price">Harga item</param>
        /// <param name="quantity">Jumlah item</param>
        /// <returns>Total warehouse fee</returns>
        public static decimal CalculateWarehouseFee(decimal price, int quantity = 1)
        {
            var rate = GetWarehouseFeeRate(price);
            return price * rate * quantity;
        }

        /// <summary>
        /// Mendapatkan deskripsi tier dalam bahasa Indonesia - FIXED
        /// </summary>
        /// <param name="price">Harga item</param>
        /// <returns>Deskripsi tier</returns>
        public static string GetTierDescription(decimal price)
        {
            var tier = GetWarehouseFeeTier(price);
            return tier switch
            {
                WarehouseFeeTier.Low => $"Harga Rendah (≤ {Constants.WAREHOUSE_FEE_THRESHOLD_LOW:C}) - Fee {Constants.WAREHOUSE_FEE_LOW:P}",
                WarehouseFeeTier.Medium => $"Harga Menengah ({Constants.WAREHOUSE_FEE_THRESHOLD_LOW:C} - {Constants.WAREHOUSE_FEE_THRESHOLD_HIGH:C}) - Fee {Constants.WAREHOUSE_FEE_MEDIUM:P}",
                WarehouseFeeTier.High => $"Harga Tinggi (> {Constants.WAREHOUSE_FEE_THRESHOLD_HIGH:C}) - Fee {Constants.WAREHOUSE_FEE_HIGH:P}",
                _ => "Unknown"
            };
        }
    }
}