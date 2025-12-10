# MASTER DATA VIEW - WAREHOUSE STAFF DOCUMENTATION

## OVERVIEW
Warehouse Staff memiliki akses VIEW ONLY untuk master data (Item, Location, Customer, Supplier). Tidak bisa create, update, atau delete master data. Akses ini digunakan untuk referensi saat melakukan operasional (create PO, SO, dll).

## PERMISSIONS REQUIRED
- `ITEM_VIEW` - View Items
- `LOCATION_VIEW` - View Locations
- `CUSTOMER_VIEW` - View Customers
- `SUPPLIER_VIEW` - View Suppliers

---

## ITEM MANAGEMENT (VIEW ONLY)

### 1. GET Item List
**Endpoint:** `GET /api/item`
**Permission:** `ITEM_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `search` (string, optional) - Search by Item Code, Name, Description
- `supplierId` (int, optional) - Filter by supplier
- `isActive` (bool, optional) - Filter by active status

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 10,
      "itemCode": "PROD-A-001",
      "name": "Product A",
      "description": "Product description",
      "unit": "PCS",
      "purchasePrice": 45000,
      "standardPrice": 75000,
      "supplierId": 5,
      "supplierName": "PT Supplier ABC",
      "isActive": true,
      "createdDate": "2025-01-01T10:00:00",
      "totalStock": 500,
      "totalValue": 37500000,
      "profitMargin": 30000,
      "profitMarginPercentage": 66.67
    }
  ],
  "totalCount": 100,
  "totalPages": 10,
  "currentPage": 1
}
```

**Additional Fields:**
- `totalStock`: Total inventory quantity across all locations
- `totalValue`: StandardPrice × totalStock
- `profitMargin`: StandardPrice - PurchasePrice
- `profitMarginPercentage`: (ProfitMargin / PurchasePrice) × 100

### 2. GET Item Detail
**Endpoint:** `GET /api/item/{id}`
**Permission:** `ITEM_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 10,
    "itemCode": "PROD-A-001",
    "name": "Product A",
    "description": "Product description",
    "unit": "PCS",
    "purchasePrice": 45000,
    "standardPrice": 75000,
    "supplierId": 5,
    "supplierName": "PT Supplier ABC",
    "isActive": true,
    "createdDate": "2025-01-01T10:00:00",
    "totalStock": 500,
    "totalValue": 37500000,
    "profitMargin": 30000,
    "profitMarginPercentage": 66.67
  }
}
```

### 3. GET Item Dashboard
**Endpoint:** `GET /api/item/dashboard`
**Permission:** `ITEM_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalItems": 500,
    "activeItems": 480,
    "inactiveItems": 20,
    "totalSuppliers": 25,
    "totalValue": 500000000,
    "lowStockItems": 15
  }
}
```

**Statistics:**
- `totalItems`: Total items
- `activeItems`: Active items count
- `inactiveItems`: Inactive items count
- `totalSuppliers`: Distinct suppliers
- `totalValue`: Total inventory value
- `lowStockItems`: Items dengan stock ≤ 10

### 4. GET Suppliers List (for Item selection)
**Endpoint:** `GET /Item/GetSuppliers?search=ABC&limit=20`
**Permission:** `SUPPLIER_VIEW`

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

**Restrictions:**
- ❌ Cannot create/update/delete items
- ❌ Cannot toggle item status
- ✅ Can view for reference in operations

---

## LOCATION MANAGEMENT (VIEW ONLY)

### 1. GET Location List
**Endpoint:** `GET /api/location`
**Permission:** `LOCATION_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `search` (string, optional) - Search by Code, Name, Description
- `status` (string, optional) - active, inactive, full, near-full
- `capacity` (string, optional) - empty, low, medium, high, full
- `category` (string, optional) - Storage, Other

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 3,
        "code": "STG-A-01",
        "name": "Storage A-01",
        "description": "Main storage area",
        "category": "Storage",
        "maxCapacity": 1000,
        "currentCapacity": 500,
        "availableCapacity": 500,
        "isFull": false,
        "isActive": true,
        "capacityPercentage": 50,
        "capacityStatus": "IN USE",
        "createdDate": "2025-01-01T10:00:00"
      }
    ],
    "totalCount": 50,
    "totalPages": 5,
    "currentPage": 1,
    "pageSize": 10
  }
}
```

