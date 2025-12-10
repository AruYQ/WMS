# INVENTORY MANAGEMENT - WAREHOUSE STAFF DOCUMENTATION

## OVERVIEW
Warehouse Staff mengelola inventory/stock di warehouse, termasuk view stock, create inventory records, update quantity, adjust stock, dan delete inventory records.

## PERMISSIONS REQUIRED
- `INVENTORY_VIEW` - View inventory list, dashboard, details
- `INVENTORY_MANAGE` - Create, Update, Delete, Adjust inventory

---

## ENDPOINTS

### 1. GET Inventory Dashboard
**Endpoint:** `GET /api/Inventory/Dashboard`
**Permission:** `INVENTORY_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalItems": 500,
    "availableStock": 10000,
    "lowStockItems": 25,
    "outOfStockItems": 10
  }
}
```

**Statistics:**
- `totalItems`: Total inventory records
- `availableStock`: Total quantity dengan status "Available"
- `lowStockItems`: Items dengan quantity ≤ 10
- `outOfStockItems`: Items dengan quantity = 0

### 2. GET Inventory List
**Endpoint:** `GET /api/Inventory/List`
**Permission:** `INVENTORY_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `status` (string, optional) - Available, Empty, Reserved
- `search` (string, optional) - Search by Item Code, Item Name, Location Code

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "itemId": 10,
      "itemCode": "PROD-A-001",
      "itemName": "Product A",
      "locationId": 3,
      "locationCode": "STG-A-01",
      "locationName": "Storage A-01",
      "locationCategory": "Storage",
      "quantity": 100,
      "status": "Available",
      "lastUpdated": "2025-01-15T10:00:00",
      "sourceReference": "ASN20250101001",
      "notes": "Putaway from ASN"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "totalPages": 50,
    "totalCount": 500,
    "pageSize": 10
  }
}
```

### 3. GET Inventory Detail
**Endpoint:** `GET /api/Inventory/{id}`
**Permission:** `INVENTORY_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "itemId": 10,
    "itemCode": "PROD-A-001",
    "itemName": "Product A",
    "locationId": 3,
    "locationCode": "STG-A-01",
    "locationName": "Storage A-01",
    "quantity": 100,
    "status": "Available",
    "lastUpdated": "2025-01-15T10:00:00",
    "sourceReference": "ASN20250101001",
    "notes": "Putaway from ASN"
  }
}
```

### 4. CREATE Inventory
**Endpoint:** `POST /api/Inventory`
**Permission:** `INVENTORY_MANAGE`

**Request Body:**
```json
{
  "itemId": 10,
  "locationId": 3,
  "quantity": 50,
  "status": "Available",
  "sourceReference": "MANUAL-ENTRY",
  "notes": "Manual stock entry"
}
```

**Business Rules:**
- Item must exist and active
- Location must exist and active
- Quantity ≥ 0
- Status default: "Available"
- If inventory already exists untuk item+location, bisa update quantity atau create separate record (depends on business logic)

**Response:**
```json
{
  "success": true,
  "message": "Inventory created successfully",
  "data": {
    "id": 1
  }
}
```

### 5. UPDATE Inventory
**Endpoint:** `PUT /api/Inventory/{id}`
**Permission:** `INVENTORY_MANAGE`

**Request Body:**
```json
{
  "quantity": 75,
  "status": "Available",
  "notes": "Updated quantity"
}
```

**Business Rules:**
- Quantity ≥ 0
- Status: Available, Empty, Reserved
- Update LastUpdated timestamp
- Update ModifiedBy, ModifiedDate

**Response:**
```json
{
  "success": true,
  "message": "Inventory updated successfully"
}
```

### 6. DELETE Inventory
**Endpoint:** `DELETE /api/Inventory/{id}`
**Permission:** `INVENTORY_MANAGE`

**Business Rules:**
- Soft delete (IsDeleted = true)
- Set DeletedBy, DeletedDate
- Note: Pastikan tidak ada transaksi yang reference inventory ini

**Response:**
```json
{
  "success": true,
  "message": "Inventory deleted successfully"
}
```

### 7. ADJUST Inventory Quantity
**Endpoint:** `PATCH /api/Inventory/{id}/adjust`
**Permission:** `INVENTORY_MANAGE`

**Request Body:**
```json
{
  "quantity": 10,
  "adjustmentType": "Add",
  "reason": "Stock correction after physical count"
}
```

**Adjustment Types:**
- `"Add"`: Increase quantity (Quantity += adjustment)
- `"Subtract"`: Decrease quantity (Quantity -= adjustment)

**Business Rules:**
- Quantity > 0
- AdjustmentType: "Add" or "Subtract"
- For Subtract: New quantity must be ≥ 0
- Reason recommended untuk audit trail

**Response:**
```json
{
  "success": true,
  "message": "Inventory adjusted successfully"
}
```

**Validation Errors:**
- "Insufficient inventory for adjustment" (if Subtract makes quantity < 0)
- "Quantity must be greater than 0"
- "AdjustmentType must be 'Add' or 'Subtract'"

**Example Adjustments:**
```json
// Add 10 units
{
  "quantity": 10,
  "adjustmentType": "Add",
  "reason": "Found additional stock"
}
// Current: 100 → New: 110

// Subtract 5 units
{
  "quantity": 5,
  "adjustmentType": "Subtract",
  "reason": "Damaged goods removed"
}
// Current: 100 → New: 95
```

