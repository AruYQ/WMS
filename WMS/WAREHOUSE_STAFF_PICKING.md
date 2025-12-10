# PICKING MANAGEMENT - WAREHOUSE STAFF DOCUMENTATION

## OVERVIEW
Warehouse Staff melakukan proses Picking untuk mengambil barang dari Storage Location dan memindahkannya ke Holding Location sebelum di-ship ke customer. Picking dibuat dari Sales Order dan dieksekusi secara bertahap.

## PERMISSIONS REQUIRED
- `PICKING_VIEW` - View semua Picking
- `PICKING_MANAGE` - Create Picking, Process Picking
- `PICKING_UPDATE` - Update quantity picked

---

## ENDPOINTS

### 1. GET Picking List
**Endpoint:** `GET /api/picking`
**Permission:** `PICKING_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `status` (string, optional) - Pending, In Progress, Completed, Cancelled
- `search` (string, optional) - Search by Picking Number, SO Number, Customer Name

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "pickingNumber": "PKG20250101001",
      "salesOrderId": 10,
      "salesOrderNumber": "SO20250101001",
      "customerName": "PT Customer XYZ",
      "pickingDate": "2025-01-01",
      "completedDate": null,
      "status": "Pending",
      "totalQuantityRequired": 150,
      "totalQuantityPicked": 0,
      "completionPercentage": 0,
      "createdDate": "2025-01-01T10:00:00"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "totalPages": 5,
    "totalCount": 50,
    "pageSize": 10
  }
}
```

### 2. GET Picking Detail
**Endpoint:** `GET /api/picking/{id}`
**Permission:** `PICKING_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "pickingNumber": "PKG20250101001",
    "salesOrderId": 10,
    "salesOrderNumber": "SO20250101001",
    "customerName": "PT Customer XYZ",
    "pickingDate": "2025-01-01",
    "completedDate": null,
    "status": "Pending",
    "holdingLocationId": 6,
    "holdingLocationName": "Shipping Area",
    "notes": null,
    "details": [
      {
        "id": 1,
        "salesOrderDetailId": 5,
        "itemId": 10,
        "itemCode": "PROD-A-001",
        "itemName": "Product A",
        "itemUnit": "PCS",
        "locationId": 3,
        "locationCode": "STG-A-01",
        "locationName": "Storage A-01",
        "quantityRequired": 100,
        "quantityToPick": 0,
        "quantityPicked": 0,
        "remainingQuantity": 100,
        "status": "Pending",
        "notes": null
      }
    ]
  }
}
```

### 3. GET Available Locations for Item
**Endpoint:** `GET /api/picking/locations/{itemId}?quantityRequired=100`
**Permission:** `PICKING_VIEW`

