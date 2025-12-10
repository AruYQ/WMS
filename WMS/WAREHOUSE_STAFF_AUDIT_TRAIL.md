# AUDIT TRAIL VIEW - WAREHOUSE STAFF DOCUMENTATION

## OVERVIEW
Warehouse Staff dapat melihat Audit Trail untuk tracking semua aktivitas di sistem, termasuk create, update, delete, dan actions lainnya. Akses VIEW ONLY untuk monitoring dan audit purposes.

## PERMISSIONS REQUIRED
- `AUDIT_VIEW` - View all audit logs

---

## ENDPOINTS

### 1. GET Audit Logs List
**Endpoint:** `GET /api/audittrail`
**Permission:** `AUDIT_VIEW`

**Query Parameters (AuditSearchRequest):**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `action` (string, optional) - CREATE, UPDATE, DELETE, CANCEL, PROCESS, etc.
- `entityType` (string, optional) - PurchaseOrder, SalesOrder, ASN, Picking, Inventory, etc.
- `entityId` (int, optional) - Filter by specific entity ID
- `userId` (int, optional) - Filter by user who performed action
- `fromDate` (DateTime, optional) - Start date filter
- `toDate` (DateTime, optional) - End date filter
- `search` (string, optional) - Search in description/notes

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "action": "CREATE",
        "entityType": "PurchaseOrder",
        "entityId": 10,
        "entityName": "PO20250101001",
        "description": "Created Purchase Order PO20250101001",
        "userId": 5,
        "userName": "John Doe",
        "userEmail": "john@company.com",
        "companyId": 1,
        "oldValues": null,
        "newValues": {
          "PONumber": "PO20250101001",
          "SupplierId": 5,
          "TotalAmount": 5000000,
          "Status": "Draft"
        },
        "ipAddress": "192.168.1.100",
        "userAgent": "Mozilla/5.0...",
        "timestamp": "2025-01-01T10:00:00",
        "additionalData": null
      },
      {
        "id": 2,
        "action": "UPDATE",
        "entityType": "SalesOrder",
        "entityId": 15,
        "entityName": "SO20250101001",
        "description": "Updated Sales Order SO20250101001",
        "userId": 5,
        "userName": "John Doe",
        "oldValues": {
          "Status": "Pending",
          "TotalAmount": 7500000
        },
        "newValues": {
          "Status": "In Progress",
          "TotalAmount": 8000000
        },
        "timestamp": "2025-01-01T11:00:00"
      }
    ],
    "totalCount": 1000,
    "totalPages": 100,
    "currentPage": 1,
    "pageSize": 10
  }
}
```

**Fields Explanation:**
- `action`: Type of action (CREATE, UPDATE, DELETE, CANCEL, PROCESS, SHIP, PUTAWAY, PICK, etc.)
- `entityType`: Entity yang di-action (PurchaseOrder, SalesOrder, ASN, Inventory, etc.)
- `entityId`: ID dari entity
- `entityName`: Nama/Number dari entity (PO Number, SO Number, ASN Number, etc.)
- `description`: Human-readable description
- `oldValues`: Values sebelum action (untuk UPDATE)
- `newValues`: Values setelah action
- `userId`, `userName`: User yang melakukan action
- `timestamp`: Waktu action dilakukan
- `ipAddress`, `userAgent`: Request information

### 2. GET Audit Log Detail
**Endpoint:** `GET /api/audittrail/{id}`
**Permission:** `AUDIT_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "action": "UPDATE",
    "entityType": "SalesOrder",
    "entityId": 15,
    "entityName": "SO20250101001",
    "description": "Updated Sales Order SO20250101001",
    "userId": 5,
    "userName": "John Doe",
    "userEmail": "john@company.com",
    "companyId": 1,
    "oldValues": {
      "Status": "Pending",
      "TotalAmount": 7500000,
      "Notes": "Original notes"
    },
    "newValues": {
      "Status": "In Progress",
      "TotalAmount": 8000000,
      "Notes": "Updated notes"
    },
    "ipAddress": "192.168.1.100",
    "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
    "timestamp": "2025-01-01T11:00:00",
    "additionalData": {
      "pickingId": 5,
      "pickingNumber": "PKG20250101001"
    }
  }
}
```

### 3. GET Audit Statistics
**Endpoint:** `GET /api/audittrail/statistics?fromDate=2025-01-01&toDate=2025-01-31`
**Permission:** `AUDIT_VIEW`

**Query Parameters:**
- `fromDate` (DateTime, required) - Start date
- `toDate` (DateTime, required) - End date

**Response:**
```json
{
  "success": true,
  "data": {
    "totalActions": 5000,
    "actionsByType": {
      "CREATE": 1500,
      "UPDATE": 2000,
      "DELETE": 100,
      "PROCESS": 800,
      "CANCEL": 200,
      "SHIP": 400
    },
    "actionsByEntity": {
      "PurchaseOrder": 800,
      "SalesOrder": 1200,
      "ASN": 600,
      "Picking": 500,
      "Inventory": 1500,
      "Putaway": 400
    },
    "actionsByUser": [
      {
        "userId": 5,
        "userName": "John Doe",
        "actionCount": 500
      }
    ],
    "dailyActivity": [
      {
        "date": "2025-01-01",
        "actionCount": 150
      },
      {
        "date": "2025-01-02",
        "actionCount": 200
      }
    ],
    "mostActiveHours": [
      {
        "hour": 10,
        "actionCount": 300
      },
      {
        "hour": 14,
        "actionCount": 250
      }
    ]
  }
}
```

**Statistics:**
- `totalActions`: Total actions dalam periode
- `actionsByType`: Breakdown by action type
- `actionsByEntity`: Breakdown by entity type
- `actionsByUser`: Top users by action count
- `dailyActivity`: Actions per day
- `mostActiveHours`: Peak activity hours

### 4. GET User Recent Activities
**Endpoint:** `GET /api/audittrail/user-activities?take=10`
**Permission:** `AUDIT_VIEW`

**Query Parameters:**
- `take` (int, default: 10) - Number of recent activities

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 100,
      "action": "CREATE",
      "entityType": "PurchaseOrder",
      "entityName": "PO20250101050",
      "description": "Created Purchase Order PO20250101050",
      "timestamp": "2025-01-15T10:30:00"
    },
    {
      "id": 99,
      "action": "PROCESS",
      "entityType": "Picking",
      "entityName": "PKG20250101025",
      "description": "Processed Picking PKG20250101025",
      "timestamp": "2025-01-15T09:15:00"
    }
  ]
}
```

