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
        /// Role names - 3 Role System
        /// </summary>
        public static class Roles
        {
            public const string SuperAdmin = "SuperAdmin";      // Company management only
            public const string Admin = "Admin";                // Master data management
            public const string WarehouseStaff = "WarehouseStaff"; // Operational tasks
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
        public const string ASN_STATUS_PENDING = "Pending";
        public const string ASN_STATUS_IN_TRANSIT = "In Transit";
        public const string ASN_STATUS_ARRIVED = "Arrived";
        public const string ASN_STATUS_PROCESSED = "Processed";
        public const string ASN_STATUS_CANCELLED = "Cancelled";
        public const string ASN_STATUS_COMPLETED = "Completed";

        // Status Sales Order
        public const string SO_STATUS_DRAFT = "Draft";
        public const string SO_STATUS_CONFIRMED = "Confirmed";
        public const string SO_STATUS_PICKING = "Picking";
        public const string SO_STATUS_READY_TO_SHIP = "ReadyToShip";
        public const string SO_STATUS_SHIPPED = "Shipped";
        public const string SO_STATUS_COMPLETED = "Completed";
        public const string SO_STATUS_CANCELLED = "Cancelled";

        // Status Picking
        public const string PICKING_STATUS_PENDING = "Pending";
        public const string PICKING_STATUS_IN_PROGRESS = "InProgress";
        public const string PICKING_STATUS_COMPLETED = "Completed";
        public const string PICKING_STATUS_CANCELLED = "Cancelled";

        // Status Picking Detail
        public const string PICKING_DETAIL_STATUS_PENDING = "Pending";
        public const string PICKING_DETAIL_STATUS_PICKED = "Picked";
        public const string PICKING_DETAIL_STATUS_SHORT = "Short";

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
        public const int DASHBOARD_REFRESH_INTERVAL = 300; // 5 minutes in seconds

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
        public const string CACHE_KEY_ITEMS = "items_active";
        public const string CACHE_KEY_LOCATIONS = "locations_active";

        // Pagination
        public const int MIN_PAGE_SIZE = 5;
        public const int MAX_PAGE_SIZE_LIMIT = 1000;

        // Validation
        public const int MAX_NAME_LENGTH = 100;
        public const int MAX_DESCRIPTION_LENGTH = 500;
        public const int MAX_NOTES_LENGTH = 1000;

        // Date Formats
        public const string DATE_FORMAT = "dd/MM/yyyy";
        public const string DATETIME_FORMAT = "dd/MM/yyyy HH:mm";
        public const string TIME_FORMAT = "HH:mm";

        // Currency
        public const string CURRENCY_SYMBOL = "Rp";
        public const string CURRENCY_CODE = "IDR";
        public const string CURRENCY_FORMAT = "C"; // Standard currency format

        // Status Colors (CSS Classes)
        public const string STATUS_SUCCESS = "text-success";
        public const string STATUS_WARNING = "text-warning";
        public const string STATUS_DANGER = "text-danger";
        public const string STATUS_INFO = "text-info";
        public const string STATUS_SECONDARY = "text-secondary";

        // Badge Colors
        public const string BADGE_SUCCESS = "badge bg-success";
        public const string BADGE_WARNING = "badge bg-warning";
        public const string BADGE_DANGER = "badge bg-danger";
        public const string BADGE_INFO = "badge bg-info";
        public const string BADGE_SECONDARY = "badge bg-secondary";

        // Capacity Thresholds
        public const int CAPACITY_WARNING_THRESHOLD = 20;
        public const int CAPACITY_CRITICAL_THRESHOLD = 5;

        // Stock Level Thresholds
        public const int LOW_STOCK_THRESHOLD = 10;
        public const int CRITICAL_STOCK_THRESHOLD = 5;
        public const int ZERO_STOCK_THRESHOLD = 0;

        // Notification Types
        public const string NOTIFICATION_SUCCESS = "success";
        public const string NOTIFICATION_WARNING = "warning";
        public const string NOTIFICATION_ERROR = "error";
        public const string NOTIFICATION_INFO = "info";

        // Action Types
        public const string ACTION_CREATE = "Create";
        public const string ACTION_UPDATE = "Update";
        public const string ACTION_DELETE = "Delete";
        public const string ACTION_VIEW = "View";
        public const string ACTION_EXPORT = "Export";
        public const string ACTION_IMPORT = "Import";

        // Module Names
        public const string MODULE_INVENTORY = "Inventory";
        public const string MODULE_PURCHASE_ORDER = "PurchaseOrder";
        public const string MODULE_SALES_ORDER = "SalesOrder";
        public const string MODULE_ASN = "ASN";
        public const string MODULE_SUPPLIER = "Supplier";
        public const string MODULE_CUSTOMER = "Customer";
        public const string MODULE_ITEM = "Item";
        public const string MODULE_LOCATION = "Location";
        public const string MODULE_USER = "User";
        public const string MODULE_REPORT = "Report";

        // Generic Permission Names
        public const string PERMISSION_VIEW = "VIEW";
        public const string PERMISSION_CREATE = "CREATE";
        public const string PERMISSION_UPDATE = "UPDATE";
        public const string PERMISSION_DELETE = "DELETE";
        public const string PERMISSION_EXPORT = "EXPORT";
        public const string PERMISSION_IMPORT = "IMPORT";
        public const string PERMISSION_APPROVE = "APPROVE";
        public const string PERMISSION_REJECT = "REJECT";

        // Company Management (SuperAdmin)
        public const string COMPANY_MANAGE = "COMPANY_MANAGE";

        // Master Data Permissions (Admin - MANAGE, WarehouseStaff - VIEW)
        public const string ITEM_VIEW = "ITEM_VIEW";
        public const string ITEM_MANAGE = "ITEM_MANAGE";
        public const string LOCATION_VIEW = "LOCATION_VIEW";
        public const string LOCATION_MANAGE = "LOCATION_MANAGE";
        public const string CUSTOMER_VIEW = "CUSTOMER_VIEW";
        public const string CUSTOMER_MANAGE = "CUSTOMER_MANAGE";
        public const string SUPPLIER_VIEW = "SUPPLIER_VIEW";
        public const string SUPPLIER_MANAGE = "SUPPLIER_MANAGE";
        public const string USER_VIEW = "USER_VIEW";
        public const string USER_MANAGE = "USER_MANAGE";

        // Operational Permissions (WarehouseStaff - MANAGE)
        public const string PO_MANAGE = "PO_MANAGE";
        public const string ASN_MANAGE = "ASN_MANAGE";
        public const string SO_MANAGE = "SO_MANAGE";
        public const string PICKING_MANAGE = "PICKING_MANAGE";
        public const string PUTAWAY_MANAGE = "PUTAWAY_MANAGE";
        
        // Inventory Permissions (Admin - VIEW, WarehouseStaff - MANAGE)
        public const string INVENTORY_VIEW = "INVENTORY_VIEW";
        public const string INVENTORY_MANAGE = "INVENTORY_MANAGE";
        public const string INVENTORY_ADJUST = "INVENTORY_ADJUST";
        public const string INVENTORY_TRANSFER = "INVENTORY_TRANSFER";
        public const string INVENTORY_PUTAWAY = "INVENTORY_PUTAWAY";

        // Reports & Audit (Admin - CREATE, WarehouseStaff - VIEW)
        public const string REPORT_CREATE = "REPORT_CREATE";
        public const string REPORT_VIEW = "REPORT_VIEW";
        public const string AUDIT_VIEW = "AUDIT_VIEW";
    }
}