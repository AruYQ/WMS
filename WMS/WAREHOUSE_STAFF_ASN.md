# ASN (ADVANCED SHIPPING NOTICE) MANAGEMENT - WAREHOUSE STAFF DOCUMENTATION

## OVERVIEW
Warehouse Staff mengelola proses penerimaan barang dari supplier melalui ASN. ASN dibuat dari Purchase Order dan tracking status pengiriman sampai barang sampai dan diproses (putaway).

## PERMISSIONS REQUIRED
- `ASN_VIEW` - View semua ASN
- `ASN_MANAGE` - Create, Update, Cancel, Delete ASN, Update Status

---

## ENDPOINTS

### 1. GET ASN List
**Endpoint:** `GET /api/asn`
**Permission:** `ASN_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `status` (string, optional) - Pending, On Delivery, Arrived, Processed, Cancelled
- `search` (string, optional) - Search by ASN Number, PO Number, Supplier Name

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "asnNumber": "ASN20250101001",
      "purchaseOrderId": 10,
      "purchaseOrderNumber": "PO20250101001",
      "supplierName": "PT Supplier ABC",
      "status": "Pending",
      "expectedArrivalDate": "2025-01-15",
      "actualArrivalDate": null,
      "totalItems": 5,
      "totalQuantity": 500,
      "createdDate": "2025-01-01T10:00:00",
      "createdBy": "user1"
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

### 2. GET ASN Detail
**Endpoint:** `GET /api/asn/{id}`
**Permission:** `ASN_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "asnNumber": "ASN20250101001",
    "purchaseOrderId": 10,
    "purchaseOrderNumber": "PO20250101001",
    "supplierName": "PT Supplier ABC",
    "supplierContact": "John Doe",
    "status": "Pending",
    "expectedArrivalDate": "2025-01-15",
    "actualArrivalDate": null,
    "shipmentDate": "2025-01-10",
    "carrierName": "JNE Express",
    "trackingNumber": "TRK20250101001",
    "notes": "Fragile items",
    "holdingLocationId": 5,
    "holdingLocationName": "Receiving Area",
    "createdDate": "2025-01-01T10:00:00",
    "details": [
      {
        "id": 1,
        "itemId": 10,
        "itemCode": "PROD-A-001",
        "itemName": "Product A",
        "shippedQuantity": 100,
        "actualPricePerItem": 50000,
        "alreadyPutAwayQuantity": 0,
        "remainingQuantity": 100,
        "notes": null
      }
    ]
  }
}
```

### 3. GET Available Purchase Orders
**Endpoint:** `GET /api/asn/purchaseorders`
**Permission:** `ASN_MANAGE`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 10,
      "poNumber": "PO20250101001",
      "supplierId": 5,
      "supplierName": "PT Supplier ABC",
      "orderDate": "2025-01-01",
      "expectedDeliveryDate": "2025-01-15",
      "status": "Sent",
      "totalAmount": 5000000,
      "items": [
        {
          "id": 1,
          "itemId": 10,
          "itemCode": "PROD-A-001",
          "itemName": "Product A",
          "orderedQuantity": 100,
          "unitPrice": 50000,
          "totalPrice": 5000000
        }
      ]
    }
  ]
}
```

**Business Rules:**
- Hanya PO dengan status "Sent" atau "Draft" (bukan "Received" atau "Cancelled")
- PO yang sudah memiliki ASN aktif tidak ditampilkan (optional - depends on business logic)

