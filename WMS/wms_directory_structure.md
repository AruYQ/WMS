# WMS 3.0 - Directory Structure

```
WMS/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── WMS.csproj
│
├── Controllers/
│   ├── HomeController.cs
│   ├── PurchaseOrderController.cs
│   ├── ASNController.cs
│   ├── SalesOrderController.cs
│   ├── InventoryController.cs
│   ├── ItemController.cs
│   ├── SupplierController.cs
│   ├── CustomerController.cs
│   └── LocationController.cs
│
├── Models/
│   ├── BaseEntity.cs
│   ├── Supplier.cs
│   ├── Customer.cs
│   ├── Item.cs
│   ├── Location.cs
│   ├── PurchaseOrder.cs
│   ├── PurchaseOrderDetail.cs
│   ├── AdvancedShippingNotice.cs
│   ├── ASNDetail.cs
│   ├── SalesOrder.cs
│   ├── SalesOrderDetail.cs
│   ├── Inventory.cs
│   └── ViewModels/
│       ├── PurchaseOrderViewModel.cs
│       ├── ASNViewModel.cs
│       ├── SalesOrderViewModel.cs
│       ├── InventoryViewModel.cs
│       ├── ItemTrackingViewModel.cs
│       └── DashboardViewModel.cs
│
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbInitializer.cs
│   └── Repositories/
│       ├── IRepository.cs
│       ├── Repository.cs
│       ├── IPurchaseOrderRepository.cs
│       ├── PurchaseOrderRepository.cs
│       ├── IASNRepository.cs
│       ├── ASNRepository.cs
│       ├── ISalesOrderRepository.cs
│       ├── SalesOrderRepository.cs
│       ├── IInventoryRepository.cs
│       ├── InventoryRepository.cs
│       ├── IItemRepository.cs
│       ├── ItemRepository.cs
│       ├── ISupplierRepository.cs
│       ├── SupplierRepository.cs
│       ├── ICustomerRepository.cs
│       ├── CustomerRepository.cs
│       ├── ILocationRepository.cs
│       └── LocationRepository.cs
│
├── Services/
│   ├── IPurchaseOrderService.cs
│   ├── PurchaseOrderService.cs
│   ├── IASNService.cs
│   ├── ASNService.cs
│   ├── ISalesOrderService.cs
│   ├── SalesOrderService.cs
│   ├── IInventoryService.cs
│   ├── InventoryService.cs
│   ├── IItemService.cs
│   ├── ItemService.cs
│   ├── IEmailService.cs
│   ├── EmailService.cs
│   ├── IWarehouseFeeCalculator.cs
│   └── WarehouseFeeCalculator.cs
│
├── Views/
│   ├── _ViewStart.cshtml
│   ├── _ViewImports.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _LayoutPartial.cshtml
│   │   ├── Error.cshtml
│   │   └── _ValidationScriptsPartial.cshtml
│   │
│   ├── Home/
│   │   ├── Index.cshtml
│   │   └── Privacy.cshtml
│   │
│   ├── PurchaseOrder/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   ├── Delete.cshtml
│   │   └── Send.cshtml
│   │
│   ├── ASN/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   └── Delete.cshtml
│   │
│   ├── SalesOrder/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   └── Delete.cshtml
│   │
│   ├── Inventory/
│   │   ├── Index.cshtml
│   │   ├── Putaway.cshtml
│   │   ├── Tracking.cshtml
│   │   └── LocationStatus.cshtml
│   │
│   ├── Item/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   └── Delete.cshtml
│   │
│   ├── Supplier/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   └── Delete.cshtml
│   │
│   ├── Customer/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   ├── Details.cshtml
│   │   └── Delete.cshtml
│   │
│   └── Location/
│       ├── Index.cshtml
│       ├── Create.cshtml
│       ├── Edit.cshtml
│       ├── Details.cshtml
│       └── Delete.cshtml
│
├── wwwroot/
│   ├── css/
│   │   ├── bootstrap.min.css
│   │   ├── site.css
│   │   └── wms-custom.css
│   │
│   ├── js/
│   │   ├── bootstrap.bundle.min.js
│   │   ├── jquery.min.js
│   │   ├── site.js
│   │   └── wms-scripts.js
│   │
│   ├── lib/
│   │   ├── bootstrap/
│   │   ├── jquery/
│   │   └── jquery-validation/
│   │
│   └── images/
│       └── logo.png
│
├── Migrations/
│   └── (Entity Framework migrations will be generated here)
│
├── Utilities/
│   ├── Constants.cs
│   ├── Enums.cs
│   ├── Extensions.cs
│   └── Helpers.cs
│
└── Properties/
    └── launchSettings.json
```

## Key Directory Explanations:

### **Controllers/**
Contains all MVC controllers that handle HTTP requests and coordinate between Views and Services.

### **Models/**
- **Root Models**: Entity classes that map to database tables
- **ViewModels/**: Classes specifically designed for passing data between Controllers and Views

### **Data/**
- **ApplicationDbContext.cs**: Entity Framework DbContext
- **Repositories/**: Repository pattern implementation for data access

### **Services/**
Contains business logic services that are called by Controllers. Each major feature has its own service interface and implementation.

### **Views/**
Contains all Razor views organized by controller name, plus shared layouts and partials.

### **wwwroot/**
Static files including CSS, JavaScript, images, and client-side libraries.

### **Utilities/**
Helper classes, constants, enums, and extension methods used throughout the application.

## Architecture Principles Applied:

1. **Clean Architecture**: Separation of concerns with clear boundaries between layers
2. **Repository Pattern**: Data access abstraction
3. **Service Layer**: Business logic encapsulation
4. **MVC Pattern**: Clear separation between Model, View, and Controller
5. **Dependency Injection**: Services registered and injected where needed