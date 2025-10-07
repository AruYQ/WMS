/**
 * Advanced Search Service - FIXED VERSION  
 * Provides reusable functionality for advanced search across all dropdowns
 */

class AdvancedSearchService {
    constructor() {
        this.currentEntityType = null;
        this.currentCallback = null;
        this.searchConfig = null;
        this.selectedItem = null;
        this.modal = null;
        this.isInitialized = false;
        this.init();
    }

    init() {
        // Wait for DOM to be ready before initializing
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                this.initializeWhenReady();
            });
        } else {
            this.initializeWhenReady();
        }
    }

    initializeWhenReady() {
        console.log('AdvancedSearchService: Initializing...');
        this.setupModal();
        this.bindEvents();
        this.isInitialized = true;
        console.log('AdvancedSearchService: Initialization complete');
    }

    bindEvents() {
        console.log('AdvancedSearchService: Binding events...');
        
        // Use event delegation for better reliability
        const modal = document.getElementById('advancedSearchModal');
        if (!modal) {
            console.error('AdvancedSearchService: Modal not found!');
            return;
        }

        // Search button click
        const searchBtn = document.getElementById('searchBtn');
        if (searchBtn) {
            searchBtn.addEventListener('click', () => {
                console.log('AdvancedSearchService: Search button clicked');
                this.performSearch();
            });
        } else {
            console.error('AdvancedSearchService: Search button not found!');
        }

        // Clear button click
        const clearBtn = document.getElementById('clearSearchBtn');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => {
                console.log('AdvancedSearchService: Clear button clicked');
                this.clearSearch();
            });
        } else {
            console.error('AdvancedSearchService: Clear button not found!');
        }

        // Select button click
        const selectBtn = document.getElementById('selectBtn');
        if (selectBtn) {
            selectBtn.addEventListener('click', () => {
                console.log('AdvancedSearchService: Select button clicked');
                this.selectItem();
            });
        } else {
            console.error('AdvancedSearchService: Select button not found!');
        }

        // Enter key in search text
        const searchText = document.getElementById('searchText');
        if (searchText) {
            searchText.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    console.log('AdvancedSearchService: Enter key pressed in search text');
                    this.performSearch();
                }
            });
        } else {
            console.error('AdvancedSearchService: Search text input not found!');
        }

        // Modal hidden event
        modal.addEventListener('hidden.bs.modal', () => {
            console.log('AdvancedSearchService: Modal hidden');
            this.resetModal();
        });

        console.log('AdvancedSearchService: Events bound successfully');
    }

    setupModal() {
        console.log('AdvancedSearchService: Setting up modal...');
        
        // Check if Bootstrap is available
        if (typeof bootstrap === 'undefined') {
            console.error('AdvancedSearchService: Bootstrap not available!');
            return;
        }

        // Get modal element
        const modalElement = document.getElementById('advancedSearchModal');
        if (!modalElement) {
            console.error('AdvancedSearchService: Modal element not found!');
            return;
        }

        try {
            this.modal = new bootstrap.Modal(modalElement);
            console.log('AdvancedSearchService: Modal initialized successfully');
        } catch (error) {
            console.error('AdvancedSearchService: Error initializing modal:', error);
        }
    }

    /**
     * Open advanced search modal for specific entity type
     * @param {string} entityType - Type of entity (supplier, customer, item, location, etc.)
     * @param {function} callback - Callback function to handle selected item
     * @param {object} config - Configuration object for search
     */
    openSearch(entityType, callback, config = {}) {
        console.log('AdvancedSearchService: Opening search for entity type:', entityType);
        
        // Check if service is initialized
        if (!this.isInitialized) {
            console.error('AdvancedSearchService: Service not initialized yet!');
            // Try to initialize now
            this.initializeWhenReady();
            if (!this.isInitialized) {
                console.error('AdvancedSearchService: Failed to initialize service!');
                return;
            }
        }

        // Validate parameters
        if (!entityType) {
            console.error('AdvancedSearchService: Entity type is required!');
            return;
        }

        if (typeof callback !== 'function') {
            console.error('AdvancedSearchService: Callback must be a function!');
            return;
        }

        this.currentEntityType = entityType;
        this.currentCallback = callback;
        this.searchConfig = config;
        this.selectedItem = null;

        // Update modal title
        const modalLabel = document.getElementById('advancedSearchModalLabel');
        if (modalLabel) {
            modalLabel.innerHTML = `<i class="fas fa-search me-2"></i>Advanced Search - ${this.getEntityDisplayName(entityType)}`;
            console.log('AdvancedSearchService: Modal title updated');
        } else {
            console.error('AdvancedSearchService: Modal label not found!');
            return;
        }

        // Setup entity-specific filters
        this.setupEntityFilters(entityType, config);

        // Show modal
        if (this.modal) {
            try {
                this.modal.show();
                console.log('AdvancedSearchService: Modal shown successfully');
            } catch (error) {
                console.error('AdvancedSearchService: Error showing modal:', error);
            }
        } else {
            console.error('AdvancedSearchService: Modal not initialized!');
            // Try to reinitialize modal
            this.setupModal();
            if (this.modal) {
                try {
                    this.modal.show();
                    console.log('AdvancedSearchService: Modal shown after reinitialization');
                } catch (error) {
                    console.error('AdvancedSearchService: Error showing modal after reinitialization:', error);
                }
            }
        }
    }

    getEntityDisplayName(entityType) {
        const displayNames = {
            'supplier': 'Supplier',
            'customer': 'Customer',
            'item': 'Item',
            'location': 'Location',
            'purchaseorder': 'Purchase Order',
            'salesorder': 'Sales Order',
            'asn': 'ASN'
        };
        return displayNames[entityType] || entityType;
    }

    setupEntityFilters(entityType, config) {
        const dynamicFilters = document.getElementById('dynamicFilters');
        console.log('Setting up entity filters for:', entityType);
        console.log('Dynamic filters container:', dynamicFilters);
        
        if (!dynamicFilters) {
            console.error('dynamicFilters container not found!');
            return;
        }
        
        dynamicFilters.innerHTML = '';

        // Entity-specific filters
        switch (entityType) {
            case 'supplier':
                console.log('Adding supplier filters...');
                this.addSupplierFilters(dynamicFilters, config);
                console.log('Supplier filters added. Container HTML:', dynamicFilters.innerHTML);
                break;
            case 'customer':
                this.addCustomerFilters(dynamicFilters, config);
                break;
            case 'item':
                this.addItemFilters(dynamicFilters, config);
                break;
            case 'location':
                this.addLocationFilters(dynamicFilters, config);
                break;
            case 'purchaseorder':
                this.addPurchaseOrderFilters(dynamicFilters, config);
                break;
            case 'salesorder':
                this.addSalesOrderFilters(dynamicFilters, config);
                break;
            case 'asn':
                this.addASNFilters(dynamicFilters, config);
                break;
        }
    }

    addSupplierFilters(container, config) {
        console.log('Adding supplier filters to container:', container);
        const html = `
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="supplierNameFilter" class="form-label">
                        <i class="fas fa-building me-1"></i>
                        Nama Supplier
                    </label>
                    <input type="text" class="form-control" id="supplierNameFilter" placeholder="Enter supplier name...">
                </div>
                <div class="col-md-6 mb-3">
                    <label for="phoneFilter" class="form-label">
                        <i class="fas fa-phone me-1"></i>
                        Phone
                    </label>
                    <input type="text" class="form-control" id="phoneFilter" placeholder="Enter phone number...">
                </div>
            </div>
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="cityFilter" class="form-label">
                        <i class="fas fa-map-marker-alt me-1"></i>
                        City
                    </label>
                    <input type="text" class="form-control" id="cityFilter" placeholder="Enter city name...">
                </div>
                <div class="col-md-6 mb-3">
                    <label for="supplierCodeFilter" class="form-label">
                        <i class="fas fa-tag me-1"></i>
                        Supplier Code
                    </label>
                    <input type="text" class="form-control" id="supplierCodeFilter" placeholder="Enter supplier code...">
                </div>
            </div>
        `;
        console.log('Supplier filters HTML:', html);
        container.innerHTML = html;
        console.log('Container innerHTML after setting:', container.innerHTML);
    }

    addCustomerFilters(container, config) {
        container.innerHTML = `
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="cityFilter" class="form-label">
                        <i class="fas fa-map-marker-alt me-1"></i>
                        City
                    </label>
                    <input type="text" class="form-control" id="cityFilter" placeholder="Enter city name...">
                </div>
                <div class="col-md-6 mb-3">
                    <label for="customerTypeFilter" class="form-label">
                        <i class="fas fa-tag me-1"></i>
                        Customer Type
                    </label>
                    <select class="form-select" id="customerTypeFilter">
                        <option value="">All Types</option>
                        <option value="individual">Individual</option>
                        <option value="company">Company</option>
                    </select>
                </div>
            </div>
        `;
    }

    addItemFilters(container, config) {
        container.innerHTML = `
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="supplierFilter" class="form-label">
                        <i class="fas fa-truck me-1"></i>
                        Supplier
                    </label>
                    <select class="form-select" id="supplierFilter">
                        <option value="">All Suppliers</option>
                        ${config.supplierOptions || ''}
                    </select>
                </div>
                <div class="col-md-6 mb-3">
                    <label for="priceRangeFilter" class="form-label">
                        <i class="fas fa-dollar-sign me-1"></i>
                        Price Range
                    </label>
                    <div class="row">
                        <div class="col-6">
                            <input type="number" class="form-control" id="priceFrom" placeholder="From" step="0.01">
                        </div>
                        <div class="col-6">
                            <input type="number" class="form-control" id="priceTo" placeholder="To" step="0.01">
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    addLocationFilters(container, config) {
        container.innerHTML = `
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="capacityRangeFilter" class="form-label">
                        <i class="fas fa-cubes me-1"></i>
                        Capacity Range
                    </label>
                    <div class="row">
                        <div class="col-6">
                            <input type="number" class="form-control" id="capacityFrom" placeholder="From" min="0">
                        </div>
                        <div class="col-6">
                            <input type="number" class="form-control" id="capacityTo" placeholder="To" min="0">
                        </div>
                    </div>
                </div>
                <div class="col-md-6 mb-3">
                    <label for="capacityStatusFilter" class="form-label">
                        <i class="fas fa-info-circle me-1"></i>
                        Capacity Status
                    </label>
                    <select class="form-select" id="capacityStatusFilter">
                        <option value="">All Status</option>
                        <option value="available">Available</option>
                        <option value="full">Full</option>
                        <option value="nearly-full">Nearly Full</option>
                    </select>
                </div>
            </div>
        `;
    }

    addPurchaseOrderFilters(container, config) {
        container.innerHTML = `
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="supplierNameFilter" class="form-label">
                        <i class="fas fa-truck me-1"></i>
                        Nama Supplier
                    </label>
                    <input type="text" class="form-control" id="supplierNameFilter" placeholder="Enter supplier name...">
                </div>
                <div class="col-md-6 mb-3">
                    <label for="phoneFilter" class="form-label">
                        <i class="fas fa-phone me-1"></i>
                        Phone
                    </label>
                    <input type="text" class="form-control" id="phoneFilter" placeholder="Enter phone number...">
                </div>
            </div>
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="poStatusFilter" class="form-label">
                        <i class="fas fa-tag me-1"></i>
                        PO Status
                    </label>
                    <select class="form-select" id="poStatusFilter">
                        <option value="">All Status</option>
                        <option value="Draft">Draft</option>
                        <option value="Sent">Sent</option>
                        <option value="Received">Received</option>
                        <option value="Closed">Closed</option>
                        <option value="Cancelled">Cancelled</option>
                    </select>
                </div>
                <div class="col-md-6 mb-3">
                    <label for="poNumberFilter" class="form-label">
                        <i class="fas fa-file-invoice me-1"></i>
                        PO Number
                    </label>
                    <input type="text" class="form-control" id="poNumberFilter" placeholder="Enter PO number...">
                </div>
            </div>
        `;
    }

    addSalesOrderFilters(container, config) {
        container.innerHTML = `
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="customerFilter" class="form-label">
                        <i class="fas fa-user me-1"></i>
                        Customer
                    </label>
                    <select class="form-select" id="customerFilter">
                        <option value="">All Customers</option>
                        ${config.customerOptions || ''}
                    </select>
                </div>
                <div class="col-md-6 mb-3">
                    <label for="soStatusFilter" class="form-label">
                        <i class="fas fa-tag me-1"></i>
                        SO Status
                    </label>
                    <select class="form-select" id="soStatusFilter">
                        <option value="">All Status</option>
                        <option value="pending">Pending</option>
                        <option value="confirmed">Confirmed</option>
                        <option value="picking">Picking</option>
                        <option value="completed">Completed</option>
                        <option value="cancelled">Cancelled</option>
                    </select>
                </div>
            </div>
        `;
    }

    addASNFilters(container, config) {
        container.innerHTML = `
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="asnNumberFilter" class="form-label">
                        <i class="fas fa-file-invoice me-1"></i>
                        ASN Number
                    </label>
                    <input type="text" class="form-control" id="asnNumberFilter" placeholder="Enter ASN number...">
                </div>
                <div class="col-md-6 mb-3">
                    <label for="supplierNameFilter" class="form-label">
                        <i class="fas fa-truck me-1"></i>
                        Supplier Name
                    </label>
                    <input type="text" class="form-control" id="supplierNameFilter" placeholder="Enter supplier name...">
                </div>
            </div>
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label for="asnStatusFilter" class="form-label">
                        <i class="fas fa-tag me-1"></i>
                        ASN Status
                    </label>
                    <select class="form-select" id="asnStatusFilter">
                        <option value="">All Status</option>
                        <option value="Pending">Pending</option>
                        <option value="Shipped">Shipped</option>
                        <option value="Received">Received</option>
                        <option value="Completed">Completed</option>
                    </select>
                </div>
            </div>
        `;
    }

    async performSearch() {
        console.log('AdvancedSearchService: Starting search...');
        
        try {
            // Validate current state
            if (!this.currentEntityType) {
                throw new Error('No entity type set for search');
            }

            const searchData = this.collectSearchData();
            
            // Comprehensive logging for debugging
            console.log('AdvancedSearchService: Search data collected:', searchData);
            console.log('AdvancedSearchService: Entity type:', this.currentEntityType);
            console.log('AdvancedSearchService: Search config:', this.searchConfig);
            
            // Validate search data
            if (!searchData) {
                throw new Error('Failed to collect search data');
            }

            // Show loading state
            this.showLoadingState();

            // Make API call
            const apiUrl = `/api/search/${this.currentEntityType}`;
            console.log('AdvancedSearchService: Making API call to:', apiUrl);
            
            const response = await fetch(apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(searchData)
            });

            console.log('AdvancedSearchService: Response received');
            console.log('AdvancedSearchService: Response status:', response.status);
            console.log('AdvancedSearchService: Response headers:', Object.fromEntries(response.headers.entries()));

            if (!response.ok) {
                // Try to get error details from response
                let errorDetails = '';
                let errorMessage = `HTTP error! status: ${response.status}`;
                
                try {
                    const errorData = await response.json();
                    console.log('AdvancedSearchService: Error response data:', errorData);
                    
                    if (errorData.details) {
                        errorDetails = ` - Details: ${JSON.stringify(errorData.details)}`;
                    } else if (errorData.error) {
                        errorDetails = ` - Error: ${errorData.error}`;
                    }
                    
                    errorMessage += errorDetails;
                } catch (e) {
                    console.log('AdvancedSearchService: Could not parse error response:', e);
                    const responseText = await response.text();
                    console.log('AdvancedSearchService: Raw error response:', responseText);
                    errorDetails = ` - Response text: ${responseText}`;
                    errorMessage += errorDetails;
                }
                
                throw new Error(errorMessage);
            }

            const results = await response.json();
            console.log('AdvancedSearchService: Search results received:', results);
            console.log('AdvancedSearchService: Number of results:', results.length);
            
            this.displaySearchResults(results);

        } catch (error) {
            console.error('AdvancedSearchService: Search error:', error);
            console.error('AdvancedSearchService: Error stack:', error.stack);
            this.showErrorState(error.message);
        }
    }

    // FIXED: collectSearchData method - now sends all fields including empty ones
    collectSearchData() {
        console.log('AdvancedSearchService: Collecting search data...');
        
        try {
            const data = {
                searchText: this.getInputValue('searchText'),
                statusFilter: this.getInputValue('statusFilter'),
                dateFrom: this.getInputValue('dateFrom'),
                dateTo: this.getInputValue('dateTo'),
                // Add default pagination values
                page: 1,
                pageSize: 50
            };

            console.log('AdvancedSearchService: Basic data collected:', data);

            // Add entity-specific filters with proper property mapping
            const dynamicFilters = document.getElementById('dynamicFilters');
            console.log('AdvancedSearchService: Dynamic filters container:', dynamicFilters);
            console.log('AdvancedSearchService: Dynamic filters HTML:', dynamicFilters ? dynamicFilters.innerHTML : 'NOT FOUND');
            
            if (dynamicFilters) {
                const inputs = dynamicFilters.querySelectorAll('input, select');
                console.log('AdvancedSearchService: Found dynamic filter inputs:', inputs.length);
                
                inputs.forEach((input, index) => {
                    console.log(`AdvancedSearchService: Input ${index}:`, {
                        id: input.id,
                        value: input.value,
                        type: input.type,
                        name: input.name
                    });
                    
                    // FIXED: Always map fields, even if empty
                    const fieldMapping = this.getFieldMapping();
                    const backendFieldName = fieldMapping[input.id] || input.id;
                    
                    // Send all fields, including empty ones (convert empty string to null)
                    const value = input.value && input.value.trim() !== '' ? input.value : null;
                    data[backendFieldName] = value;
                    console.log(`AdvancedSearchService: Mapped ${input.id} -> ${backendFieldName} = ${value}`);
                });
            } else {
                console.error('AdvancedSearchService: dynamicFilters container not found during data collection!');
            }

            // Handle date formatting
            if (data.dateFrom) {
                const formattedDate = this.formatDateForBackend(data.dateFrom);
                console.log('AdvancedSearchService: DateFrom formatted:', data.dateFrom, '->', formattedDate);
                data.dateFrom = formattedDate;
            }
            if (data.dateTo) {
                const formattedDate = this.formatDateForBackend(data.dateTo);
                console.log('AdvancedSearchService: DateTo formatted:', data.dateTo, '->', formattedDate);
                data.dateTo = formattedDate;
            }

            // Validate data before sending
            const validationResult = this.validateSearchData(data);
            if (!validationResult.isValid) {
                console.error('AdvancedSearchService: Search data validation failed:', validationResult.errors);
                throw new Error(`Validation failed: ${validationResult.errors.join(', ')}`);
            }

            console.log('AdvancedSearchService: Final collected data:', data);
            return data;
            
        } catch (error) {
            console.error('AdvancedSearchService: Error collecting search data:', error);
            throw error;
        }
    }

    getInputValue(elementId) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.warn(`AdvancedSearchService: Element ${elementId} not found`);
            return null;
        }
        return element.value || null;
    }

    validateSearchData(data) {
        const errors = [];

        // Validate date range
        if (data.dateFrom && data.dateTo) {
            const fromDate = new Date(data.dateFrom);
            const toDate = new Date(data.dateTo);
            if (fromDate > toDate) {
                errors.push('DateFrom cannot be greater than DateTo');
            }
        }

        // Validate pagination
        if (data.page && (data.page < 1 || data.page > 1000)) {
            errors.push('Page must be between 1 and 1000');
        }
        if (data.pageSize && (data.pageSize < 1 || data.pageSize > 1000)) {
            errors.push('PageSize must be between 1 and 1000');
        }

        return {
            isValid: errors.length === 0,
            errors: errors
        };
    }

    getFieldMapping() {
        const mappings = {
            'cityFilter': 'CityFilter',
            'contactPersonFilter': 'ContactPersonFilter',
            'statusFilter': 'StatusFilter',
            'supplierFilter': 'SupplierFilter',
            'supplierNameFilter': 'SupplierNameFilter',
            'supplierCodeFilter': 'SupplierCodeFilter',
            'phoneFilter': 'PhoneFilter',
            'poNumberFilter': 'PONumberFilter',
            'poStatusFilter': 'POStatusFilter',
            'customerFilter': 'CustomerFilter',
            'customerTypeFilter': 'CustomerTypeFilter',
            'priceFrom': 'PriceFrom',
            'priceTo': 'PriceTo',
            'capacityFrom': 'CapacityFrom',
            'capacityTo': 'CapacityTo',
            'capacityStatusFilter': 'CapacityStatusFilter',
            'soStatusFilter': 'SOStatusFilter',
            'asnStatusFilter': 'ASNStatusFilter',
            'asnNumberFilter': 'ASNNumberFilter'
        };
        return mappings;
    }

    formatDateForBackend(dateString) {
        if (!dateString) return null;
        
        try {
            // Handle different date formats
            let date;
            
            // Check if it's already in ISO format
            if (dateString.includes('T') || dateString.includes('Z')) {
                date = new Date(dateString);
            } else if (dateString.includes('/')) {
                // Handle DD/MM/YYYY format (common in date inputs)
                const parts = dateString.split('/');
                if (parts.length === 3) {
                    // Assume DD/MM/YYYY format
                    const day = parseInt(parts[0], 10);
                    const month = parseInt(parts[1], 10) - 1; // Month is 0-indexed
                    const year = parseInt(parts[2], 10);
                    date = new Date(year, month, day);
                } else {
                    date = new Date(dateString);
                }
            } else if (dateString.includes('-')) {
                // Handle YYYY-MM-DD format (HTML date input)
                date = new Date(dateString);
            } else {
                // Try direct parsing
                date = new Date(dateString);
            }
            
            if (isNaN(date.getTime())) {
                console.warn('Invalid date format:', dateString);
                return null;
            }
            
            // Return in ISO format for backend (date only, no time)
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            return `${year}-${month}-${day}T00:00:00.000Z`;
        } catch (error) {
            console.warn('Error parsing date:', dateString, error);
            return null;
        }
    }

    displaySearchResults(results) {
        const resultsContainer = document.getElementById('resultsContainer');
        const resultCount = document.getElementById('resultCount');
        
        resultCount.textContent = results.length;
        
        if (results.length === 0) {
            resultsContainer.innerHTML = `
                <div class="text-center py-4">
                    <i class="fas fa-search fa-2x text-muted mb-3"></i>
                    <p class="text-muted">No results found</p>
                </div>
            `;
        } else {
            resultsContainer.innerHTML = results.map(result => this.renderResultItem(result)).join('');
        }

        document.getElementById('searchResults').style.display = 'block';
    }

    renderResultItem(item) {
        const displayText = this.getDisplayText(item);
        const additionalInfo = this.getAdditionalInfo(item);
        
        return `
            <div class="search-result-item border-bottom py-2 cursor-pointer" 
                 data-item='${JSON.stringify(item)}' 
                 onclick="advancedSearchService.selectResultItem(this)">
                <div class="d-flex justify-content-between align-items-start">
                    <div class="flex-grow-1">
                        <h6 class="mb-1 text-primary">${displayText}</h6>
                        <small class="text-muted">${additionalInfo}</small>
                    </div>
                    <i class="fas fa-chevron-right text-muted"></i>
                </div>
            </div>
        `;
    }

    getDisplayText(item) {
        switch (this.currentEntityType) {
            case 'supplier':
                return `${item.name} (${item.code || 'N/A'})`;
            case 'customer':
                return `${item.name} (${item.code || 'N/A'})`;
            case 'item':
                return `${item.name} - ${item.itemCode}`;
            case 'location':
                return `${item.name} (${item.code})`;
            case 'purchaseorder':
                return `${item.poNumber} - ${item.supplier?.name || 'N/A'}`;
            case 'salesorder':
                return `${item.soNumber} - ${item.customerName}`;
            case 'asn':
                return `${item.asnNumber} - ${item.supplierName}`;
            default:
                return item.name || item.displayName || 'Unknown';
        }
    }

    getAdditionalInfo(item) {
        switch (this.currentEntityType) {
            case 'supplier':
                return `Phone: ${item.phone || 'N/A'} | City: ${item.city || 'N/A'} | Code: ${item.code || 'N/A'}`;
            case 'customer':
                return `Type: ${item.customerType || 'N/A'} | City: ${item.city || 'N/A'}`;
            case 'item':
                return `Supplier: ${item.supplierName || 'N/A'} | Price: ${item.standardPrice || 0}`;
            case 'location':
                return `Capacity: ${item.currentCapacity || 0}/${item.maxCapacity || 0} | Status: ${item.capacityStatus || 'N/A'}`;
            case 'purchaseorder':
                return `Date: ${item.orderDate} | Amount: ${item.totalAmount || 0} | Status: ${item.status}`;
            case 'salesorder':
                return `Date: ${item.orderDate} | Amount: ${item.totalAmount || 0}`;
            case 'asn':
                return `Shipment Date: ${item.shipmentDate} | Status: ${item.status}`;
            default:
                return item.description || '';
        }
    }

    selectResultItem(element) {
        // Remove previous selection
        document.querySelectorAll('.search-result-item').forEach(item => {
            item.classList.remove('bg-light', 'border-primary');
        });

        // Add selection styling
        element.classList.add('bg-light', 'border-primary');
        
        // Store selected item
        this.selectedItem = JSON.parse(element.dataset.item);
        
        // Show select button
        document.getElementById('selectBtn').style.display = 'inline-block';
    }

    selectItem() {
        if (this.selectedItem && this.currentCallback) {
            this.currentCallback(this.selectedItem);
            this.modal.hide();
        }
    }

    clearSearch() {
        // Clear all form inputs
        document.getElementById('advancedSearchForm').reset();
        
        // Hide results
        document.getElementById('searchResults').style.display = 'none';
        document.getElementById('selectBtn').style.display = 'none';
        
        // Clear selected item
        this.selectedItem = null;
    }

    resetModal() {
        this.clearSearch();
        this.currentEntityType = null;
        this.currentCallback = null;
        this.searchConfig = null;
        this.selectedItem = null;
    }

    showLoadingState() {
        const resultsContainer = document.getElementById('resultsContainer');
        resultsContainer.innerHTML = `
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-2 text-muted">Searching...</p>
            </div>
        `;
        document.getElementById('searchResults').style.display = 'block';
    }

    showErrorState(message) {
        const resultsContainer = document.getElementById('resultsContainer');
        resultsContainer.innerHTML = `
            <div class="text-center py-4">
                <i class="fas fa-exclamation-triangle fa-2x text-danger mb-3"></i>
                <p class="text-danger">Error: ${message}</p>
                <button class="btn btn-outline-primary btn-sm" onclick="advancedSearchService.performSearch()">
                    <i class="fas fa-redo me-1"></i>
                    Try Again
                </button>
            </div>
        `;
        document.getElementById('searchResults').style.display = 'block';
    }
}

// Initialize global instance
const advancedSearchService = new AdvancedSearchService();