### 4. GET Items for Purchase Order
**Endpoint:** `GET /api/asn/items/{purchaseOrderId}`
**Permission:** `ASN_MANAGE`

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
      "itemUnit": "PCS",
      "purchasePrice": 45000,
      "orderedQuantity": 100,
      "unitPrice": 50000,
      "totalPrice": 5000000
    }
  ]
}
```

### 5. CREATE ASN
**Endpoint:** `POST /api/asn`
**Permission:** `ASN_MANAGE`

**Request Body:**
```json
{
  "purchaseOrderId": 10,
  "expectedArrivalDate": "2025-01-15",
  "shipmentDate": "2025-01-10",
  "carrierName": "JNE Express",
  "trackingNumber": "TRK20250101001",
  "notes": "Fragile items",
  "holdingLocationId": 5,
  "items": [
    {
      "itemId": 10,
      "shippedQuantity": 100,
      "actualPricePerItem": 50000,
      "notes": "Good condition"
    }
  ]
}
```

**Business Rules:**
- PO harus exist dan status bukan "Cancelled"
- Minimal 1 item required
- Shipped quantity tidak boleh melebihi ordered quantity dari PO Detail
- Holding Location harus category "Other" (bukan Storage)
- Holding Location capacity harus cukup untuk total quantity
- ASN Number auto-generated: `ASN{YYYYMMDD}{NNN}`
- Tracking Number auto-generated: `TRK{YYYYMMDD}{NNN}` (jika tidak diisi)
- **CRITICAL - AUTO-UPDATE PO STATUS:** 
  - PO status berubah ke "Received" saat ASN dibuat
  - This is automatic, tidak perlu manual update
- Status ASN default: "Pending"
- ASNDetail.RemainingQuantity = ShippedQuantity (initial)

**Response:**
```json
{
  "success": true,
  "message": "ASN created successfully",
  "data": {
    "id": 1
  }
}
```

**Validation Errors:**
- "Purchase Order not found"
- "Cannot create ASN for a cancelled Purchase Order"
- "At least one item must be provided"
- "Shipped quantity (150) cannot exceed ordered quantity (100) for item Product A"
- "Holding location not found or inactive"
- "Holding location must be of category 'Other', not 'Storage'"
- "Insufficient capacity in holding location 'Receiving Area'. Available: 200, Required: 500"

### 6. UPDATE ASN
**Endpoint:** `PUT /api/asn/{id}`
**Permission:** `ASN_MANAGE`

**Request Body:**
```json
{
  "expectedArrivalDate": "2025-01-16",
  "shipmentDate": "2025-01-11",
  "carrierName": "J&T Express",
  "trackingNumber": "TRK20250101002",
  "notes": "Updated notes",
  "holdingLocationId": 6,
  "items": [
    {
      "itemId": 10,
      "shippedQuantity": 105,
      "actualPricePerItem": 51000,
      "notes": "Updated"
    }
  ]
}
```

**Business Rules:**
- Bisa update selama ASN belum di-cancel
- Update items akan update existing ASNDetail
- RemainingQuantity dihitung ulang: RemainingQuantity = ShippedQuantity - AlreadyPutAwayQuantity
- Jika update holding location, validasi capacity dan category

**Response:**
```json
{
  "success": true,
  "message": "ASN updated successfully"
}
```

### 7. CANCEL ASN
**Endpoint:** `PATCH /api/asn/{id}/cancel`
**Permission:** `ASN_MANAGE`

**Request Body:**
```json
{
  "reason": "Supplier cancelled shipment"
}
```

**Business Rules:**
- Hanya bisa cancel jika status = "Pending"
- Status berubah ke "Cancelled"
- **CRITICAL - ROLLBACK PO STATUS:**
  - Jika PO status = "Received" dan tidak ada ASN aktif lainnya untuk PO tersebut
  - PO status kembali ke "Sent"
  - Logged di audit trail
- Reason ditambahkan ke Notes dengan format:
  ```
  [Existing Notes]
  
  Cancelation reason : [Reason]
  ```

**Response:**
```json
{
  "success": true,
  "message": "ASN cancelled successfully and Purchase Order status has been rolled back to 'Sent'",
  "data": {
    "asnId": 1,
    "asnNumber": "ASN20250101001",
    "asnStatus": "Cancelled",
    "poId": 10,
    "poNumber": "PO20250101001",
    "poStatus": "Sent",
    "poPreviousStatus": "Received"
  }
}
```

**Validation Errors:**
- "Reason is required"
- "ASN cannot be cancelled. Current status: On Delivery. Only Pending status can be cancelled."

### 8. UPDATE ASN STATUS
**Endpoint:** `PATCH /api/asn/{id}/status`
**Permission:** `ASN_MANAGE`

**Request Body:**
```json
{
  "status": "Arrived"
}
```

**Status Flow:**
1. **Pending** → **On Delivery** → **Arrived** → **Processed**
2. Status bisa di-skip (langsung ke Arrived dari Pending)

**Business Rules:**
- Status "Arrived":
  - Auto-set `actualArrivalDate` = current date/time
  - **CRITICAL - AUTO-CREATE INVENTORY:**
    - Untuk setiap ASNDetail, create inventory di Holding Location
    - Inventory quantity = ShippedQuantity dari ASNDetail
    - Inventory status = "Available"
    - Location capacity bertambah
    - Jika inventory sudah ada di holding location untuk item tersebut, quantity ditambahkan (bukan create baru)
- Status "Processed":
  - Manual update setelah semua items sudah di-putaway
  - Tidak ada auto-operation

**Response:**
```json
{
  "success": true,
  "message": "ASN status updated successfully. Inventory has been automatically created at holding location 'Receiving Area' for 5 items."
}
```

**Inventory Creation Logic:**
```csharp
foreach (var asnDetail in asn.ASNDetails)
{
    // Check if inventory exists
    var existingInventory = GetInventory(itemId, holdingLocationId);
    
    if (existingInventory != null)
        existingInventory.Quantity += asnDetail.ShippedQuantity;
    else
        CreateInventory(itemId, holdingLocationId, asnDetail.ShippedQuantity);
    
    holdingLocation.CurrentCapacity += asnDetail.ShippedQuantity;
}
```

### 9. DELETE ASN
**Endpoint:** `DELETE /api/asn/{id}`
**Permission:** `ASN_MANAGE`

**Business Rules:**
- Soft delete (IsDeleted = true)
- Tidak ada business rules khusus
- Set DeletedBy, DeletedDate

**Response:**
```json
{
  "success": true,
  "message": "ASN deleted successfully"
}
```

### 10. GET Dashboard Statistics
**Endpoint:** `GET /api/asn/dashboard`
**Permission:** `ASN_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalASNs": 100,
    "pendingASNs": 10,
    "onDeliveryASNs": 20,
    "arrivedASNs": 15,
    "processedASNs": 50,
    "cancelledASNs": 5
  }
}
```

### 11. GET Holding Locations
**Endpoint:** `GET /api/asn/locations`
**Permission:** `ASN_VIEW`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 5,
      "name": "Receiving Area",
      "code": "RCV-001",
      "currentCapacity": 500,
      "maxCapacity": 1000,
      "availableCapacity": 500
    }
  ]
}
```

