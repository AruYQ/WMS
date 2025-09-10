# WMS 3.0 - Updated Directory Structure (Company Management Separated)

```
WMS/
├── Program.cs                                 
├── appsettings.json                        
├── appsettings.Development.json            
├── WMS.csproj
│
├── Controllers/
│   ├── HomeController.cs                      
│   ├── AccountController.cs                   
│   ├── UserController.cs                     
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
│   ├── Company.cs                             
│   ├── User.cs                               
│   ├── Role.cs                               
│   ├── UserRole.cs                           
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
│       ├── LoginViewModel.cs                
│       ├── UserViewModel.cs                  
│       ├── ResetUserPasswordViewModel.cs
│       ├── ResetPasswordViewModel.cs          
│       ├── UserListViewModel.cs              
│       ├── UserDetailsViewModel.cs           
│       ├── ForgetPasswordViewModel.cs        
│       ├── UserProfileViewModel.cs            
│       ├── CreateUserViewModel.cs             
│       ├── EditUserViewModel.cs             
│       ├── ChangePasswordViewModel.cs       
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
│       ├── IUserRepository.cs               
│       ├── UserRepository.cs                 
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
│   ├── ICurrentUserService.cs              
│   ├── CurrentUserService.cs                
│   ├── IAuthenticationService.cs             
│   ├── AuthenticationService.cs              
│   ├── IUserService.cs                      
│   ├── UserService.cs                         
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
├── Middleware/
│   ├── CompanyContextMiddleware.cs          
│   ├── RequestLoggingMiddleware.cs          
│   ├── ExceptionHandlingMiddleware.cs         
│   └── AuthenticationMiddleware.cs          
│
├── Attributes/
│   ├── RequireCompanyAttribute.cs            
│   ├── RequirePermissionAttribute.cs         
│   ├── AuditLogAttribute.cs                  
│   ├── WMSAllowAnonymousAttribute.cs          
│   └── RequireRoleAttribute.cs               
│
├── Views/
│   ├── _ViewStart.cshtml                   
│   ├── _ViewImports.cshtml                  
│   ├── Shared/
│   │   ├── _Layout.cshtml                    
│   │   ├── _LoginLayout.cshtml
│   │   ├── _LayoutPartial.cshtml            
│   │   ├── Error.cshtml                      
│   │   └── _ValidationScriptsPartial.cshtml  
│   │
│   ├── Account/                           
│   │   ├── Login.cshtml                      
│   │   ├── AccessDenied.cshtml               
│   │   └── Logout.cshtml              
│   │
│   ├── User/                                 
│   │   ├── Index.cshtml                       
│   │   ├── Create.cshtml                   
│   │   ├── Edit.cshtml                       
│   │   ├── Details.cshtml                    
│   │   ├── Delete.cshtml                    
│   │   ├── Profile.cshtml                   
│   │   └── ChangePassword.cshtml            
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
│   │   ├── wms-custom.css                    
│   │   └── auth.css                          
│   │
│   ├── js/
│   │   ├── bootstrap.bundle.min.js           
│   │   ├── jquery.min.js                    
│   │   ├── site.js                           
│   │   ├── wms-scripts.js                    
│   │   └── auth.js                          
│   ├── lib/                                   
│   │   ├── bootstrap/
│   │   ├── jquery/
│   │   └── jquery-validation/
│   │
│   └── images/
│       ├── logo.png                         
│       └── default-avatar.png               
│
├── Migrations/                               
│   ├── Constants.cs                        
│   ├── Enums.cs                             
│   ├── Extensions.cs                      
│   ├── Helpers.cs                          
│   ├── PasswordHelper.cs                  
│   └── TokenHelper.cs                     
│
├── Configuration/                          
│   ├── JwtSettings.cs                      
│   ├── AuthenticationSettings.cs          
│   └── CompanySettings.cs                  
│
└── Properties/
    └── launchSettings.json                  
```

## REMOVED (Moved to separate Company Management application):
- CompanyController.cs
- CompanyRepository.cs & ICompanyRepository.cs
- CompanyService.cs & ICompanyService.cs
- All Company management views
- Company registration views/ViewModels
- User registration functionality

## Key Changes from Original Structure:

### **1. Authentication Only (No Registration)**
- Login/logout functionality only
- Users are added by existing company admins
- No public registration process

### **2. Company Context (Not Management)**
- Users belong to a company (set during user creation)
- All data automatically filtered by user's company
- No company switching - one company per user

### **3. Simplified User Management**
- Company admins can add/edit users within their company
- Users can edit their own profile
- Role-based permissions within company

### **4. All Business Data Company-Filtered**
- Every entity gets CompanyId
- All repositories filter by current user's company
- Complete data isolation between companies

## Views Transferred from updated_wms_directory.md:

### **Account Views Added:**
- `ForgotPassword.cshtml` - Password recovery
- `ResetPassword.cshtml` - Password reset form

### **Complete View Folders Added:**
- `ASN/` - All ASN management views
- `SalesOrder/` - All Sales Order management views  
- `Item/` - All Item management views
- `Supplier/` - All Supplier management views
- `Customer/` - All Customer management views
- `Location/` - All Location management views

### **Inventory Views Added:**
- `Putaway.cshtml` - Inventory putaway functionality
- `Tracking.cshtml` - Item tracking views
- `LocationStatus.cshtml` - Location status views

### **Utilities Added:**
- `TokenHelper.cs` - JWT token utilities

### **Configuration Folder Added:**
- Complete Configuration folder with JWT and auth settings

### **wwwroot lib folder Added:**
- Standard client-side libraries structure

## Next Implementation Priority:

1. **Update appsettings** - Add authentication configuration
2. **Update ApplicationDbContext** - Add User/Company entities
3. **Create Core Services** - CurrentUserService, AuthenticationService
4. **Update Repository base** - Add company filtering
5. **Create AccountController** - Login/logout functionality
6. **Update Program.cs** - Add authentication middleware
