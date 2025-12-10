# SALES ORDER MANAGEMENT - WAREHOUSE STAFF DOCUMENTATION

## OVERVIEW
Warehouse Staff mengelola Sales Order (SO) dari customer, termasuk create order dengan validasi stock, proses picking, dan shipping. SO digunakan untuk menjual barang ke customer.

## PERMISSIONS REQUIRED
- `SO_VIEW` - View semua Sales Orders
- `SO_MANAGE` - Create, Update, Cancel, Delete, Update Status

---

## ENDPOINTS

### 1. GET Sales Order List
**Endpoint:** `GET /api/salesorder`
**Permission:** `SO_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `status` (string, optional) - Pending, In Progress, Picked, Shipped, Cancelled
- `search` (string, optional) - Search by SO Number, Customer Name

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "soNumber": "SO20250101001",
      "customerId": 5,
      "customerName": "PT Customer XYZ",
      "orderDate": "2025-01-01",
      "requiredDate": "2025-01-10",
      "status": "Pending",
      "totalAmount": 7500000,
      "holdingLocationId": 6,
      "holdingLocationName": "Shipping Area",
      "totalItems": 3,
      "totalQuantity": 150,
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

### 2. GET Sales Order Detail
**Endpoint:** `GET /api/salesorder/{id}`
**Permission:** `SO_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "soNumber": "SO20250101001",
    "customerId": 5,
    "customerName": "PT Customer XYZ",
    "customerEmail": "customer@xyz.com",
    "orderDate": "2025-01-01",
    "requiredDate": "2025-01-10",
    "status": "Pending",
    "totalAmount": 7500000,
    "holdingLocationId": 6,
    "holdingLocationName": "Shipping Area",
    "notes": "Urgent order",
    "createdDate": "2025-01-01T10:00:00",
    "createdBy": "user1",
    "details": [
      {
        "id": 1,
        "itemId": 10,
        "itemCode": "PROD-A-001",
        "itemName": "Product A",
        "itemUnit": "PCS",
        "quantity": 100,
        "unitPrice": 75000,
        "totalPrice": 7500000,
        "notes": null
      }
    ]
  }
}
```

### 3. CREATE Sales Order
**Endpoint:** `POST /api/salesorder`
**Permission:** `SO_MANAGE`

**Request Body:**
```json
{
  "customerId": 5,
  "orderDate": "2025-01-01",
  "expectedArrivalDate": "2025-01-10",
  "holdingLocationId": 6,
  "notes": "Urgent order",
  "items": [
    {
      "itemId": 10,
      "quantity": 100,
      "unitPrice": 75000,
      "notes": "Color: Red"
    }
  ]
}
```

**Business Rules:**
- Customer harus exist dan active
- Minimal 1 item required
- **CRITICAL - STOCK VALIDATION:**
  - Item harus ada stock di Storage locations (bukan holding)
  - Stock dihitung dari Storage locations ONLY
  - Available stock ≥ quantity ordered
  - Status inventory = "Available"
  - Quantity > 0
- Holding Location harus category "Other"
- Holding Location capacity harus cukup
- Duplicate items tidak allowed (1 item per SO)
- SO Number auto-generated: `SO{YYYYMMDD}{NNN}`
- Status default: "Pending"
- UnitPrice default: Item.StandardPrice (jika tidak diisi)
- Total Amount calculated automatically

**Stock Validation Logic:**
```csharp
// Get total stock from Storage locations only
var totalStock = Sum(Inventory.Quantity) 
                 WHERE ItemId = itemId
                 AND Location.Category = 'Storage'
                 AND Inventory.Status = 'Available'
                 AND Inventory.Quantity > 0
                 AND Inventory.CompanyId = companyId

if (totalStock < requestedQuantity)
    Return Error: "Insufficient stock"