**Business Rules:**
- Hanya location dengan category "Other" (holding location)
- Hanya location yang active
- Display capacity information

### 12. ADVANCED SEARCH Purchase Orders
**Endpoint:** `POST /api/asn/purchaseorders/advanced-search`
**Permission:** `ASN_VIEW`

**Request Body:**
```json
{
  "poNumber": "PO2025",
  "supplierName": "ABC",
  "orderDateFrom": "2025-01-01",
  "orderDateTo": "2025-01-31",
  "page": 1,
  "pageSize": 10
}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 10,
      "poNumber": "PO20250101001",
      "supplierName": "PT Supplier ABC",
      "supplierEmail": "supplier@abc.com",
      "orderDate": "2025-01-01",
      "expectedDeliveryDate": "2025-01-15",
      "status": "Sent",
      "totalAmount": 5000000,
      "itemCount": 5,
      "notes": null,
      "createdDate": "2025-01-01T10:00:00"
    }
  ],
  "totalCount": 10,
  "totalPages": 1,
  "currentPage": 1
}
```

### 13. ADVANCED SEARCH Locations
**Endpoint:** `POST /api/asn/locations/advanced-search`
**Permission:** `ASN_VIEW`

**Request Body:**
```json
{
  "name": "Receiving",
  "code": "RCV",
  "createdDateFrom": "2025-01-01",
  "createdDateTo": "2025-01-31",
  "page": 1,
  "pageSize": 10
}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 5,
      "name": "Receiving Area",
      "code": "RCV-001",
      "description": "Main receiving area",
      "maxCapacity": 1000,
      "currentCapacity": 500,
      "availableCapacity": 500,
      "isActive": true,
      "createdDate": "2025-01-01T10:00:00"
    }
  ],
  "totalCount": 5,
  "totalPages": 1,
  "currentPage": 1
}
```

