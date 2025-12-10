# WAREHOUSE STAFF - COMPLETE FEATURE INDEX

## OVERVIEW
Dokumentasi lengkap untuk semua fitur yang dapat diakses oleh Warehouse Staff role. Setiap fitur memiliki dokumentasi detail terpisah.

## ROLE PERMISSIONS

Warehouse Staff memiliki permissions berikut:
```
ITEM_VIEW, LOCATION_VIEW, CUSTOMER_VIEW, SUPPLIER_VIEW,
PO_VIEW, PO_MANAGE,
ASN_VIEW, ASN_MANAGE,
SO_VIEW, SO_MANAGE,
PICKING_VIEW, PICKING_MANAGE, PICKING_UPDATE,
PUTAWAY_MANAGE,
INVENTORY_VIEW, INVENTORY_MANAGE,
REPORT_VIEW,
AUDIT_VIEW
```

---

## DOCUMENTATION FILES

### 1. PURCHASE ORDER MANAGEMENT
**File:** `WAREHOUSE_STAFF_PURCHASE_ORDER.md`
**Permissions:** `PO_VIEW`, `PO_MANAGE`

**Features:**
- Create Purchase Order
- Update Purchase Order (Draft only)
- Cancel Purchase Order
- Delete Purchase Order (Draft only)
- Send PO via Email
- View PO List & Details
- Search & Filter PO
- Dashboard Statistics

---

### 2. ASN (ADVANCED SHIPPING NOTICE) MANAGEMENT
**File:** `WAREHOUSE_STAFF_ASN.md`
**Permissions:** `ASN_VIEW`, `ASN_MANAGE`

**Features:**
- Create ASN from Purchase Order
- Update ASN details
- Update ASN Status (Pending → On Delivery → Arrived → Processed)
- Cancel ASN (Pending only)
- Auto-create inventory when ASN Arrived
- View ASN List & Details
- Search & Filter ASN
- Dashboard Statistics
- Get Holding Locations

---

### 3. SALES ORDER MANAGEMENT
**File:** `WAREHOUSE_STAFF_SALES_ORDER.md`
**Permissions:** `SO_VIEW`, `SO_MANAGE`

**Features:**
- Create Sales Order (with stock validation)
- Update Sales Order (Pending only)
- Cancel Sales Order (with Picking cancellation if exists)
- Delete Sales Order (Pending only)
- Update SO Status (including Ship operation)
- View SO List & Details
- Search & Filter SO
- Dashboard Statistics
- Get Customers List
- Get Items with Stock (Storage locations only)
- Get Holding Locations

---

### 4. PICKING MANAGEMENT
**File:** `WAREHOUSE_STAFF_PICKING.md`
**Permissions:** `PICKING_VIEW`, `PICKING_MANAGE`, `PICKING_UPDATE`

**Features:**
- Create Picking from Sales Order
- Process Picking (execute picks: Storage → Holding)
- View Picking List & Details
- Get Available Locations for Item
- Search & Filter Picking
- Auto-update SO Status when Picking Complete
- FIFO Location Distribution
- Inventory Movement Tracking

---

### 5. PUTAWAY MANAGEMENT
**File:** `WAREHOUSE_STAFF_PUTAWAY.md`
**Permissions:** `INVENTORY_VIEW`, `PUTAWAY_MANAGE`

**Features:**
- View Putaway Dashboard
- View ASNs Ready for Putaway
- Process Putaway (Holding → Storage)
- View Putaway Details
- Partial Putaway Support
- Capacity Management
- Completion Tracking

---

### 6. INVENTORY MANAGEMENT
**File:** `WAREHOUSE_STAFF_INVENTORY.md`
**Permissions:** `INVENTORY_VIEW`, `INVENTORY_MANAGE`

**Features:**
- View Inventory List & Details
- Create Inventory Record
- Update Inventory (quantity, status)
- Delete Inventory (soft delete)
- Adjust Inventory (Add/Subtract)
- View Inventory by Location
- Dashboard Statistics
- Low Stock Monitoring

---

### 7. MASTER DATA VIEW (READ ONLY)
**File:** `WAREHOUSE_STAFF_MASTER_DATA_VIEW.md`
**Permissions:** `ITEM_VIEW`, `LOCATION_VIEW`, `CUSTOMER_VIEW`, `SUPPLIER_VIEW`

**Features:**
- View Items (with stock information)
- View Locations (with capacity information)
- View Customers
- View Suppliers
- Search & Filter all master data
- Dashboard Statistics for each entity
- Reference data untuk operations

---

### 8. AUDIT TRAIL VIEW
**File:** `WAREHOUSE_STAFF_AUDIT_TRAIL.md`
**Permissions:** `AUDIT_VIEW`

**Features:**
- View Audit Logs
- Search & Filter Audit Logs
- View Audit Statistics
- View User Recent Activities
- Track Entity History
- Monitor System Activities

---

## MENU NAVIGATION

Berdasarkan `_Layout.cshtml`, Warehouse Staff dapat mengakses:

### Inbound Operations:
1. **Purchase Orders** → `/PurchaseOrder`
2. **ASN (Receiving)** → `/ASN`
3. **Putaway** → `/Inventory/Putaway`

### Outbound Operations:
4. **Sales Orders** → `/SalesOrder`
5. **Picking** → `/Picking`

### Inventory Management:
6. **Stock Management** → `/Inventory`

### Audit:
7. **Audit Trail** → `/AuditTrail`

