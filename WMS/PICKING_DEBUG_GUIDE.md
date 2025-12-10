# Picking Debug Guide

## Overview
Dokumentasi ini menjelaskan cara menggunakan debugging tools yang telah ditambahkan untuk menganalisis bug picking quantity.

## Debugging Features

### 1. Backend Logging
- **Comprehensive logging** di `ProcessPickingItem` method
- **Database transaction logging** untuk tracking perubahan inventory
- **Detailed error logging** dengan context yang lengkap

### 2. Frontend Logging
- **Console logging** di `process-picking-manager.js`
- **Request/Response tracking** untuk debugging data flow
- **Item data validation** sebelum mengirim request

### 3. Debug Endpoints
- **`/api/picking/debug/inventory/{itemId}`** - Debug inventory state per item
- **`/api/picking/debug/picking/{pickingId}`** - Debug picking state dan details
- **`/picking/debug-test`** - Debug test page untuk monitoring

## Cara Menggunakan Debugging

### Step 1: Setup Logging
1. Pastikan logging level di `appsettings.json` set ke `Information` atau `Debug`
2. Monitor console output atau log files saat melakukan picking

### Step 2: Test Scenario
1. **Buat Sales Order** dengan 2 item:
   - Item 1: qty 3
   - Item 2: qty 2

2. **Lakukan Picking** satu per satu sambil monitor:
   - **Console logs** di browser (F12 → Console)
   - **Application logs** di server
   - **Database changes** via debug endpoints

### Step 3: Monitor Logs

#### Backend Logs (Server Console/Log Files)
```
=== PICKING DEBUG START ===
Request Details - PickingId: 123, PickingDetailId: 456, SourceLocationId: 789, QuantityToPick: 3
PickingDetail Found - ItemId: 1, ItemCode: ITM001, QuantityRequired: 3, QuantityPicked: 0, RemainingQuantity: 3
Source Inventory BEFORE - ItemId: 1, LocationId: 789, CurrentQuantity: 10, WillReduceBy: 3
PickingDetail AFTER UPDATE - QuantityPicked: 3, RemainingQuantity: 0, Status: Picked
Source Inventory AFTER - ItemId: 1, LocationId: 789, NewQuantity: 7
Holding Inventory BEFORE ADD - ItemId: 1, LocationId: 999, CurrentQuantity: 0, WillAdd: 3
Holding Inventory AFTER ADD - ItemId: 1, LocationId: 999, NewQuantity: 3
=== PICKING DEBUG END - SUCCESS ===
```

#### Frontend Logs (Browser Console)
```
=== FRONTEND PICKING DEBUG START ===
PickingDetailId: 456
SourceLocationId: 789
QuantityToPick: 3
Current PickingId: 123
Item Data: {itemId: 1, itemCode: "ITM001", quantityRequired: 3, quantityPicked: 0, remainingQuantity: 3, availableLocations: [...]}
Sending request data: {pickingDetailId: 456, sourceLocationId: 789, quantityToPick: 3}
Response received: {success: true, message: "Item processed successfully"}
=== FRONTEND PICKING DEBUG END - SUCCESS ===
```

### Step 4: Use Debug Endpoints

#### Debug Inventory State
```bash
GET /api/picking/debug/inventory/1
```
Response:
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "itemId": 1,
      "itemCode": "ITM001",
      "locationCode": "LOC001",
      "locationName": "Warehouse A",
      "quantity": 7,
      "status": "Available",
      "lastUpdated": "2025-01-24T10:30:00Z"
    },
    {
      "id": 2,
      "itemId": 1,
      "itemCode": "ITM001",
      "locationCode": "HOLD001",
      "locationName": "Holding Area",
      "quantity": 3,
      "status": "Available",
      "lastUpdated": "2025-01-24T10:30:00Z"
    }
  ]
}
```

#### Debug Picking State
```bash
GET /api/picking/debug/picking/123
```
Response:
```json
{
  "success": true,
  "data": {
    "picking": {
      "id": 123,
      "pickingNumber": "PICK-001",
      "status": "In Progress",
      "salesOrderNumber": "SO-001",
      "customerName": "Customer A",
      "holdingLocationId": 999,
      "holdingLocationName": "Holding Area"
    },
    "details": [
      {
        "id": 456,
        "itemId": 1,
        "itemCode": "ITM001",
        "quantityRequired": 3,
        "quantityPicked": 3,
        "remainingQuantity": 0,
        "status": "Picked"
      }
    ]
  }
}
```

### Step 5: Debug Test Page
1. Navigate ke `/picking/debug-test`
2. Masukkan Picking ID dan Item ID
3. Klik "Debug Picking State" atau "Debug Inventory State"
4. Lihat hasil debugging dalam format tabel

## Expected Bug Patterns

### Pattern 1: Quantity Mismatch
- **Symptom**: Quantity yang digunakan tidak sesuai dengan item yang dipick
- **Debug**: Check logs untuk melihat apakah `QuantityToPick` sesuai dengan `request.QuantityToPick`
- **Root Cause**: Kemungkinan data `availableLocations` tidak ter-update

### Pattern 2: Inventory Not Updated
- **Symptom**: Source location tidak berkurang atau holding location tidak bertambah
- **Debug**: Check logs untuk melihat apakah `ReduceStock` dan `AddStock` dipanggil dengan parameter yang benar
- **Root Cause**: Kemungkinan transaction rollback atau error dalam database operation

### Pattern 3: Wrong Item Processed
- **Symptom**: Item yang dipick tidak sesuai dengan yang dipilih
- **Debug**: Check logs untuk melihat apakah `pickingDetail.ItemId` sesuai dengan yang diharapkan
- **Root Cause**: Kemungkinan data `pickingDetail` tidak sesuai dengan `request.PickingDetailId`

## Troubleshooting

### Jika Logs Tidak Muncul
1. Check logging configuration di `appsettings.json`
2. Pastikan log level set ke `Information` atau `Debug`
3. Check console output atau log files

### Jika Debug Endpoints Error
1. Check permissions - pastikan user memiliki `PICKING_VIEW` permission
2. Check company context - pastikan user sudah login dengan company yang benar
3. Check database connection

### Jika Frontend Logs Tidak Muncul
1. Open browser console (F12)
2. Pastikan tidak ada JavaScript errors
3. Check network tab untuk melihat request/response

## Next Steps

Setelah debugging selesai dan bug ditemukan:
1. **Fix the root cause** berdasarkan hasil debugging
2. **Remove debug logs** untuk production
3. **Add proper error handling** untuk mencegah bug serupa
4. **Add unit tests** untuk memastikan fix bekerja dengan benar

## Debug Commands

### Check Logs (Windows)
```powershell
# Check application logs
Get-Content "logs\app-*.log" -Tail 50

# Monitor logs in real-time
Get-Content "logs\app-*.log" -Wait
```

### Check Database
```sql
-- Check picking details
SELECT * FROM PickingDetails WHERE PickingId = 123;

-- Check inventory changes
SELECT * FROM Inventories WHERE ItemId = 1 ORDER BY LastUpdated DESC;

-- Check audit logs
SELECT * FROM AuditLogs WHERE EntityType = 'Picking' ORDER BY CreatedDate DESC;
```