**Capacity Status:**
- `FULL`: CurrentCapacity >= MaxCapacity
- `NEAR FULL`: CurrentCapacity >= MaxCapacity × 0.8
- `IN USE`: CurrentCapacity > 0
- `AVAILABLE`: CurrentCapacity = 0

### 2. GET Location Detail
**Endpoint:** `GET /api/location/{id}`
**Permission:** `LOCATION_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 3,
    "code": "STG-A-01",
    "name": "Storage A-01",
    "description": "Main storage area",
    "category": "Storage",
    "maxCapacity": 1000,
    "currentCapacity": 500,
    "availableCapacity": 500,
    "isFull": false,
    "isActive": true,
    "capacityPercentage": 50,
    "capacityStatus": "IN USE",
    "createdDate": "2025-01-01T10:00:00",
    "createdBy": "admin"
  }
}
```

### 3. GET Location Inventory
**Endpoint:** `GET /api/location/{id}/inventory`
**Permission:** `LOCATION_VIEW`

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

### 4. GET Location Dashboard
**Endpoint:** `GET /api/location/dashboard`
**Permission:** `LOCATION_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalLocations": 50,
    "activeLocations": 45,
    "inactiveLocations": 5,
    "nearFullLocations": 3,
    "fullLocations": 2,
    "emptyLocations": 10
  }
}
```

**Restrictions:**
- ❌ Cannot create/update/delete locations
- ❌ Cannot toggle location status
- ❌ Cannot update capacity
- ✅ Can view for reference in operations
- ✅ Can view inventory in location

---

## CUSTOMER MANAGEMENT (VIEW ONLY)

### 1. GET Customer List
**Endpoint:** `GET /api/customer`
**Permission:** `CUSTOMER_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `search` (string, optional) - Search by Name, Email, Code, Phone, Address
- `status` (string, optional) - active, inactive
- `type` (string, optional) - Customer type filter

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 5,
      "code": "CUST0001",
      "name": "PT Customer XYZ",
      "email": "customer@xyz.com",
      "phone": "081234567890",
      "address": "Jakarta",
      "city": "Jakarta",
      "customerType": "Corporate",
      "isActive": true,
      "totalOrders": 25,
      "totalValue": 100000000,
      "createdDate": "2025-01-01T10:00:00"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 100,
    "totalPages": 10,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

**Additional Fields:**
- `totalOrders`: Count of Sales Orders
- `totalValue`: Sum of Sales Order TotalAmount

### 2. GET Customer Detail
**Endpoint:** `GET /api/customer/{id}`
**Permission:** `CUSTOMER_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 5,
    "code": "CUST0001",
    "name": "PT Customer XYZ",
    "email": "customer@xyz.com",
    "phone": "081234567890",
    "address": "Jakarta",
    "city": "Jakarta",
    "customerType": "Corporate",
    "isActive": true,
    "totalOrders": 25,
    "totalValue": 100000000,
    "createdDate": "2025-01-01T10:00:00",
    "createdBy": "admin"
  }
}
```

### 3. GET Customer Dashboard
**Endpoint:** `GET /api/customer/dashboard`
**Permission:** `CUSTOMER_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalCustomers": 100,
    "activeCustomers": 90,
    "inactiveCustomers": 10,
    "customersWithOrders": 75,
    "newCustomersThisMonth": 5,
    "topCustomerType": "Corporate"
  }
}
```

**Restrictions:**
- ❌ Cannot create/update/delete customers
- ✅ Can view for reference in Sales Order operations

---

## SUPPLIER MANAGEMENT (VIEW ONLY)

### 1. GET Supplier List
**Endpoint:** `GET /api/supplier`
**Permission:** `SUPPLIER_VIEW`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10)
- `search` (string, optional) - Search by Name, Email
- `isActive` (bool, optional) - Filter by active status

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 5,
      "code": "SUP0001",
      "name": "PT Supplier ABC",
      "email": "supplier@abc.com",
      "phone": "081234567890",
      "contactPerson": "John Doe",
      "address": "Jakarta",
      "city": "Jakarta",
      "isActive": true,
      "createdDate": "2025-01-01T10:00:00",
      "purchaseOrderCount": 50
    }
  ],
  "totalCount": 25,
  "totalPages": 3,
  "currentPage": 1
}
```