### 14. SEARCH Items (for ASN autocomplete)
**Endpoint:** `GET /api/asn/items/search?q=PROD&purchaseOrderId=10`
**Permission:** `ASN_VIEW`

**Query Parameters:**
- `q` (string, required, min 2 chars) - Search term
- `purchaseOrderId` (int, optional) - Filter by PO items only

**Response:**
```json
[
  {
    "id": 10,
    "itemCode": "PROD-A-001",
    "name": "Product A",
    "unit": "PCS",
    "purchasePrice": 45000,
    "description": "Product description"
  }
]
```

### 15. GET Top Items (for ASN autocomplete)
**Endpoint:** `GET /api/asn/items/top?purchaseOrderId=10`
**Permission:** `ASN_VIEW`

**Response:**
```json
[
  {
    "id": 10,
    "itemCode": "PROD-A-001",
    "name": "Product A",
    "unit": "PCS",
    "purchasePrice": 45000,
    "description": "Product description"
  }
]
```

**Business Rules:**
- Returns top 3 items
- If purchaseOrderId provided, filter by PO items
- Ordered by ItemCode

---

## WORKFLOW

### Normal Flow:
1. **Create ASN** dari PO → Status: "Pending" → **PO Status: "Received"**
   - Select PO yang status "Sent"
   - Enter shipment details
   - Select items dengan quantities
   - Select holding location
   - ASN created, PO auto-updated
2. **Update Status** → "On Delivery" (barang dalam perjalanan)
   - Optional status update
3. **Update Status** → "Arrived" → **Auto-create inventory** di holding location
   - ActualArrivalDate auto-set
   - Inventory created untuk semua items
   - Holding location capacity bertambah
4. **Process Putaway** → Pindah dari holding ke storage location
   - ASNDetail.RemainingQuantity berkurang
   - AlreadyPutAwayQuantity bertambah
5. **Update Status** → "Processed" (semua barang sudah di-putaway)
   - Manual update setelah semua items complete

### Cancellation Flow:
1. **Cancel ASN** (Pending only) → Status: "Cancelled"
   - Check: Status = "Pending"
   - **Rollback PO** → Jika tidak ada ASN aktif, PO status kembali ke "Sent"
   - Reason ditambahkan ke Notes

---

## ASNDETAIL TRACKING

### Fields:
- **ShippedQuantity:** Quantity yang di-ship dari supplier
- **AlreadyPutAwayQuantity:** Quantity yang sudah di-putaway
- **RemainingQuantity:** Quantity yang masih perlu di-putaway
  - Formula: `RemainingQuantity = ShippedQuantity - AlreadyPutAwayQuantity`
  - Auto-calculated by `ASNDetail.InitializeRemainingQuantity()`

### Status Tracking:
- **Complete:** RemainingQuantity = 0 (all putaway)
- **Partial:** RemainingQuantity > 0 but AlreadyPutAwayQuantity > 0
- **Pending:** AlreadyPutAwayQuantity = 0

---

## VALIDATION RULES