### Master Data (View Only):
- Item → `/Item`
- Location → `/Location`
- Customer → `/Customer`
- Supplier → `/Supplier`

---

## WORKFLOW DIAGRAMS

### Inbound Flow (Receiving):
```
1. Create PO → Status: Draft
2. Send PO → Status: Sent (email to supplier)
3. Supplier ships → Create ASN → PO Status: Received
4. ASN Status: Arrived → Auto-create inventory at Holding Location
5. Process Putaway → Move from Holding → Storage Location
6. ASN Status: Processed (all items putaway)
```

### Outbound Flow (Shipping):
```
1. Create SO → Status: Pending (validate stock)
2. Create Picking → SO Status: In Progress
3. Process Picking → Move from Storage → Holding Location
4. Picking Complete → SO Status: Picked
5. Ship SO → Status: Shipped (reduce inventory from Holding)
```

---

## FEATURE COMPARISON

### FULL MANAGE vs VIEW ONLY

**FULL MANAGE:**
- Purchase Order
- ASN
- Sales Order
- Picking
- Putaway
- Inventory

**VIEW ONLY:**
- Item
- Location
- Customer
- Supplier
- Audit Trail
- Reports (VIEW only, cannot CREATE)

---

## COMMON OPERATIONS

### Create Purchase Order:
1. Select Supplier → `/api/purchaseorder/suppliers`
2. Select Items → `/api/purchaseorder/items?supplierId=X`
3. Create PO → `POST /api/purchaseorder`

### Create Sales Order:
1. Select Customer → `/api/salesorder/customers`
2. Select Items with Stock → `/api/salesorder/items/search?q=...`
3. Check Stock → `/api/salesorder/items/{itemId}/stock`
4. Select Holding Location → `/api/salesorder/locations`
5. Create SO → `POST /api/salesorder`

### Process Putaway:
1. View ASNs → `GET /api/Putaway`
2. Select ASN → `GET /Inventory/ProcessPutaway?asnId=X`
3. Select Storage Location
4. Process → `POST /Inventory/ProcessPutaway`

### Process Picking:
1. View SO → `GET /api/salesorder`
2. Create Picking → `POST /api/picking`
3. Get Locations → `GET /api/picking/locations/{itemId}`
4. Process → `POST /api/picking/{id}/process`

---

## VALIDATION SUMMARY

### Stock Validation:
- **Sales Order:** Stock dari Storage locations only
- **Picking:** Stock dari Storage locations only
- **ASN:** Auto-create inventory at Holding Location
- **Putaway:** Move from Holding to Storage

### Location Validation:
- **Holding Location:** Category = "Other" (for ASN, SO)
- **Storage Location:** Category = "Storage" (for Putaway, Picking)
- **Capacity:** Check before operations

### Status Validation:
- **PO:** Draft → Sent → Received (via ASN)
- **ASN:** Pending → On Delivery → Arrived → Processed
- **SO:** Pending → In Progress → Picked → Shipped
- **Picking:** Pending → In Progress → Completed

---

## ERROR HANDLING SUMMARY

**Common HTTP Status:**
- `200 OK`: Success
- `400 Bad Request`: Validation failed
- `401 Unauthorized`: No company context
- `403 Forbidden`: Missing permission
- `404 Not Found`: Entity not found
- `500 Internal Server Error`: Server error

**Validation Patterns:**
- Check entity exists
- Check status allows operation
- Check permissions
- Check business rules (capacity, stock, etc.)

---

## QUICK REFERENCE

### Create Operations:
- `POST /api/purchaseorder` - Create PO
- `POST /api/asn` - Create ASN
- `POST /api/salesorder` - Create SO
- `POST /api/picking` - Create Picking
- `POST /Inventory/ProcessPutaway` - Process Putaway

### Update Operations:
- `PUT /api/purchaseorder/{id}` - Update PO
- `PUT /api/asn/{id}` - Update ASN
- `PUT /api/salesorder/{id}` - Update SO
- `POST /api/picking/{id}/process` - Process Picking

### Status Updates:
- `PATCH /api/asn/{id}/status` - Update ASN Status
- `PATCH /api/salesorder/{id}/status` - Update SO Status

### Cancel Operations:
- `PATCH /api/purchaseorder/{id}/cancel` - Cancel PO
- `PATCH /api/asn/{id}/cancel` - Cancel ASN
- `PATCH /api/salesorder/{id}/cancel` - Cancel SO

---

## RESTRICTIONS SUMMARY

### Cannot Access:
- ❌ Create/Update/Delete Items
- ❌ Create/Update/Delete Locations
- ❌ Create/Update/Delete Customers
- ❌ Create/Update/Delete Suppliers
- ❌ User Management
- ❌ Company Management
- ❌ Generate/Create Reports

### Can Access:
- ✅ All operational workflows (PO, ASN, SO, Picking, Putaway)
- ✅ Inventory Management
- ✅ View all master data
- ✅ View Audit Trail
- ✅ View Reports (read only)

---

## SUPPORT & MAINTENANCE

Untuk pertanyaan atau issues terkait fitur Warehouse Staff:
1. Check dokumentasi detail untuk fitur terkait
2. Review error messages dari API responses
3. Check Audit Trail untuk tracking actions
4. Contact Admin untuk permission issues

---

## VERSION HISTORY

- **v1.0** (2025-01-15): Initial documentation
  - Complete feature documentation
  - All endpoints documented
  - Workflows explained
  - Validation rules included