**Additional Fields:**
- `purchaseOrderCount`: Count of Purchase Orders

### 2. GET Supplier Detail
**Endpoint:** `GET /api/supplier/{id}`
**Permission:** `SUPPLIER_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 5,
    "code": "SUP0001",
    "name": "PT Supplier ABC",
    "email": "supplier@abc.com",
    "phone": "081234567890",
    "contactPerson": "John Doe",
    "address": "Jakarta",
    "city": "Jakarta",
    "isActive": true,
    "createdDate": "2025-01-01T10:00:00",
    "modifiedDate": null,
    "createdBy": "admin",
    "modifiedBy": null
  }
}
```

### 3. GET Supplier Dashboard
**Endpoint:** `GET /api/supplier/dashboard`
**Permission:** `SUPPLIER_VIEW`

**Response:**
```json
{
  "success": true,
  "data": {
    "totalSuppliers": 25,
    "activeSuppliers": 20,
    "suppliersWithOrders": 18,
    "inactiveSuppliers": 5
  }
}
```

### 4. SEARCH Suppliers (for PO dropdown)
**Endpoint:** `GET /api/supplier/search?search=ABC&limit=20`
**Permission:** `SUPPLIER_VIEW`

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

**Restrictions:**
- ❌ Cannot create/update/delete suppliers
- ✅ Can view for reference in Purchase Order operations

---

## USE CASES

### For Purchase Order:
- View suppliers → Select supplier untuk PO
- View items → Select items dari supplier
- View locations → Select holding location (optional)

### For Sales Order:
- View customers → Select customer untuk SO
- View items with stock → Select items untuk SO
- View locations → Select holding location

### For Picking:
- View locations → Select source location (Storage)
- View items → Verify item untuk picking

### For Putaway:
- View locations → Select target location (Storage)
- View ASN items → Verify items untuk putaway

---

## SEARCH FUNCTIONALITY

All master data endpoints support search:
- **Item:** Search by Item Code, Name, Description
- **Location:** Search by Code, Name, Description
- **Customer:** Search by Name, Email, Code, Phone, Address
- **Supplier:** Search by Name, Email

**Search Implementation:**
- Case-insensitive
- Partial match (contains)
- Multiple fields searched simultaneously

---

## FILTERING OPTIONS

### Item Filters:
- `supplierId`: Filter by supplier
- `isActive`: Filter by active status
- `search`: General search

### Location Filters:
- `status`: active, inactive, full, near-full
- `capacity`: empty, low, medium, high, full
- `category`: Storage, Other
- `search`: General search

### Customer Filters:
- `status`: active, inactive
- `type`: Customer type
- `search`: General search

### Supplier Filters:
- `isActive`: Filter by active status
- `search`: General search

---

## ERROR HANDLING

**Common Errors:**
- `404 Not Found`: Item/Location/Customer/Supplier not found
- `403 Forbidden`: Missing permission
- `500 Internal Server Error`: Server error

**Error Messages:**
- "Item not found"
- "Location not found"
- "Customer not found"
- "Supplier not found"

---

## BEST PRACTICES

1. **Reference Usage:**
   - Use master data sebagai reference only
   - Tidak perlu edit, hanya view untuk operasional

2. **Search Efficiency:**
   - Use search parameters untuk filter data
   - Use pagination untuk large datasets

3. **Data Accuracy:**
   - Verify data sebelum digunakan dalam operations
   - Check active status sebelum select

4. **Dashboard Monitoring:**
   - Monitor statistics untuk insight
   - Check low stock items
   - Monitor location capacity