```

**Response:**
```json
{
  "success": true,
  "message": "Sales Order created successfully",
  "data": {
    "id": 1
  }
}
```

**Validation Errors:**
- "Customer not found"
- "At least one item must be provided"
- "Duplicate items are not allowed"
- "Insufficient stock for item PROD-A-001. Available: 50, Required: 100"
- "Item with ID 10 not found"
- "Holding location must be of category 'Other', not 'Storage'"
- "Insufficient capacity in holding location"

### 4. UPDATE Sales Order
**Endpoint:** `PUT /api/salesorder/{id}`
**Permission:** `SO_MANAGE`

**Request Body:**
```json
{
  "expectedArrivalDate": "2025-01-12",
  "holdingLocationId": 7,
  "notes": "Updated notes",
  "items": [
    {
      "itemId": 10,
      "quantity": 120,
      "unitPrice": 75000,
      "notes": "Updated"
    }
  ]
}
```

**Business Rules:**
- Hanya bisa edit jika status = "Pending"
- Jika update items, harus re-validate stock
- Update details akan **REPLACE** semua existing details
- Total Amount di-recalculate

**Response:**
```json
{
  "success": true,
  "message": "Sales Order updated successfully"
}
```

**Validation Errors:**
- "Sales Order cannot be edited in current status"
- "Sales Order not found"
- "Insufficient stock for item PROD-A-001. Available: 50, Required: 120"

### 5. CANCEL Sales Order
**Endpoint:** `PATCH /api/salesorder/{id}/cancel`
**Permission:** `SO_MANAGE`

**Request Body:**
```json
{
  "reason": "Customer cancelled order"
}
```

**Business Rules:**
- Bisa cancel jika status = "Pending" atau "In Progress"
- **Case 1: No Picking** (Status = Pending)
  - Cancel SO only
  - Status → "Cancelled"
  - Reason ditambahkan ke Notes
- **Case 2: Has Picking** (Status = In Progress)
  - Cancel SO dan Picking
  - Picking status harus "Pending"
  - Both → "Cancelled"
  - Reason ditambahkan ke Notes

**Response (Case 1 - No Picking):**
```json
{
  "success": true,
  "message": "Sales Order cancelled successfully",
  "data": {
    "soId": 1,
    "soNumber": "SO20250101001",
    "status": "Cancelled",
    "pickingCancelled": false
  }
}
```

**Response (Case 2 - With Picking):**
```json
{
  "success": true,
  "message": "Sales Order and Picking cancelled successfully",
  "data": {
    "soId": 1,
    "soNumber": "SO20250101001",
    "soStatus": "Cancelled",
    "pickingId": 5,
    "pickingNumber": "PKG20250101001",
    "pickingStatus": "Cancelled",
    "pickingCancelled": true
  }
}
```

**Validation Errors:**
- "Reason is required"
- "Sales Order cannot be cancelled. Current status: Shipped. Only Pending or In Progress status can be cancelled."
- "Sales Order status must be 'In Progress' when Picking exists. Current status: Pending"
- "Sales Order cannot be cancelled because Picking status is In Progress. Only Pending Picking can be cancelled along with Sales Order."

### 6. DELETE Sales Order
**Endpoint:** `DELETE /api/salesorder/{id}`
**Permission:** `SO_MANAGE`

**Business Rules:**
- Hanya bisa delete jika status = "Pending"
- Soft delete (IsDeleted = true)
- Tidak bisa delete jika sudah ada Picking

**Response:**
```json
{
  "success": true,
  "message": "Sales Order deleted successfully"
}
```

**Validation Errors:**
- "Sales Order cannot be deleted in current status"
- "Sales Order not found"

### 7. UPDATE Sales Order STATUS
**Endpoint:** `PATCH /api/salesorder/{id}/status`
**Permission:** `SO_MANAGE`

**Request Body:**
```json
{
  "status": "Shipped"
}
```

**Status Flow:**
1. **Pending** → Create Picking → **In Progress**
2. **In Progress** → Process Picking Complete → **Picked**
3. **Picked** → Ship → **Shipped**

**Special Handling - Status "Shipped":**
- Hanya bisa ship dari status "Picked"
- **CRITICAL - REDUCE INVENTORY:**
  - Untuk setiap SO Detail, reduce inventory dari Holding Location
  - Inventory.Quantity -= SO Detail.Quantity
  - Holding Location CurrentCapacity -= quantity
  - Jika inventory quantity = 0, status = "Empty" atau record dihapus
- Status berubah ke "Shipped"
- Final state (barang sudah di-ship ke customer)

**Inventory Reduction Logic:**
```csharp
foreach (var soDetail in salesOrder.SalesOrderDetails)
{
    // Find inventory in holding location
    var holdingInventory = GetInventory(soDetail.ItemId, holdingLocationId);
    
    // Validate stock
    if (holdingInventory.Quantity < soDetail.Quantity)
        Return Error: "Insufficient stock"
    
    // Reduce inventory
    holdingInventory.Quantity -= soDetail.Quantity;
    holdingLocation.CurrentCapacity -= soDetail.Quantity;
    
    // Update status if empty
    if (holdingInventory.Quantity == 0)
        holdingInventory.Status = "Empty";
}
```

**Response:**
```json
{
  "success": true,
  "message": "Sales Order status updated successfully"
}
```

**Validation Errors:**
- "Sales Order must be in 'Picked' status before it can be shipped"
- "Holding location not set for this Sales Order"
- "No available inventory found for item PROD-A-001 at holding location Shipping Area"
- "Insufficient stock for item PROD-A-001 at holding location. Available: 50, Required: 100"

### 8. GET Dashboard Statistics
**Endpoint:** `GET /api/salesorder/dashboard`
**Permission:** `SO_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalSOs": 100,
    "pendingSOs": 20,
    "inProgressSOs": 15,
    "pickedSOs": 30,
    "shippedSOs": 35
  }
}
```

### 9. GET Customers List
**Endpoint:** `GET /api/salesorder/customers`
**Permission:** `SO_VIEW`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 5,
      "name": "PT Customer XYZ",
      "email": "customer@xyz.com",
      "phone": "081234567890",
      "code": "CUST0001"
    }
  ]
}
```