**Business Rules:**
- Hanya menampilkan activities dari current user
- Ordered by timestamp descending
- Limited by `take` parameter

---

## AUDIT LOG ACTIONS

### Common Actions:
- **CREATE:** Create new entity
- **UPDATE:** Update existing entity
- **DELETE:** Delete entity (soft delete)
- **CANCEL:** Cancel entity (PO, ASN, SO)
- **PROCESS:** Process operation (Picking, Putaway)
- **SHIP:** Ship Sales Order
- **PUTAWAY:** Process putaway
- **PICK:** Process picking
- **ADJUST:** Adjust inventory
- **TOGGLE_STATUS:** Toggle active status
- **UPDATE_STATUS:** Update entity status

### Entity Types:
- PurchaseOrder
- SalesOrder
- ASN (AdvancedShippingNotice)
- Picking
- Inventory
- Item
- Location
- Customer
- Supplier
- User

---

## FILTERING OPTIONS

### By Action:
- CREATE, UPDATE, DELETE
- CANCEL, PROCESS, SHIP
- PUTAWAY, PICK, ADJUST
- All actions

### By Entity Type:
- PurchaseOrder
- SalesOrder
- ASN
- Picking
- Inventory
- Item, Location, Customer, Supplier

### By User:
- Filter by specific user ID
- View own activities

