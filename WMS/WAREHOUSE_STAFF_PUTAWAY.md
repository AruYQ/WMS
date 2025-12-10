# PUTAWAY MANAGEMENT - WAREHOUSE STAFF DOCUMENTATION

## OVERVIEW
Warehouse Staff melakukan proses Putaway untuk memindahkan barang dari Holding Location (setelah ASN arrived) ke Storage Location untuk penyimpanan jangka panjang. Putaway dilakukan per ASN dan bisa di-process secara partial.

## PERMISSIONS REQUIRED
- `INVENTORY_VIEW` - View Putaway dashboard, ASN list
- `PUTAWAY_MANAGE` - Process Putaway operations

---

## ENDPOINTS

### 1. GET Putaway Dashboard
**Endpoint:** `GET /api/Putaway/Dashboard`
**Permission:** `INVENTORY_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalProcessedASNs": 50,
    "totalPendingItems": 25,
    "totalPendingQuantity": 1250,
    "todayPutawayCount": 5
  }
}
```

**Statistics:**
- `totalProcessedASNs`: ASN dengan status "Processed"
- `totalPendingItems`: ASNDetail dengan RemainingQuantity > 0
- `totalPendingQuantity`: Total RemainingQuantity dari semua ASNDetail
- `todayPutawayCount`: ASN yang arrived hari ini

### 2. GET ASNs Ready for Putaway
**Endpoint:** `GET /api/Putaway`
**Permission:** `INVENTORY_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `statusFilter` (string, optional)
- `showTodayOnly` (bool, default: false) - Filter ASN yang arrived hari ini

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "asnId": 1,
      "asnNumber": "ASN20250101001",
      "poNumber": "PO20250101001",
      "supplierName": "PT Supplier ABC",
      "actualArrivalDate": "2025-01-01",
      "status": "Processed",
      "totalItemTypes": 5,
      "totalQuantity": 500,
      "pendingPutawayCount": 3,
      "completionPercentage": 60,
      "isCompleted": false
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

**Business Rules:**
- Hanya ASN dengan status "Processed"
- Show completion percentage berdasarkan items yang sudah complete
- `isCompleted` = true jika semua ASNDetail sudah RemainingQuantity = 0

### 3. GET Putaway Process Page
**Endpoint:** `GET /Inventory/ProcessPutaway?asnId=1&bulk=false`
**Permission:** `INVENTORY_VIEW`

**MVC View:** Form untuk process putaway dengan:
- ASN information
- List items dengan RemainingQuantity > 0
- Available Storage Locations
- Location capacity information

**Query Parameters:**
- `asnId` (int, required) - ASN ID
- `bulk` (bool, default: false) - Bulk processing mode

**ViewModel Data:**
```json
{
  "asnId": 1,
  "asnNumber": "ASN20250101001",
  "poNumber": "PO20250101001",
  "supplierName": "PT Supplier ABC",
  "shipmentDate": "2025-01-10",
  "processedDate": "2025-01-15",
  "availableLocations": [
    {
      "id": 3,
      "code": "STG-A-01",
      "name": "Storage A-01",
      "maxCapacity": 1000,
      "currentCapacity": 500,
      "availableCapacity": 500
    }
  ],
  "putawayDetails": [
    {
      "asnDetailId": 1,
      "itemId": 10,
      "itemCode": "PROD-A-001",
      "itemName": "Product A",
      "totalQuantity": 100,
      "remainingQuantity": 100,
      "quantityToPutaway": 100,
      "locationId": 0,
      "notes": ""
    }
  ]
}
```

### 4. PROCESS Putaway
**Endpoint:** `POST /Inventory/ProcessPutaway`
**Permission:** `INVENTORY_MANAGE`

**Request (Form Data):**
```
asnDetailId: 1
quantityToPutaway: 100
locationId: 3
asnId: 1
itemId: 10
```

**Business Rules:**
- QuantityToPutaway > 0
- QuantityToPutaway ≤ RemainingQuantity
- Location must exist, active, category = "Storage"
- Location capacity must be sufficient
- **CRITICAL:** Source location adalah Holding Location dari ASN
- **CRITICAL:** Target location adalah Storage Location yang dipilih user

**Process Flow:**
1. Validate ASNDetail exists dan RemainingQuantity cukup
2. Validate Storage Location (target) valid dan capacity cukup
3. Get Holding Location dari ASN (source)
4. Find inventory di Holding Location
5. Validate stock di holding location
6. **Move Inventory:**
   - Create or Update inventory di Storage Location (target)
   - Reduce inventory di Holding Location (source)
   - If holding inventory quantity = 0, remove record
7. **Update ASNDetail:**
   - AlreadyPutAwayQuantity += quantityToPutaway
   - RemainingQuantity -= quantityToPutaway
8. **Update Location Capacity:**
   - Storage Location: CurrentCapacity += quantityToPutaway
   - Holding Location: CurrentCapacity -= quantityToPutaway

**Response:**
```json
{
  "success": true,
  "message": "Successfully putaway 100 units of PROD-A-001 to STG-A-01",
  "data": {
    "asnDetailId": 1,
    "remainingQuantity": 0,
    "alreadyPutAwayQuantity": 100,
    "isCompleted": true
  }
}
```

**Validation Errors:**
- "Quantity must be greater than 0"
- "Location must be selected"
- "Cannot putaway 150 units. Only 100 remaining."
- "Location not found or inactive"
- "Putaway can only be done to Storage locations, not holding locations"
- "Insufficient location capacity. Available: 50, Required: 100"
- "Item not found in holding location"
- "Insufficient quantity in holding location. Available: 50, Required: 100"

### 5. GET Putaway Details View
**Endpoint:** `GET /inventory/putaway/{asnId}/details`
**Permission:** `INVENTORY_VIEW`

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
      "shippedQuantity": 100,
      "alreadyPutAwayQuantity": 60,
      "remainingQuantity": 40,
      "actualPricePerItem": 50000,
      "totalActualValue": 5000000,
      "status": "Partial",
      "statusIndonesia": "Sebagian",
      "asnNumber": "ASN20250101001",
      "purchaseOrderNumber": "PO20250101001",
      "supplierName": "PT Supplier ABC",
      "shipmentDate": "2025-01-10",
      "expectedArrivalDate": "2025-01-15",
      "actualArrivalDate": "2025-01-15",
      "asnStatus": "Processed",
      "canBeProcessed": true,
      "isOnTime": true,
      "delayDays": 0,
      "createdDate": "2025-01-01T10:00:00",
      "modifiedDate": "2025-01-15T14:30:00"
    }
  ]
}
```