**Business Rules:**
- Hanya customer yang active
- Ordered by name

### 10. ADVANCED SEARCH Customers
**Endpoint:** `POST /api/salesorder/customers/search`
**Permission:** `SO_VIEW`

**Request Body:**
```json
{
  "name": "Customer",
  "email": "customer",
  "phone": "0812",
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
      "name": "PT Customer XYZ",
      "email": "customer@xyz.com",
      "phone": "081234567890",
      "code": "CUST0001",
      "isActive": true
    }
  ],
  "totalCount": 10,
  "totalPages": 1,
  "currentPage": 1
}
```

### 11. ADVANCED SEARCH Items (with Stock)
**Endpoint:** `POST /api/salesorder/items/advanced-search`
**Permission:** `SO_VIEW`

**Request Body:**
```json
{
  "itemCode": "PROD",
  "name": "Product",
  "unit": "PCS",
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
      "id": 10,
      "itemCode": "PROD-A-001",
      "name": "Product A",
      "unit": "PCS",
      "standardPrice": 75000,
      "totalStock": 500,
      "createdDate": "2025-01-01T10:00:00",
      "isActive": true
    }
  ],
  "totalCount": 20,
  "totalPages": 2,
  "currentPage": 1
}
```

**Business Rules:**
- **CRITICAL:** Hanya menampilkan items yang ada stock di Storage locations
- Total stock dihitung dari Storage locations ONLY (bukan holding)
- Items harus active
- Stock status = "Available"
- Stock quantity > 0

### 12. QUICK SEARCH Items
**Endpoint:** `GET /api/salesorder/items/search?q=PROD`
**Permission:** `SO_VIEW`

**Response:**
```json
[
  {
    "id": 10,
    "itemCode": "PROD-A-001",
    "name": "Product A",
    "unit": "PCS",
    "standardPrice": 75000,
    "purchasePrice": 50000,
    "totalStock": 500
  }
]
```