1. **PO Validation:**
   - PO must exist
   - PO status bukan "Cancelled" atau "Received" (untuk create)
   - PO belongs to same company

2. **Item Validation:**
   - Item must exist in PO (dari PO Details)
   - ShippedQuantity ≤ OrderedQuantity (dari PO Detail)
   - Item belongs to same company

3. **Location Validation:**
   - Holding Location must be category "Other"
   - Holding Location must be active
   - Capacity must be sufficient
   - AvailableCapacity = MaxCapacity - CurrentCapacity

4. **Status Validation:**
   - **Pending:** Can update, cancel, change status
   - **On Delivery:** Can update, change status
   - **Arrived:** Auto-create inventory, ready for putaway
   - **Processed:** Final state (all items putaway)
   - **Cancelled:** Final state, cannot modify

---

## INVENTORY AUTO-CREATION (Status = Arrived)

**Trigger:** When ASN status changed to "Arrived"

**Process:**
1. Untuk setiap ASNDetail:
   - Check inventory di holding location
   - Jika sudah ada: Update quantity (add ShippedQuantity)
   - Jika belum ada: Create inventory baru
   - Status = "Available"
   - SourceReference = ASN Number
   - Notes = "From ASN {ASNNumber}"
2. Update holding location:
   - CurrentCapacity += Total ShippedQuantity
   - If CurrentCapacity >= MaxCapacity: IsFull = true

**Inventory Record:**
```json
{
  "itemId": 10,
  "locationId": 5, // Holding Location
  "quantity": 100, // ShippedQuantity
  "status": "Available",
  "sourceReference": "ASN20250101001",
  "notes": "From ASN ASN20250101001"
}
```

---

## ASN NUMBER GENERATION

**Format:** `ASN{YYYYMMDD}{NNN}`

**Example:**
- `ASN20250101001` → Date 2025-01-01, Sequence 001
- `ASN20250101002` → Date 2025-01-01, Sequence 002

**Logic:**
```csharp
var today = DateTime.Now;
var prefix = $"ASN{today:yyyyMMdd}";

// Get last ASN for today
var lastASN = GetLatestASN(prefix);
var nextNumber = (lastASN?.Sequence ?? 0) + 1;

return $"{prefix}{nextNumber:D3}";
```

---

## TRACKING NUMBER GENERATION

**Format:** `TRK{YYYYMMDD}{NNN}`

**Example:**
- `TRK20250101001` → Date 2025-01-01, Sequence 001

**Logic:**
- Auto-generated jika tidak diisi user
- Format sama dengan ASN Number tapi prefix "TRK"
- Unique per company

---

## ERROR HANDLING

**Common Errors:**
- `400 Bad Request`: Validation failed, insufficient capacity
- `404 Not Found`: ASN/PO/Item/Location not found
- `403 Forbidden`: Missing permission
- `500 Internal Server Error`: Database error

**Error Messages:**
- "Cannot create ASN for a cancelled Purchase Order"
- "Insufficient capacity in holding location 'Receiving Area'. Available: 200, Required: 500"
- "Shipped quantity (150) cannot exceed ordered quantity (100) for item Product A"
- "Holding location must be of category 'Other', not 'Storage'"
- "ASN cannot be cancelled. Current status: On Delivery. Only Pending status can be cancelled."

---

## BEST PRACTICES

1. **ASN Creation:**
   - Verify PO details sebelum create
   - Check holding location capacity
   - Enter accurate shipped quantities
   - Track carrier and tracking number

2. **Status Management:**
   - Update status sesuai actual progress
   - Mark "Arrived" saat barang benar-benar sampai
   - Mark "Processed" setelah semua putaway

3. **Putaway Coordination:**
   - Monitor RemainingQuantity untuk tracking
   - Complete putaway untuk optimal tracking
   - Update ASN status setelah complete

4. **Cancellation:**
   - Cancel hanya jika benar-benar perlu
   - Provide clear reason
   - Check PO impact sebelum cancel
