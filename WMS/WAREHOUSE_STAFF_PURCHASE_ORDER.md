# PURCHASE ORDER MANAGEMENT - WAREHOUSE STAFF DOCUMENTATION

## OVERVIEW
Warehouse Staff memiliki akses penuh untuk mengelola Purchase Order (PO) termasuk create, update, cancel, dan send via email. PO digunakan untuk memesan barang dari supplier.

## PERMISSIONS REQUIRED
- `PO_VIEW` - View semua Purchase Orders
- `PO_MANAGE` - Create, Update, Cancel, Delete, Send PO

---

## ENDPOINTS

### 1. GET Purchase Order List
**Endpoint:** `GET /api/purchaseorder`
**Permission:** `PO_MANAGE`

**Query Parameters:**
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 10) - Items per page
- `search` (string, optional) - Search by PO Number, Supplier Name, Notes
- `status` (string, optional) - Filter by status (Draft, Sent, Received, Cancelled)
- `supplier` (string, optional) - Filter by supplier name

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "poNumber": "PO20250101001",
      "supplierName": "PT Supplier ABC",
      "supplierEmail": "supplier@abc.com",
      "orderDate": "2025-01-01",
      "expectedDeliveryDate": "2025-01-15",
      "status": "Draft",
      "totalAmount": 5000000,
      "itemCount": 5,
      "notes": "Urgent order",
      "createdDate": "2025-01-01T10:00:00",
      "modifiedDate": null,
      "createdBy": "user1",
      "modifiedBy": null
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 25,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