**Business Rules:**
- Query minimum 2 characters
- Hanya items dengan stock di Storage locations
- Returns top 10 results
- Ordered by relevance

### 13. GET Item Stock Detail
**Endpoint:** `GET /api/salesorder/items/{itemId}/stock`
**Permission:** `SO_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "itemId": 10,
    "totalStock": 500,
    "stockByLocation": [
      {
        "locationId": 3,
        "locationCode": "STG-A-01",
        "locationName": "Storage A-01",
        "quantity": 300
      },
      {
        "locationId": 4,
        "locationCode": "STG-A-02",
        "locationName": "Storage A-02",
        "quantity": 200
      }
    ]
  }
}
```

**Business Rules:**
- Total stock dari Storage locations ONLY
- Stock breakdown by location
- Hanya locations dengan quantity > 0
- Inventory status = "Available"

### 14. GET Holding Locations
**Endpoint:** `GET /api/salesorder/locations`
**Permission:** `SO_VIEW`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 6,
      "name": "Shipping Area",
      "code": "SHIP-001",
      "currentCapacity": 200,
      "maxCapacity": 500,
      "availableCapacity": 300
    }
  ]
}
```

**Business Rules:**
- Hanya location category "Other" (holding location)
- Hanya location yang active
- Display capacity information

---

## WORKFLOW

### Normal Flow:
1. **Create SO** → Status: "Pending" (validasi stock dari Storage)
   - Select customer
   - Select items dengan stock validation
   - Select holding location
   - Stock validated dari Storage locations only
2. **Create Picking** → SO Status: "In Progress"
   - Picking dibuat dari SO
   - Auto-create PickingDetails
   - SO status auto-updated
3. **Process Picking** → Move stock dari Storage → Holding Location
   - Execute picks per item
   - Update QuantityPicked
   - Inventory moved
4. **Picking Complete** → SO Status: "Picked"
   - All items picked
   - Ready for shipping
5. **Ship SO** → Status: "Shipped"
   - Reduce inventory dari Holding Location
   - Final state

### Cancellation Flow:
1. **Cancel SO (Pending)** → Cancel SO only
2. **Cancel SO (In Progress)** → Cancel SO + Cancel Picking

---

## SO STATUS

### Status Values:
- **Pending:** SO baru dibuat, belum ada picking
  - Can: Edit, Delete, Cancel, Create Picking
- **In Progress:** SO sudah ada picking
  - Can: Process Picking, Cancel (with picking cancellation)
  - Cannot: Edit, Delete (tanpa cancel picking)
- **Picked:** Picking complete, barang di holding location
  - Can: Ship
  - Cannot: Edit, Delete, Cancel
- **Shipped:** Barang sudah di-ship ke customer
  - Final state
  - Inventory reduced dari holding
- **Cancelled:** SO dibatalkan
  - Final state
  - Cannot modify

### Status Transitions:
```
Pending → In Progress (via Create Picking)
In Progress → Picked (via Process Picking Complete)
Picked → Shipped (via Ship)
Pending/In Progress → Cancelled (via Cancel)
```

---

## STOCK VALIDATION

### For Create/Update SO:
**Stock Source:** Storage locations ONLY

**Validation Query:**
```sql
SELECT SUM(Inventory.Quantity) as TotalStock
FROM Inventories
WHERE ItemId = @itemId
AND Location.Category = 'Storage'
AND Inventory.Status = 'Available'
AND Inventory.Quantity > 0
AND Inventory.CompanyId = @companyId
```

**Rules:**
- Total stock ≥ quantity ordered
- Stock dari Storage locations only (holding locations excluded)
- Inventory status = "Available"
- Quantity > 0

### Stock Calculation:
```csharp
var totalStock = await _context.Inventories
    .Where(i => i.ItemId == itemId &&
               i.CompanyId == companyId &&
               !i.IsDeleted &&
               i.Status == Constants.INVENTORY_STATUS_AVAILABLE &&
               i.Quantity > 0 &&
               i.Location.Category == Constants.LOCATION_CATEGORY_STORAGE)
    .SumAsync(i => i.Quantity);