### 8. GET Inventory by Location
**Endpoint:** `GET /api/location/{locationId}/inventory`
**Permission:** `INVENTORY_VIEW` (via Location controller)

**Response:**
```json
{
  "success": true,
  "data": {
    "location": {
      "id": 3,
      "code": "STG-A-01",
      "name": "Storage A-01"
    },
    "inventories": [
      {
        "id": 1,
        "itemCode": "PROD-A-001",
        "itemName": "Product A",
        "quantity": 100,
        "unit": "PCS",
        "lastUpdated": "2025-01-15T10:00:00"
      }
    ],
    "totalItems": 5,
    "totalQuantity": 500
  }
}
```

---

## INVENTORY STATUS

### Status Values:
- **Available:** Stock tersedia untuk digunakan
- **Empty:** Quantity = 0 atau stock habis
- **Reserved:** Stock di-reserve untuk order tertentu (future use)

### Status Auto-Update:
```csharp
if (Quantity == 0)
    Status = "Empty"
else if (Quantity > 0 && Status == "Empty")
    Status = "Available"
```

---

## INVENTORY SOURCES

### SourceReference Values:
- **ASN Number:** `ASN20250101001` - From ASN putaway
- **SO Reference:** `SO-10-5` - From Sales Order picking
- **Manual Entry:** `MANUAL-ENTRY` - Manual stock entry
- **Adjustment:** `ADJUST-{Date}` - Stock adjustment
- **Transfer:** `TRANSFER-{From}-{To}` - Stock transfer

---

## VALIDATION RULES

1. **Item Validation:**
   - Item must exist
   - Item must be active
   - Item belongs to same company

2. **Location Validation:**
   - Location must exist
   - Location must be active
   - Location belongs to same company

3. **Quantity Validation:**
   - Quantity ≥ 0
   - For Subtract adjustment: New quantity must be ≥ 0

4. **Status Validation:**
   - Status: Available, Empty, Reserved
   - Auto-update based on quantity

---

## INVENTORY TRACKING

### Fields:
- **ItemId:** Reference ke Item
- **LocationId:** Reference ke Location (Storage atau Holding)
- **Quantity:** Current quantity
- **Status:** Available, Empty, Reserved
- **SourceReference:** Source of inventory (ASN, SO, Manual, etc.)
- **Notes:** Additional notes
- **LastUpdated:** Last modification timestamp
- **CreatedDate:** Creation timestamp
- **ModifiedDate:** Last modification date

### Audit Trail:
- All inventory changes logged ke AuditLog
- Track: CREATE, UPDATE, DELETE, ADJUST actions
- Include old/new values untuk UPDATE

---

## INVENTORY BY LOCATION TYPE

### Storage Locations:
- Final storage location
- Inventory untuk sales
- Used in Sales Order picking
- Used in Putaway operations (target)

### Holding Locations:
- Temporary storage
- Inventory from ASN (after arrived)
- Inventory for Sales Order (after picking)
- Used in Putaway operations (source)

---

## INVENTORY QUERIES

### Get Available Stock (for Sales Order):
```sql
SELECT SUM(Quantity) 
FROM Inventories 
WHERE ItemId = @itemId
AND Location.Category = 'Storage'
AND Status = 'Available'
AND Quantity > 0
```

### Get Stock by Location:
```sql
SELECT LocationId, SUM(Quantity) 
FROM Inventories 
WHERE ItemId = @itemId
AND Location.Category = 'Storage'
AND Status = 'Available'
GROUP BY LocationId
```

### Low Stock Alert:
```sql
SELECT ItemId, SUM(Quantity) as TotalStock
FROM Inventories
WHERE Status = 'Available'
GROUP BY ItemId
HAVING SUM(Quantity) <= 10
```

---

## ERROR HANDLING

**Common Errors:**
- `400 Bad Request`: Validation failed, insufficient quantity
- `404 Not Found`: Inventory/Item/Location not found
- `403 Forbidden`: Missing permission
- `500 Internal Server Error`: Database error

**Error Messages:**
- "Item not found"
- "Location not found"
- "Insufficient inventory for adjustment"
- "Quantity must be greater than 0"

---

## BEST PRACTICES

1. **Stock Accuracy:**
   - Regular physical count
   - Use adjustments untuk corrections
   - Track all movements

2. **Source Tracking:**
   - Always set SourceReference
   - Use Notes untuk additional info
   - Maintain audit trail

3. **Location Management:**
   - Monitor capacity
   - Track by location category
   - Optimize location usage

4. **Status Management:**
   - Auto-update status based on quantity
   - Use Reserved status untuk planned orders
   - Monitor Empty status untuk restocking

---

## INVENTORY FLOW

### Inbound (ASN):
1. ASN Arrived → Create inventory di Holding Location
2. Putaway → Move dari Holding → Storage Location
3. Inventory di Storage ready untuk sales

### Outbound (Sales Order):
1. Create SO → Validate stock di Storage
2. Create Picking → Allocate inventory dari Storage
3. Process Picking → Move dari Storage → Holding Location
4. Ship SO → Reduce inventory dari Holding Location

### Adjustments:
1. Physical count → Adjust quantity
2. Damaged goods → Subtract quantity
3. Found stock → Add quantity