### 2. GET Purchase Order Detail
**Endpoint:** `GET /api/purchaseorder/{id}`
**Permission:** `PO_MANAGE`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "poNumber": "PO20250101001",
    "supplierId": 5,
    "supplierName": "PT Supplier ABC",
    "supplierEmail": "supplier@abc.com",
    "orderDate": "2025-01-01",
    "expectedDeliveryDate": "2025-01-15",
    "status": "Draft",
    "totalAmount": 5000000,
    "notes": "Urgent order",
    "createdDate": "2025-01-01T10:00:00",
    "createdBy": "user1",
    "details": [
      {
        "id": 1,
        "itemId": 10,
        "itemName": "Product A",
        "itemCode": "PROD-A-001",
        "itemUnit": "PCS",
        "quantity": 100,
        "unitPrice": 50000,
        "totalPrice": 5000000,
        "notes": null
      }
    ]
  }
}
```

### 3. CREATE Purchase Order
**Endpoint:** `POST /api/purchaseorder`
**Permission:** `PO_MANAGE`

**Request Body:**
```json
{
  "supplierId": 5,
  "orderDate": "2025-01-01",
  "expectedDeliveryDate": "2025-01-15",
  "notes": "Urgent order",
  "details": [
    {
      "itemId": 10,
      "quantity": 100,
      "unitPrice": 50000,
      "notes": "Color: Red"
    }
  ]
}
```

**Business Rules:**
- Supplier harus exist dan active
- Minimal 1 item required
- Item harus belong ke supplier yang dipilih
- PO Number auto-generated format: `PO{YYYY}{MM}{NNN}`
  - Example: `PO20250101001` (Year=2025, Month=01, Sequence=001)
- Status default: "Draft"
- Total Amount calculated automatically dari details
- OrderDate default: Today jika tidak diisi

**Response:**
```json
{
  "success": true,
  "message": "Purchase Order created successfully",
  "data": {
    "id": 1,
    "poNumber": "PO20250101001",
    "totalAmount": 5000000
  }
}
```

**Validation Errors:**
- "Supplier is required"
- "Supplier not found"
- "At least one item is required"
- "Item with ID X not found"
- "Item must belong to selected supplier"

### 4. UPDATE Purchase Order
**Endpoint:** `PUT /api/purchaseorder/{id}`
**Permission:** `PO_MANAGE`

**Request Body:**
```json
{
  "supplierId": 5,
  "orderDate": "2025-01-02",
  "expectedDeliveryDate": "2025-01-16",
  "notes": "Updated notes",
  "details": [
    {
      "itemId": 10,
      "quantity": 150,
      "unitPrice": 50000,
      "notes": "Updated quantity"
    }
  ]
}
```

**Business Rules:**
- Hanya bisa di-edit jika status = "Draft"
- Jika status sudah "Sent" atau "Received", tidak bisa di-edit
- Update details akan **REPLACE** semua existing details (tidak merge)
- Total Amount di-recalculate dari details baru

**Response:**
```json
{
  "success": true,
  "message": "Purchase Order updated successfully"
}
```

**Validation Errors:**
- "Purchase Order cannot be edited in current status"
- "Purchase Order not found"
- "Supplier not found"

### 5. CANCEL Purchase Order
**Endpoint:** `PATCH /api/purchaseorder/{id}/cancel`
**Permission:** `PO_MANAGE`

**Request Body:**
```json
{
  "reason": "Customer cancelled order"
}
```

**Business Rules:**
- Hanya bisa cancel jika status = "Draft"
- Tidak bisa cancel jika sudah ada ASN yang aktif (status bukan Cancelled)
- Status berubah ke "Cancelled"
- Reason ditambahkan ke Notes field dengan format:
  ```
  [Existing Notes]
  
  Cancelation reason : [Reason]
  ```
- Max Notes length: 500 characters
- Jika Notes sudah panjang, reason akan di-truncate

**Response:**
```json
{
  "success": true,
  "message": "Purchase Order cancelled successfully",
  "data": {
    "id": 1,
    "poNumber": "PO20250101001",
    "status": "Cancelled"
  }
}
```

**Validation Errors:**
- "Reason is required"
- "Purchase Order cannot be cancelled. Current status: Sent. Only Draft status can be cancelled."
- "Purchase Order cannot be cancelled because it has X active ASN(s). Please cancel the ASN(s) first."

### 6. DELETE Purchase Order
**Endpoint:** `DELETE /api/purchaseorder/{id}`
**Permission:** `PO_MANAGE`

**Business Rules:**
- Hanya bisa delete jika status = "Draft"
- Soft delete (IsDeleted = true, bukan hard delete)
- Tidak bisa delete jika sudah ada ASN

**Response:**
```json
{
  "success": true,
  "message": "Purchase Order deleted successfully"
}
```

**Validation Errors:**
- "Purchase Order cannot be deleted in current status"
- "Purchase Order not found"

### 7. SEND Purchase Order
**Endpoint:** `POST /api/purchaseorder/{id}/send`
**Permission:** `PO_MANAGE`

**Request:** No body required

**Business Rules:**
- Hanya bisa send jika status = "Draft"
- Supplier email harus valid (tidak kosong)
- Email dikirim ke supplier dengan format HTML
- Email content includes:
  - PO Number, Order Date, Expected Delivery
  - Company name
  - Item list dengan details (Code, Name, Quantity, Price)
  - Total Amount
  - Notes (jika ada)
- Status berubah ke "Sent" setelah email terkirim successfully
- Email sent status tracked (optional feature)

**Email Format:**
```html
<h2>Purchase Order</h2>
<p>Dear {SupplierName},</p>
<p>Please find below our purchase order details:</p>
<table>
  <tr><td>PO Number:</td><td>{PONumber}</td></tr>
  <tr><td>Order Date:</td><td>{OrderDate}</td></tr>
  <tr><td>Expected Delivery:</td><td>{ExpectedDeliveryDate}</td></tr>
</table>
<h3>Items:</h3>
<table>
  <tr><th>Item Code</th><th>Name</th><th>Quantity</th><th>Price</th><th>Total</th></tr>
  <!-- Item rows -->
</table>
<p><strong>Total Amount: {TotalAmount}</strong></p>
```

**Response:**
```json
{
  "success": true,
  "message": "Purchase Order sent successfully to supplier@abc.com"
}
```

**Validation Errors:**
- "Purchase Order cannot be sent in current status"
- "Supplier email address is missing"
- "Failed to send email" (email service error)

### 8. GET Dashboard Statistics
**Endpoint:** `GET /api/purchaseorder/dashboard`
**Permission:** `PO_MANAGE`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalPurchaseOrders": 100,
    "draftOrders": 10,
    "sentOrders": 30,
    "receivedOrders": 50,
    "cancelledOrders": 10,
    "totalValue": 500000000,
    "averageOrderValue": 5000000
  }
}
```

**Statistics:**
- `totalPurchaseOrders`: Total PO (all status)
- `draftOrders`: Status = "Draft"
- `sentOrders`: Status = "Sent"
- `receivedOrders`: Status = "Received"
- `cancelledOrders`: Status = "Cancelled"
- `totalValue`: Sum of TotalAmount (all PO)
- `averageOrderValue`: Average TotalAmount