---

## WORKFLOW

### Normal Flow:
1. **ASN Created** → Status: "Pending"
2. **ASN Status → Arrived** → Auto-create inventory di Holding Location
3. **ASN Status → Processed** → Ready for Putaway
4. **Process Putaway** → Move items dari Holding → Storage
   - Process bisa partial (tidak harus semua sekaligus)
   - RemainingQuantity berkurang per process
5. **All Items Putaway** → ASNDetail.RemainingQuantity = 0 untuk semua items

### Partial Putaway:
- Bisa putaway sebagian quantity
- Multiple putaway untuk 1 item ASN Detail
- RemainingQuantity akan berkurang setiap kali process

**Example:**
- ASNDetail: ShippedQuantity = 100, RemainingQuantity = 100
- Process 1: Putaway 60 → RemainingQuantity = 40
- Process 2: Putaway 40 → RemainingQuantity = 0 (Complete)

---

## VALIDATION RULES

1. **ASN Validation:**
   - ASN must exist and not deleted
   - ASN status = "Processed"
   - ASN must have Holding Location

2. **ASNDetail Validation:**
   - ASNDetail must exist
   - RemainingQuantity > 0
   - QuantityToPutaway ≤ RemainingQuantity

3. **Location Validation:**
   - **Source (Holding):**
     - Category = "Other"
     - Must have inventory for the item
     - Quantity must be sufficient
   - **Target (Storage):**
     - Category = "Storage" (CRITICAL!)
     - Must be active
     - Capacity must be sufficient

4. **Inventory Validation:**
   - Inventory must exist in Holding Location
   - Inventory.Quantity ≥ QuantityToPutaway

5. **Capacity Validation:**
   - Storage Location: AvailableCapacity ≥ QuantityToPutaway
   - AvailableCapacity = MaxCapacity - CurrentCapacity

---

## INVENTORY MOVEMENT DETAIL

### From Holding Location (Source):
```csharp
// Find inventory in holding location
holdingInventory = GetInventory(itemId, holdingLocationId)

// Reduce quantity
holdingInventory.Quantity -= quantityToPutaway
holdingLocation.CurrentCapacity -= quantityToPutaway

// Remove if empty
if (holdingInventory.Quantity <= 0)
    RemoveInventory(holdingInventory)
```