**Query Parameters:**
- `quantityRequired` (int, optional) - Filter locations dengan stock >= quantityRequired

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "locationId": 3,
      "locationCode": "STG-A-01",
      "locationName": "Storage A-01",
      "availableStock": 300,
      "status": "Available",
      "currentCapacity": 500,
      "maxCapacity": 1000
    },
    {
      "locationId": 4,
      "locationCode": "STG-A-02",
      "locationName": "Storage A-02",
      "availableStock": 200,
      "status": "Available",
      "currentCapacity": 300,
      "maxCapacity": 500
    }
  ]
}
```

**Business Rules:**
- Hanya menampilkan Storage locations (bukan holding)
- Hanya inventory dengan status "Available"
- Hanya inventory dengan quantity > 0
- Ordered by location code

### 4. CREATE Picking
**Endpoint:** `POST /api/picking`
**Permission:** `PICKING_MANAGE`

**Request Body:**
```json
{
  "salesOrderId": 10,
  "notes": "Handle with care"
}
```

**Business Rules:**
- Sales Order harus exist dan status = "Pending" atau "In Progress"
- Tidak bisa create jika sudah ada Picking aktif (Pending/In Progress) untuk SO yang sama
- Sales Order harus punya Holding Location
- **AUTO-CREATE PICKING DETAILS:**
  - Untuk setiap SO Detail, cari available inventory di Storage locations
  - Distribute quantity across multiple locations (FIFO)
  - Create PickingDetail dengan LocationId dari inventory
- Picking Number auto-generated: `PKG{YYYYMMDD}{NNN}`
- Status default: "Pending"
- **AUTO-UPDATE SO STATUS:** SO status → "In Progress"

**Response:**
```json
{
  "success": true,
  "message": "Picking created successfully",
  "data": {
    "id": 1
  }
}
```

**Picking Detail Creation Logic:**
1. Untuk setiap SO Detail:
   - Cari inventory di Storage locations (FIFO order)
   - Jika stock di 1 location cukup → Create 1 PickingDetail
   - Jika stock tersebar → Create multiple PickingDetails
   - RemainingQuantity = QuantityRequired (initial)

**Validation Errors:**
- "Picking can only be created for Sales Orders with status Pending or In Progress"
- "An active picking already exists for this Sales Order"
- "Sales Order must have a holding location"
- "No available inventory found for ItemId X in Sales Order Y"

### 5. PROCESS Picking
**Endpoint:** `POST /api/picking/{id}/process`
**Permission:** `PICKING_MANAGE`

**Request Body:**
```json
{
  "details": [
    {
      "pickingDetailId": 1,
      "quantityToPick": 100,
      "locationId": 3
    },
    {
      "pickingDetailId": 2,
      "quantityToPick": 50,
      "locationId": 4
    }
  ]
}
```

**Business Rules:**
- Tidak bisa process jika status = "Cancelled"
- Tidak bisa process jika SO status = "Cancelled"
- QuantityToPick tidak boleh > RemainingQuantity
- Source Location harus punya stock cukup
- **PROCESS FLOW:**
  1. Reduce inventory dari Source Location (Storage)
  2. Reduce Source Location capacity
  3. Add inventory ke Holding Location
  4. Add Holding Location capacity
  5. Update PickingDetail: QuantityPicked += quantityToPick, RemainingQuantity -= quantityToPick
  6. Update Picking Status:
     - Jika semua details complete → Status = "Completed", SO Status = "Picked"
     - Jika ada yang picked tapi belum complete → Status = "In Progress"

**Inventory Movement:**
```
Storage Location (Source):
- Inventory.Quantity -= quantityToPick
- Location.CurrentCapacity -= quantityToPick
- If Quantity == 0: Status = "Empty"

Holding Location (Target):
- Inventory.Quantity += quantityToPick (create or update)
- Location.CurrentCapacity += quantityToPick
- SourceReference = "SO-{SalesOrderId}-{SalesOrderDetailId}"
```

**Response:**
```json
{
  "success": true,
  "message": "Picking processed successfully"
}
```

**Validation Errors:**
- "Cannot process a cancelled Picking"
- "Cannot process Picking for a cancelled Sales Order"
- "Quantity to pick (150) exceeds remaining quantity (100) for item PROD-A-001"
- "Insufficient stock at location STG-A-01 for item PROD-A-001. Available: 50, Required: 100"
- "Source location must be selected for item PROD-A-001"
- "Holding location is invalid or inactive"

### 6. GET Picking by Sales Order
**Endpoint:** `GET /api/picking/salesorder/{salesOrderId}`
**Permission:** `PICKING_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "pickingNumber": "PKG20250101001"
  }
}
```

**Business Rules:**
- Return null jika tidak ada picking untuk SO tersebut

### 7. GET Picking Details Page
**Endpoint:** `GET /Picking/Details/{id}`
**Permission:** `PICKING_VIEW`

**MVC View:** Menampilkan detail picking dengan form untuk process picking

---

## WORKFLOW

### Normal Flow:
1. **Sales Order Created** → Status: "Pending"
2. **Create Picking** → Status: "Pending", SO Status: "In Progress"
   - Auto-create PickingDetails dari SO Details
   - Assign locations dari available inventory (FIFO)
3. **Process Picking** → Execute picks
   - Move inventory: Storage → Holding Location
   - Update QuantityPicked, RemainingQuantity
   - Status: "In Progress" (partial) atau "Completed" (all done)
4. **Picking Completed** → Status: "Completed", SO Status: "Picked"
5. **Ship SO** → Status: "Shipped" (inventory reduced from holding)

### Cancellation Flow:
1. **Cancel SO** → Jika ada Picking Pending, cancel Picking juga
2. Picking Status → "Cancelled"

---

## VALIDATION RULES

1. **Sales Order Validation:**
   - SO must exist and active
   - SO status = "Pending" atau "In Progress"
   - SO must have Holding Location
   - No active picking exists for SO

2. **Item Validation:**
   - Item must have available stock in Storage locations
   - Stock must be sufficient for required quantity
   - Inventory status = "Available"

3. **Location Validation:**
   - Source Location (Storage):
     - Category = "Storage"
     - Must have sufficient stock
     - Must be active
   - Target Location (Holding):
     - Category = "Other"
     - Must have sufficient capacity
     - Must be active

4. **Quantity Validation:**
   - QuantityToPick > 0
   - QuantityToPick ≤ RemainingQuantity
   - QuantityToPick ≤ AvailableStock (source location)

5. **Status Validation:**
   - Pending: Ready to process
   - In Progress: Partially picked
   - Completed: All items picked, SO status = "Picked"
   - Cancelled: Cannot process

---

## PICKING DETAIL STATUS

- **Pending:** Belum di-pick
- **Short:** Sudah di-pick tapi quantity < required (RemainingQuantity > 0)
- **Picked:** Sudah complete (RemainingQuantity = 0)

**Status Calculation:**
```csharp
if (RemainingQuantity == 0)
    Status = "Picked"