### 9. GET Suppliers List
**Endpoint:** `GET /api/purchaseorder/suppliers`
**Permission:** `PO_MANAGE`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 5,
      "name": "PT Supplier ABC",
      "email": "supplier@abc.com",
      "phone": "081234567890",
      "address": "Jakarta"
    }
  ]
}
```

**Business Rules:**
- Hanya supplier yang active
- Ordered by name
- Tidak include deleted suppliers

### 10. GET Items List
**Endpoint:** `GET /api/purchaseorder/items?supplierId=5`
**Permission:** `PO_MANAGE`

**Query Parameters:**
- `supplierId` (int, optional) - Filter items by supplier

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 10,
      "name": "Product A",
      "code": "PROD-A-001",
      "unit": "PCS",
      "purchasePrice": 45000,
      "description": "Product description",
      "supplierId": 5
    }
  ]
}
```

**Business Rules:**
- Jika supplierId provided, hanya items dari supplier tersebut
- Items harus active
- Ordered by name

### 11. ADVANCED SEARCH Suppliers
**Endpoint:** `POST /api/purchaseorder/suppliers/advanced-search`
**Permission:** `SUPPLIER_VIEW`

**Request Body:**
```json
{
  "name": "ABC",
  "email": "supplier",
  "phone": "0812",
  "city": "Jakarta",
  "contactPerson": "John",
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
      "name": "PT Supplier ABC",
      "email": "supplier@abc.com",
      "phone": "081234567890",
      "city": "Jakarta",
      "contactPerson": "John Doe",
      "createdDate": "2025-01-01T10:00:00",
      "isActive": true
    }
  ],
  "totalCount": 10,
  "totalPages": 1,
  "currentPage": 1
}
```

### 12. ADVANCED SEARCH Items
**Endpoint:** `POST /api/purchaseorder/items/advanced-search`
**Permission:** `ITEM_VIEW`

**Request Body:**
```json
{
  "supplierId": 5,
  "itemCode": "PROD",
  "name": "Product",
  "unit": "PCS",
  "createdDateFrom": "2025-01-01",
  "createdDateTo": "2025-01-31",
  "page": 1,
  "pageSize": 10
}
```

**Business Rules:**
- `supplierId` adalah **WAJIB** (required)
- Items harus active
- Filtered by supplier items only

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
      "supplierId": 5,
      "supplierName": "PT Supplier ABC",
      "createdDate": "2025-01-01T10:00:00",
      "isActive": true
    }
  ],
  "totalCount": 20,
  "totalPages": 2,
  "currentPage": 1
}
```

### 13. QUICK SEARCH Purchase Orders
**Endpoint:** `GET /api/purchaseorder/quick-search?q=PO2025`
**Permission:** `PO_MANAGE`

**Response:**
```json
[
  {
    "id": 1,
    "poNumber": "PO20250101001",
    "supplierName": "PT Supplier ABC",
    "orderDate": "2025-01-01",
    "status": "Draft",
    "totalAmount": 5000000
  }
]
```

### 14. QUICK SEARCH Suppliers
**Endpoint:** `GET /api/purchaseorder/suppliers/quick-search?q=ABC`
**Permission:** `PO_MANAGE`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 5,
      "name": "PT Supplier ABC",
      "code": "SUP0001",
      "email": "supplier@abc.com",
      "phone": "081234567890",
      "contactPerson": "John Doe"
    }
  ]
}
```

### 15. QUICK SEARCH Items
**Endpoint:** `GET /api/purchaseorder/items/quick-search?q=PROD&supplierId=5`
**Permission:** `PO_MANAGE`

**Response:**
```json
[
  {
    "id": 10,
    "name": "Product A",
    "itemCode": "PROD-A-001",
    "unit": "PCS",
    "purchasePrice": 45000,
    "supplierName": "PT Supplier ABC",
    "description": "Product description"
  }
]
```

---

## WORKFLOW