### To Storage Location (Target):
```csharp
// Find or create inventory in storage location
storageInventory = GetInventory(itemId, storageLocationId)

if (storageInventory == null)
    // Create new inventory
    storageInventory = new Inventory {
        ItemId = itemId,
        LocationId = storageLocationId,
        Quantity = quantityToPutaway,
        Status = "Available",
        SourceReference = asnNumber,
        Notes = $"Putaway from ASN {asnNumber}"
    }
else
    // Update existing inventory
    storageInventory.Quantity += quantityToPutaway
    
storageLocation.CurrentCapacity += quantityToPutaway
```

---

## ASNDETAIL UPDATE

```csharp
// Update ASNDetail
asnDetail.AlreadyPutAwayQuantity += quantityToPutaway
asnDetail.RemainingQuantity -= quantityToPutaway

// Note: RemainingQuantity calculation handled by ASNDetail.InitializeRemainingQuantity()
// Formula: RemainingQuantity = ShippedQuantity - AlreadyPutAwayQuantity
```

---

## LOCATION CAPACITY MANAGEMENT

### Storage Location Capacity:
- **Before Putaway:** CurrentCapacity = existing inventory
- **After Putaway:** CurrentCapacity += quantityToPutaway
- **Validation:** CurrentCapacity + quantityToPutaway ≤ MaxCapacity

### Holding Location Capacity:
- **Before Putaway:** CurrentCapacity = inventory in holding
- **After Putaway:** CurrentCapacity -= quantityToPutaway
- **Note:** Capacity berkurang karena inventory dipindahkan

---

## COMPLETION TRACKING

**Item Complete:**
- RemainingQuantity = 0
- AlreadyPutAwayQuantity = ShippedQuantity

**ASN Complete:**
- All ASNDetails have RemainingQuantity = 0
- Can mark ASN as fully processed (optional)

**Dashboard Display:**
- Completion percentage = (Completed Items / Total Items) × 100
- `isCompleted` = true jika semua items complete

---

## AVAILABLE LOCATIONS FILTER

**For Putaway Process:**
- Only Storage locations shown (category = "Storage")
- Only active locations
- Display capacity information:
  - CurrentCapacity (from actual inventory)
  - MaxCapacity
  - AvailableCapacity
  - CapacityPercentage

**Capacity Calculation:**
```csharp
// Get actual inventory capacity
currentCapacity = Sum(Inventory.Quantity) 
                  WHERE LocationId = locationId 
                  AND CompanyId = companyId
                  
availableCapacity = MaxCapacity - CurrentCapacity
capacityPercentage = (CurrentCapacity / MaxCapacity) × 100
```

---

## ERROR HANDLING

**Common Errors:**
- `400 Bad Request`: Validation failed, insufficient capacity/quantity
- `404 Not Found`: ASN/ASNDetail/Location/Item not found
- `403 Forbidden`: Missing permission
- `500 Internal Server Error`: Database error

**Error Messages:**
- "Cannot putaway 150 units. Only 100 remaining."
- "Putaway can only be done to Storage locations, not holding locations"
- "Insufficient location capacity. Available: 50, Required: 100"
- "Item not found in holding location"
- "Insufficient quantity in holding location. Available: 50, Required: 100"

---

## BEST PRACTICES

1. **Capacity Planning:**
   - Check available capacity sebelum putaway
   - Pilih location dengan capacity cukup
   - Monitor capacity percentage

2. **Partial Putaway:**
   - Bisa putaway sebagian untuk flexibility
   - Track RemainingQuantity untuk monitoring
   - Complete all items untuk optimal tracking

3. **Location Selection:**
   - Pilih Storage location terdekat
   - Pertimbangkan location category dan purpose
   - Monitor capacity untuk future planning

4. **Inventory Tracking:**
   - SourceReference diisi dengan ASN Number
   - Notes untuk tracking history
   - Maintain inventory accuracy

---

## PUTAWAY VS PICKING

**Putaway (Inbound):**
- Direction: Holding Location → Storage Location
- Source: ASN (inbound from supplier)
- Purpose: Store received goods
- Location: Storage (final storage)

**Picking (Outbound):**
- Direction: Storage Location → Holding Location
- Source: Sales Order (outbound to customer)
- Purpose: Prepare for shipping
- Location: Storage → Holding (temporary before ship)