### By Date Range:
- `fromDate`: Start date (inclusive)
- `toDate`: End date (inclusive)
- Default: Last 30 days

### By Entity:
- Filter by specific entity ID
- View all actions for specific PO, SO, ASN, etc.

---

## USE CASES

### 1. Track Purchase Order History:
```
GET /api/audittrail?entityType=PurchaseOrder&entityId=10
```
Shows all actions untuk PO ID 10:
- CREATE: When PO was created
- UPDATE: When PO was modified
- SEND: When PO was sent to supplier
- CANCEL: When PO was cancelled (if applicable)

### 2. Monitor Daily Activities:
```
GET /api/audittrail?fromDate=2025-01-15&toDate=2025-01-15
```
Shows all activities for specific day

### 3. Track User Actions:
```
GET /api/audittrail?userId=5
```
Shows all actions by specific user

### 4. Monitor Inventory Changes:
```
GET /api/audittrail?entityType=Inventory&action=ADJUST
```
Shows all inventory adjustments

### 5. Track Status Changes:
```
GET /api/audittrail?entityType=SalesOrder&action=UPDATE_STATUS
```
Shows all SO status changes

---

## AUDIT DATA STRUCTURE

### OldValues / NewValues:
Stored as JSON, format depends on entity:

**PurchaseOrder:**
```json
{
  "PONumber": "PO20250101001",
  "SupplierId": 5,
  "Status": "Draft",
  "TotalAmount": 5000000,
  "OrderDate": "2025-01-01",
  "Notes": "Order notes"
}
```

**SalesOrder:**
```json
{
  "SONumber": "SO20250101001",
  "CustomerId": 5,
  "Status": "Pending",
  "TotalAmount": 7500000,
  "HoldingLocationId": 6
}
```

**Inventory:**
```json
{
  "ItemId": 10,
  "LocationId": 3,
  "Quantity": 100,
  "Status": "Available"
}
```

---

## SEARCH FUNCTIONALITY

**Search Fields:**
- Entity Name (PO Number, SO Number, ASN Number, etc.)
- Description
- User Name
- Action type
- Entity Type

**Search Implementation:**
- Case-insensitive
- Partial match
- Multiple fields searched

---

## PAGINATION

**Default:**
- Page: 1
- PageSize: 10

**Response Format:**
```json
{
  "items": [...],
  "totalCount": 1000,
  "totalPages": 100,
  "currentPage": 1,
  "pageSize": 10,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

## TIMESTAMP FORMAT

**ISO 8601 Format:**
- `2025-01-01T10:00:00`
- Timezone: UTC or local server timezone

**Display:**
- Frontend should format based on user timezone
- Show relative time (e.g., "2 hours ago") untuk recent activities

---

## ERROR HANDLING

**Common Errors:**
- `400 Bad Request`: Invalid date range or parameters
- `404 Not Found`: Audit log not found
- `403 Forbidden`: Missing permission
- `500 Internal Server Error`: Server error

**Error Messages:**
- "Audit log not found"
- "Invalid date range"
- "Missing required parameters"

---

## BEST PRACTICES

1. **Regular Monitoring:**
   - Review audit logs regularly
   - Check for unusual activities
   - Monitor critical actions (DELETE, CANCEL)

2. **Filtering:**
   - Use date range untuk focus on specific period
   - Filter by entity type untuk specific tracking
   - Filter by user untuk user-specific audit

3. **Investigation:**
   - Use oldValues/newValues untuk track changes
   - Check IP address untuk security
   - Review timestamp untuk timeline

4. **Reporting:**
   - Use statistics untuk overview
   - Export data untuk reporting (if available)
   - Track trends over time

---

## RESTRICTIONS

- ❌ Cannot modify audit logs (immutable)
- ❌ Cannot delete audit logs
- ✅ View only access
- ✅ Can filter and search
- ✅ Can view statistics