### Normal Flow:
1. **Create PO** → Status: "Draft"
   - Select supplier
   - Add items (from supplier's items)
   - Review total amount
   - Save as Draft
2. **Send PO** → Status: "Sent"
   - Send email ke supplier
   - Status berubah ke "Sent"
3. **Supplier kirim barang** → Create ASN
   - ASN dibuat dari PO
   - **Auto-update:** PO Status → "Received"
4. **Process ASN** → Status: Arrived → Processed
   - Barang di-putaway ke Storage

### Cancellation Flow:
1. **Cancel PO** (Draft only) → Status: "Cancelled"
   - Check: No active ASN exists
   - Add cancellation reason to Notes
   - Final state (cannot modify)

---

## PO STATUS

### Status Values:
- **Draft:** PO baru dibuat, belum di-send
  - Can: Edit, Delete, Cancel, Send
- **Sent:** PO sudah dikirim ke supplier via email
  - Can: Create ASN, View
  - Cannot: Edit, Delete, Cancel (kalau sudah ada ASN)
- **Received:** PO sudah memiliki ASN
  - Can: View, Create more ASN (if needed)
  - Cannot: Edit, Delete, Cancel (kalau ada ASN aktif)
- **Cancelled:** PO dibatalkan
  - Final state, cannot modify

### Status Transitions:
```
Draft → Sent (via Send)
Sent → Received (via Create ASN)
Draft → Cancelled (via Cancel)
```

---

## VALIDATION RULES

1. **Supplier Validation:**
   - Supplier must exist
   - Supplier must be active
   - Supplier must belong to same company

2. **Item Validation:**
   - Item must exist
   - Item must belong to same company
   - Item must belong to selected supplier (CRITICAL!)
   - Item must be active
   - Quantity > 0
   - UnitPrice > 0

3. **Status Validation:**
   - **Draft:** Can edit, delete, cancel, send
   - **Sent:** Can create ASN, cannot edit/delete (jika sudah ada ASN)
   - **Received:** Cannot edit/delete (sudah ada ASN)
   - **Cancelled:** Final state, cannot modify

4. **ASN Validation (for Cancel):**
   - Tidak bisa cancel jika ada ASN aktif
   - ASN aktif = status bukan "Cancelled" dan tidak deleted

---

## PO NUMBER GENERATION

**Format:** `PO{YYYY}{MM}{NNN}`

**Example:**
- `PO20250101001` → Tahun 2025, Bulan 01, Sequence 001
- `PO20250101002` → Tahun 2025, Bulan 01, Sequence 002

**Logic:**
```csharp
var today = DateTime.Today;
var year = today.Year;
var month = today.Month;
var prefix = $"PO{year}{month:D2}";

// Get last PO number for this month
var lastPO = GetLatestPO(prefix);
var nextNumber = (lastPO?.Sequence ?? 0) + 1;

return $"{prefix}{nextNumber:D3}";
```

---

## TOTAL AMOUNT CALCULATION

**Formula:**
```csharp
TotalAmount = Sum(Detail.Quantity × Detail.UnitPrice)
```

**Auto-calculation:**
- Saat create PO details
- Saat update PO details
- Calculated automatically, tidak perlu input manual

---

## EMAIL INTEGRATION

**Email Sending:**
- Email dikirim via `IEmailService`
- Subject: `Purchase Order {PONumber} from {CompanyName}`
- Format: HTML
- Includes: PO details, item list, total amount, notes

**Email Failure:**
- Jika email gagal, PO tetap dalam status "Draft"
- User bisa retry send
- Error message: "Failed to send email"

---

## ERROR HANDLING

**Common Errors:**
- `400 Bad Request`: Invalid data, validation failed
- `401 Unauthorized`: No company context
- `403 Forbidden`: Missing permission
- `404 Not Found`: PO/Supplier/Item not found
- `500 Internal Server Error`: Server error

**Error Response Format:**
```json
{
  "success": false,
  "message": "Error message here"
}
```

**Validation Error Format:**
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": {
    "supplierId": ["Supplier is required"],
    "details": ["At least one item is required"]
  }
}
```

---

## BEST PRACTICES

1. **PO Creation:**
   - Verify supplier sebelum create
   - Check items belong to supplier
   - Review total amount sebelum send

2. **Email Sending:**
   - Verify supplier email valid
   - Review PO details sebelum send
   - Keep draft jika perlu review

3. **Cancellation:**
   - Check ASN status sebelum cancel
   - Provide clear cancellation reason
   - Cancel ASN terlebih dahulu jika ada

4. **Status Management:**
   - Monitor PO status changes
   - Track ASN creation from PO
   - Use dashboard untuk overview