```

---

## INVENTORY REDUCTION (Shipping)

### When SO Status = "Shipped":
**For each SO Detail:**
1. Find inventory di Holding Location
2. Validate stock: Available ≥ Required
3. Reduce quantity: Inventory.Quantity -= SO Detail.Quantity
4. Reduce location capacity: CurrentCapacity -= quantity
5. Update status: If quantity = 0, Status = "Empty" or remove record

**Process:**
```csharp
foreach (var soDetail in salesOrder.SalesOrderDetails)
{
    var holdingInventory = GetInventory(soDetail.ItemId, holdingLocationId);
    
    if (holdingInventory.Quantity < soDetail.Quantity)
        Return Error: "Insufficient stock"
    
    holdingInventory.Quantity -= soDetail.Quantity;
    holdingLocation.CurrentCapacity -= soDetail.Quantity;
    
    if (holdingInventory.Quantity == 0)
        holdingInventory.Status = "Empty";
}
```

---

## VALIDATION RULES

1. **Customer Validation:**
   - Customer must exist
   - Customer must be active
   - Customer belongs to same company

2. **Item Validation:**
   - Item must exist and active
   - Item must have stock in Storage locations
   - Available stock ≥ quantity ordered
   - No duplicate items (1 item per SO)

3. **Location Validation:**
   - Holding Location must be category "Other"
   - Holding Location must be active
   - Capacity must be sufficient

4. **Status Validation:**
   - Pending: Can edit, delete, cancel, create picking
   - In Progress: Has picking, can cancel (with picking)
   - Picked: Ready to ship
   - Shipped: Final state, inventory reduced
   - Cancelled: Final state

---

## SO NUMBER GENERATION

**Format:** `SO{YYYYMMDD}{NNN}`

**Example:**
- `SO20250101001` → Date 2025-01-01, Sequence 001
- `SO20250101002` → Date 2025-01-01, Sequence 002

**Logic:**
```csharp
var today = DateTime.Now;
var prefix = $"SO{today:yyyyMMdd}";

var lastSO = GetLatestSO(prefix);
var nextNumber = (lastSO?.Sequence ?? 0) + 1;

return $"{prefix}{nextNumber:D3}";
```

---

## TOTAL AMOUNT CALCULATION

**Formula:**
```csharp
TotalAmount = Sum(Detail.Quantity × Detail.UnitPrice)
```

**Auto-calculation:**
- UnitPrice default: Item.StandardPrice (jika tidak diisi)
- Calculated saat create/update SO Details

---

## ERROR HANDLING

**Common Errors:**
- `400 Bad Request`: Validation failed, insufficient stock
- `404 Not Found`: SO/Customer/Item/Location not found
- `403 Forbidden`: Missing permission
- `500 Internal Server Error`: Database error

**Error Messages:**
- "Insufficient stock for item PROD-A-001. Available: 50, Required: 100"
- "Sales Order must be in 'Picked' status before it can be shipped"
- "No available inventory found for item PROD-A-001 at holding location Shipping Area"
- "Holding location must be of category 'Other'"
- "Duplicate items are not allowed"

---

## BEST PRACTICES

1. **SO Creation:**
   - Verify stock availability sebelum create
   - Check customer information
   - Select appropriate holding location
   - Review total amount

2. **Stock Management:**
   - Monitor stock levels
   - Use stock search untuk items available
   - Check stock by location untuk planning

3. **Status Management:**
   - Create picking setelah SO confirmed
   - Process picking promptly
   - Ship setelah picking complete
   - Monitor status transitions

4. **Cancellation:**
   - Cancel early jika perlu (before picking)
   - Provide clear reason
   - Check picking status sebelum cancel