else if (QuantityPicked > 0)
    Status = "Short"
else
    Status = "Pending"
```

---

## LOCATION DISTRIBUTION (FIFO)

Saat create Picking:
- Inventory diambil berdasarkan FIFO (First In First Out)
- Order by CreatedDate
- Jika 1 location tidak cukup, distribute ke multiple locations
- Setiap location create 1 PickingDetail

**Example:**
- Required: 150 units
- Location A: 100 units (older stock)
- Location B: 80 units (newer stock)
- Result: 2 PickingDetails
  - Detail 1: Location A, 100 units
  - Detail 2: Location B, 50 units

---

## INVENTORY MOVEMENT DETAIL

**From Storage Location:**
```csharp
sourceInventory.Quantity -= quantityToPick
sourceLocation.CurrentCapacity -= quantityToPick
if (sourceInventory.Quantity == 0)
    sourceInventory.Status = "Empty"
```

**To Holding Location:**
```csharp
// Find or create inventory
holdingInventory = GetInventory(itemId, holdingLocationId)
if (holdingInventory == null)
    Create new Inventory
else
    holdingInventory.Quantity += quantityToPick

holdingLocation.CurrentCapacity += quantityToPick
holdingInventory.SourceReference = "SO-{salesOrderId}-{soDetailId}"
holdingInventory.Notes = "Picked from {sourceLocation.Code} for SO {soNumber}"
```

---

## PICKING STATUS AUTO-UPDATE

**After Process:**
```csharp
allDetailsComplete = All PickingDetails have Status = "Picked"

if (allDetailsComplete && PickingDetails.Any())
    Picking.Status = "Completed"
    Picking.CompletedDate = DateTime.Now
    SalesOrder.Status = "Picked"
else if (Any PickingDetail has QuantityPicked > 0)
    Picking.Status = "In Progress"
```

---

## ERROR HANDLING

**Common Errors:**
- `400 Bad Request`: Validation failed, insufficient stock
- `404 Not Found`: Picking/SO/Item/Location not found
- `403 Forbidden`: Missing permission
- `500 Internal Server Error`: Database error

**Error Messages:**
- "An active picking already exists for this Sales Order"
- "Sales Order must have a holding location"
- "Quantity to pick (150) exceeds remaining quantity (100)"
- "Insufficient stock at location STG-A-01 for item PROD-A-001. Available: 50, Required: 100"
- "Cannot process a cancelled Picking"
- "Holding location is invalid or inactive"

---

## BEST PRACTICES

1. **Process in Batches:**
   - Process multiple items dalam 1 request untuk efisiensi
   - Pastikan semua quantity valid sebelum process

2. **Location Selection:**
   - Pilih location dengan stock terdekat
   - Prioritaskan location dengan stock cukup untuk 1 picking detail

3. **Quantity Tracking:**
   - Always check RemainingQuantity sebelum process
   - Tidak bisa pick lebih dari RemainingQuantity

4. **Status Monitoring:**
   - Monitor completion percentage
   - Complete all details untuk update SO status ke "Picked"
