/**
 * ASN Manager - AJAX-based CRUD operations
 * Manages all ASN operations following the established pattern
 */
class ASNManager {
    constructor() {
        this.currentASN = null;
        this.currentASNId = null;
        this.currentPage = 1;
        this.pageSize = 10;
        this.currentStatus = null;
        this.currentSearch = '';
        this.items = []; // Items from selected Purchase Order
        this.currentASNItems = []; // Items being added to ASN
        this.selectedItemIds = []; // Track which items are already selected in dropdowns
        this.maxAllowedItems = 10; // Maximum allowed items (will be set based on ASN/PO)
        this.selectedPurchaseOrderId = null; // Track selected Purchase Order ID
        
        // Initialize ASN Purchase Order Manager
        this.asnPOManager = new ASNPurchaseOrderManager();
        
        // Initialize ASN Location Manager
        this.asnLocationManager = new ASNLocationManager();
        
        // Initialize ASN Item Autocomplete
        this.asnItemAutocomplete = new ASNItemAutocomplete();
        
        this.init();
    }

    /**
     * Helper function to safely set element value
     */
    setElementValue(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            element.value = value || '';
        }
    }

    /**
     * Initialize the manager
     */
    async init() {
        try {
            console.log('ASNManager: Initializing...');
            
            // Load initial data
            await this.loadDashboard();
            await this.loadASNs();
            
            // Bind events
            this.bindEvents();
            
            console.log('ASNManager: Initialized successfully');
        } catch (error) {
            console.error('ASNManager: Initialization failed:', error);
        }
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        // Search and filter events
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.currentSearch = e.target.value;
                this.debounceSearch();
            });
        }

        const statusFilter = document.getElementById('statusFilter');
        if (statusFilter) {
            statusFilter.addEventListener('change', (e) => {
                this.currentStatus = e.target.value;
                this.currentPage = 1;
                this.loadASNs();
            });
        }

        const pageSizeSelect = document.getElementById('pageSizeSelect');
        if (pageSizeSelect) {
            pageSizeSelect.addEventListener('change', (e) => {
                this.pageSize = parseInt(e.target.value);
                this.currentPage = 1;
                this.loadASNs();
            });
        }

        // Create button
        const createASNBtn = document.getElementById('createASNBtn');
        if (createASNBtn) {
            createASNBtn.addEventListener('click', () => {
                this.showCreateModal();
            });
        }

        // Purchase Order Advanced Search button
        const poAdvancedSearchBtn = document.getElementById('poAdvancedSearchBtn');
        if (poAdvancedSearchBtn) {
            poAdvancedSearchBtn.addEventListener('click', () => this.asnPOManager.openPOAdvancedSearch());
        }

        // Location Advanced Search button
        const locationAdvancedSearchBtn = document.getElementById('locationAdvancedSearchBtn');
        if (locationAdvancedSearchBtn) {
            locationAdvancedSearchBtn.addEventListener('click', () => this.asnLocationManager.openLocationAdvancedSearch());
        }

        // Form submission
        const asnForm = document.getElementById('asnForm');
        if (asnForm) {
            asnForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.saveASN();
            });
        }

        // Purchase Order selection change
        const purchaseOrderSelect = document.getElementById('purchaseOrderId');
        if (purchaseOrderSelect) {
            purchaseOrderSelect.addEventListener('change', (e) => {
                this.onPurchaseOrderChange(e.target.value);
            });
        }

        // Add Item button
        const addASNItemBtn = document.getElementById('addASNItemBtn');
        if (addASNItemBtn) {
            addASNItemBtn.addEventListener('click', () => {
                this.addASNItem();
            });
        }

        // Modal events
        const asnModal = document.getElementById('asnModal');
        if (asnModal) {
            asnModal.addEventListener('hidden.bs.modal', () => {
                // PENTING: Close modal advanced search jika masih terbuka
                const poAdvancedSearchModal = document.getElementById('poAdvancedSearchModal');
                if (poAdvancedSearchModal) {
                    const poModal = bootstrap.Modal.getInstance(poAdvancedSearchModal);
                    if (poModal) {
                        poModal.hide();
                    }
                }
                
                this.resetForm();
            });
        }
    }

    /**
     * Load dashboard statistics
     */
    async loadDashboard() {
        try {
            const response = await fetch('/api/asn/dashboard');
            const result = await response.json();

            if (result.success) {
                const data = result.data;
                document.getElementById('totalASNs').textContent = data.totalASNs;
                document.getElementById('pendingASNs').textContent = data.pendingASNs;
                document.getElementById('onDeliveryASNs').textContent = data.onDeliveryASNs;
                document.getElementById('arrivedASNs').textContent = data.arrivedASNs;
                document.getElementById('processedASNs').textContent = data.processedASNs;
            } else {
                console.error('Failed to load dashboard:', result.message);
            }
        } catch (error) {
            console.error('Error loading dashboard:', error);
        }
    }

    /**
     * Load ASN list with pagination
     */
    async loadASNs() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                ...(this.currentStatus && { status: this.currentStatus }),
                ...(this.currentSearch && { search: this.currentSearch })
            });

            const response = await fetch(`/api/asn?${params}`);
            const result = await response.json();

            if (result.success) {
                this.renderASNsTable(result.data);
                if (result.pagination) {
                    this.updatePagination(result.pagination);
                }
            } else {
                this.showError(result.message || 'Failed to load ASNs');
                // Show empty state
                this.renderASNsTable([]);
                this.updatePagination({
                    currentPage: 1,
                    totalPages: 1,
                    totalCount: 0,
                    pageSize: this.pageSize
                });
            }
        } catch (error) {
            console.error('Error loading ASNs:', error);
            this.showError('Error loading ASNs');
            // Show empty state on error
            this.renderASNsTable([]);
            this.updatePagination({
                currentPage: 1,
                totalPages: 1,
                totalCount: 0,
                pageSize: this.pageSize
            });
        }
    }

    /**
     * Render ASN table
     */
    renderASNsTable(asns) {
        const container = document.getElementById('asnsTableContainer');
        if (!container) return;

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead class="table-light">
                        <tr>
                            <th style="width: 15%">ASN Number</th>
                            <th style="width: 15%">PO Number</th>
                            <th style="width: 20%">Supplier</th>
                            <th style="width: 10%">Status</th>
                            <th style="width: 12%">Expected Date</th>
                            <th style="width: 10%">Items</th>
                            <th style="width: 8%">Quantity</th>
                            <th style="width: 10%">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        if (asns && asns.length > 0) {
            asns.forEach(asn => {
                const statusBadge = this.getStatusBadge(asn.status);
                const expectedDate = new Date(asn.expectedArrivalDate).toLocaleDateString();
                
                tableHtml += `
                    <tr>
                        <td><strong>${asn.asnNumber}</strong></td>
                        <td>${asn.purchaseOrderNumber}</td>
                        <td>${asn.supplierName}</td>
                        <td>${statusBadge}</td>
                        <td>${expectedDate}</td>
                        <td><span class="badge bg-info">${asn.totalItems}</span></td>
                        <td><span class="badge bg-secondary">${asn.totalQuantity}</span></td>
                        <td>
                            <div class="btn-group" role="group">
                                <!-- View button - Always visible for all statuses -->
                                <button type="button" class="btn btn-sm btn-outline-primary" onclick="asnManager.viewASN(${asn.id})" title="View Details">
                                    <i class="fas fa-eye"></i>
                                </button>
                                ${asn.status === 'Pending' || asn.status === 'Cancelled' ? `
                                    <!-- Edit button - Only visible for Pending and Cancelled status -->
                                    <button type="button" class="btn btn-sm btn-outline-warning" onclick="asnManager.editASN(${asn.id})" title="Edit">
                                        <i class="fas fa-edit"></i>
                                    </button>
                                ` : ''}
                                ${asn.status !== 'Processed' && asn.status !== 'Cancelled' ? `
                                    <!-- Update Status button - Hidden for Processed and Cancelled status -->
                                    <button type="button" class="btn btn-sm ${this.getStatusButtonClass(asn.status)}" onclick="asnManager.updateStatus(${asn.id}, '${asn.status}')" title="Update Status">
                                        <i class="fas ${this.getStatusIcon(asn.status)}"></i>
                                    </button>
                                ` : ''}
                                ${asn.status === 'Pending' ? `
                                    <!-- Cancel button - Only for Pending status -->
                                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.showCancelASNModal(${asn.id}, '${asn.asnNumber}')" title="Cancel">
                                        <i class="fas fa-ban"></i>
                                    </button>
                                ` : ''}
                            </div>
                        </td>
                    </tr>
                `;
            });
        } else {
            tableHtml += `
                <tr>
                    <td colspan="8" class="text-center text-muted py-4">
                        <i class="fas fa-inbox fa-2x mb-2"></i><br>
                        No ASNs found
                    </td>
                </tr>
            `;
        }

        tableHtml += `
                    </tbody>
                </table>
            </div>
        `;

        container.innerHTML = tableHtml;
    }

    /**
     * Update pagination controls
     */
    updatePagination(pagination) {
        // Check if pagination parameter is valid
        if (!pagination || typeof pagination !== 'object') {
            console.error('ASNManager: Invalid pagination parameter:', pagination);
            pagination = {
                currentPage: 1,
                totalPages: 1,
                totalCount: 0,
                pageSize: this.pageSize || 10
            };
        }

        const currentPage = pagination.currentPage || 1;
        const totalPages = pagination.totalPages || 1;
        const totalCount = pagination.totalCount || 0;
        const pageSize = pagination.pageSize || this.pageSize || 10;
        
        const start = (currentPage - 1) * pageSize + 1;
        const end = Math.min(currentPage * pageSize, totalCount);
        
        // Update pagination info with proper null checking
        const showingStartEl = document.getElementById('showingStart');
        const showingEndEl = document.getElementById('showingEnd');
        const totalRecordsEl = document.getElementById('totalRecords');
        const currentPageNumEl = document.getElementById('currentPageNum');
        const totalPagesNumEl = document.getElementById('totalPagesNum');
        
        if (showingStartEl) showingStartEl.textContent = start;
        if (showingEndEl) showingEndEl.textContent = end;
        if (totalRecordsEl) totalRecordsEl.textContent = totalCount;
        if (currentPageNumEl) currentPageNumEl.textContent = currentPage;
        if (totalPagesNumEl) totalPagesNumEl.textContent = totalPages;
        
        // Update pagination buttons
        const prevBtn = document.getElementById('prevPageBtn');
        const nextBtn = document.getElementById('nextPageBtn');
        
        if (prevBtn) {
            prevBtn.disabled = currentPage <= 1;
            prevBtn.onclick = currentPage <= 1 ? null : () => this.previousPage();
        }
        
        if (nextBtn) {
            nextBtn.disabled = currentPage >= totalPages;
            nextBtn.onclick = currentPage >= totalPages ? null : () => this.nextPage();
        }
    }

    /**
     * Load available Purchase Orders for ASN creation
     */
    async loadPurchaseOrders() {
        try {
            const response = await fetch('/api/asn/purchaseorders');
            const result = await response.json();

            if (result.success) {
                this.populatePurchaseOrderDropdown(result.data);
            } else {
                console.error('Failed to load purchase orders:', result.message);
                this.showError('Failed to load purchase orders');
            }
        } catch (error) {
            console.error('Error loading purchase orders:', error);
            this.showError('Error loading purchase orders');
        }
    }

    /**
     * Load locations for holding location dropdown
     */
    async loadLocations() {
        try {
            const response = await fetch('/api/asn/locations');
            const result = await response.json();

            if (result.success) {
                this.populateLocationDropdown(result.data);
            } else {
                console.error('Failed to load locations:', result.message);
                this.showError('Failed to load locations');
            }
        } catch (error) {
            console.error('Error loading locations:', error);
            this.showError('Error loading locations');
        }
    }

    /**
     * Populate Location dropdown
     */
    populateLocationDropdown(locations) {
        const select = document.getElementById('holdingLocationId');
        if (!select) return;

        // Clear existing options (except the first one)
        select.innerHTML = '<option value="">-- Select Holding Location --</option>';

        locations.forEach(location => {
            const option = document.createElement('option');
            option.value = location.id;
            option.textContent = `${location.name} (${location.code}) - Available: ${location.availableCapacity}/${location.maxCapacity}`;
            select.appendChild(option);
        });
    }

    /**
     * Populate Purchase Order dropdown
     */
    populatePurchaseOrderDropdown(purchaseOrders) {
        const select = document.getElementById('purchaseOrderId');
        if (!select) return;

        // Clear existing options (except the first one)
        select.innerHTML = '<option value="">-- Select Purchase Order --</option>';

        purchaseOrders.forEach(po => {
            const option = document.createElement('option');
            option.value = po.id;
            option.textContent = `${po.poNumber} - ${po.supplierName} (${new Date(po.orderDate).toLocaleDateString()})`;
            option.setAttribute('data-po-data', JSON.stringify(po));
            select.appendChild(option);
        });
    }

    /**
     * Handle Purchase Order selection change
     */
    async onPurchaseOrderChange(purchaseOrderId) {
        try {
            if (!purchaseOrderId) {
                // Reset form when no PO selected
                this.setElementValue('supplierName', '');
                this.items = [];
                this.currentASNItems = [];
                this.selectedItemIds = []; // Reset selected items tracking
                this.selectedPurchaseOrderId = null; // Reset selected PO ID
                this.updateASNItemsContainer();
                this.updateASNSummary();
                this.toggleAddItemButton(false);
                return;
            }

            // Set selected Purchase Order ID
            this.selectedPurchaseOrderId = parseInt(purchaseOrderId);
            console.log('ASNManager: Selected Purchase Order ID set to:', this.selectedPurchaseOrderId);

            // Clear selected items when PO changes
            this.selectedItemIds = [];
            this.updateASNItemsContainer();

            // Get selected PO data
            const select = document.getElementById('purchaseOrderId');
            const selectedOption = select.querySelector(`option[value="${purchaseOrderId}"]`);
            
            if (selectedOption) {
                const poData = JSON.parse(selectedOption.getAttribute('data-po-data'));
                this.setElementValue('supplierName', poData.supplierName);
                
                // Load items for this Purchase Order
                await this.loadItemsForPurchaseOrder(purchaseOrderId);
                this.toggleAddItemButton(true);
            }
        } catch (error) {
            console.error('Error handling PO change:', error);
            this.showError('Error loading purchase order details');
        }
    }

    /**
     * Load items for selected Purchase Order
     */
    async loadItemsForPurchaseOrder(purchaseOrderId) {
        try {
            const response = await fetch(`/api/asn/items/${purchaseOrderId}`);
            const result = await response.json();

            if (result.success) {
                this.items = result.data;
                // Set max allowed items based on PO items (for create mode)
                this.maxAllowedItems = this.items.length;
                console.log('Items loaded for PO:', this.items);
                console.log('Max allowed items set to:', this.maxAllowedItems);
                
                // Update button state
                this.updateAddItemButtonState();
                
                // Re-initialize autocomplete for existing item rows with new PO context
                this.reinitializeItemAutocomplete();
            } else {
                console.error('Failed to load items:', result.message);
                this.showError('Failed to load items for this purchase order');
            }
        } catch (error) {
            console.error('Error loading items:', error);
            this.showError('Error loading items');
        }
    }

    /**
     * Toggle Add Item button state
     */
    toggleAddItemButton(enabled) {
        const addBtn = document.getElementById('addASNItemBtn');
        if (addBtn) {
            addBtn.disabled = !enabled;
        }
    }

    /**
     * Get maximum allowed items based on current context
     */
    getMaxAllowedItems() {
        // If edit mode, use the number of items in the current ASN
        if (this.currentASN && this.currentASN.asnDetails) {
            return this.currentASN.asnDetails.length;
        }
        
        // If create mode, use the number of items from selected Purchase Order
        if (this.items && this.items.length > 0) {
            return this.items.length;
        }
        
        return this.maxAllowedItems; // Default fallback
    }

    /**
     * Update Add Item button state based on current item count
     */
    updateAddItemButtonState() {
        const addItemBtn = document.getElementById('addASNItemBtn');
        const existingRows = document.querySelectorAll('.asn-item-row');
        const maxItems = this.getMaxAllowedItems();
        
        if (addItemBtn) {
            if (existingRows.length >= maxItems) {
                addItemBtn.disabled = true;
                addItemBtn.innerHTML = `<i class="fas fa-plus"></i> Maksimal ${maxItems} Item`;
            } else {
                addItemBtn.disabled = false;
                addItemBtn.innerHTML = '<i class="fas fa-plus"></i> Add Item';
            }
        }
    }

    /**
     * Re-initialize autocomplete for existing item rows when PO changes
     */
    reinitializeItemAutocomplete() {
        const existingRows = document.querySelectorAll('.asn-item-row');
        existingRows.forEach((row, index) => {
            const searchInputId = `itemSearchInput_${index}`;
            const resultsId = `itemSearchResults_${index}`;
            const itemIdInput = row.querySelector('.asn-item-id');
            
            if (searchInputId && resultsId && itemIdInput && window.asnItemAutocomplete) {
                // Re-initialize autocomplete with new PO context
                window.asnItemAutocomplete.initialize(
                    searchInputId, 
                    resultsId, 
                    itemIdInput.id,
                    (selectedItem) => this.onASNItemSelect(index, selectedItem)
                );
                console.log('ASNManager: Re-initialized autocomplete for row', index, 'with PO ID:', this.selectedPurchaseOrderId);
            }
        });
    }

    /**
     * Add item to ASN
     */
    addASNItem() {
        if (!this.items || this.items.length === 0) {
            this.showError('No items available. Please select a Purchase Order first.');
            return;
        }

        // Get current number of rows to determine the next index
        const existingRows = document.querySelectorAll('.asn-item-row');
        const maxItems = this.getMaxAllowedItems();
        
        // Check if we've reached the maximum allowed items
        if (existingRows.length >= maxItems) {
            this.showError(`Maksimal ${maxItems} item sesuai dengan ASN yang dipilih`);
            return;
        }
        
        const itemIndex = existingRows.length;
        
        const itemRowHtml = this.getASNItemRowHtml(itemIndex);
        
        const container = document.getElementById('asnItemsContainer');
        if (container) {
            container.insertAdjacentHTML('beforeend', itemRowHtml);
            this.updateASNSummary();
        }

        // Initialize the new row
        this.initializeASNItemRow(itemIndex);
        
        // Update button state
        this.updateAddItemButtonState();
    }

    /**
     * Get HTML for ASN item row
     */
    getASNItemRowHtml(index) {
        return `
            <div class="row mb-2 asn-item-row" data-index="${index}">
                <div class="col-md-3">
                    <div class="item-search-container">
                        <div class="search-input-wrapper">
                            <input type="text" 
                                   class="form-control item-search-input" 
                                   id="itemSearchInput_${index}"
                                   name="items[${index}].itemSearch"
                                   placeholder="Search by item code or name..."
                                   autocomplete="off">
                            <div class="search-icon">🔍</div>
                        </div>
                        <div class="search-results-dropdown" id="itemSearchResults_${index}">
                            <!-- Results will appear here -->
                        </div>
                        <input type="hidden" class="asn-item-id" name="items[${index}].itemId" value="">
                    </div>
                </div>
                <div class="col-md-2">
                    <input type="number" class="form-control asn-ordered-qty" readonly placeholder="0">
                </div>
                <div class="col-md-2">
                    <input type="number" class="form-control asn-shipped-qty" name="items[${index}].shippedQuantity" min="1" required placeholder="0">
                </div>
                <div class="col-md-2">
                    <input type="number" class="form-control asn-unit-price" name="items[${index}].actualPricePerItem" step="0.01" min="0.01" required placeholder="0.00">
                </div>
                <div class="col-md-2">
                    <input type="text" class="form-control asn-total" readonly placeholder="Rp 0">
                </div>
                <div class="col-md-1">
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.asnManager.removeASNItem(${index})">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `;
    }

    /**
     * Initialize ASN item row event listeners
     */
    initializeASNItemRow(index) {
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (!row) return;

        // Initialize autocomplete for item search
        const searchInputId = `itemSearchInput_${index}`;
        const resultsId = `itemSearchResults_${index}`;
        const itemIdInput = row.querySelector('.asn-item-id');
        
        if (searchInputId && resultsId && itemIdInput) {
            // Initialize autocomplete
            if (window.asnItemAutocomplete) {
                window.asnItemAutocomplete.initialize(
                    searchInputId, 
                    resultsId, 
                    itemIdInput.id,
                    (selectedItem) => this.onASNItemSelect(index, selectedItem)
                );
            }
        }

        // Shipped quantity change
        const shippedQty = row.querySelector('.asn-shipped-qty');
        if (shippedQty) {
            shippedQty.addEventListener('input', () => {
                this.validateASNItemQuantity(index);
                this.updateASNItemTotal(index);
            });
        }

        // Unit price change
        const unitPrice = row.querySelector('.asn-unit-price');
        if (unitPrice) {
            unitPrice.addEventListener('input', () => {
                this.updateASNItemTotal(index);
            });
        }
    }

    /**
     * Initialize ASN item row event listeners for edit mode (only shipped quantity is editable)
     */
    initializeASNItemRowEditMode(index) {
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (!row) return;

        // Only handle shipped quantity change for edit mode
        const shippedQty = row.querySelector('.asn-shipped-qty');
        const unitPriceInput = row.querySelector('.asn-unit-price');
        const totalInput = row.querySelector('.asn-total');

        if (shippedQty && unitPriceInput && totalInput) {
            shippedQty.addEventListener('input', () => {
                this.validateASNItemQuantityEditMode(index);
                this.updateASNItemTotalEditMode(index);
            });
        }
    }

    /**
     * Handle ASN item selection from autocomplete
     */
    onASNItemSelect(index, selectedItem) {
        console.log('ASN Item selected:', selectedItem);
        
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (!row) return;

        // Update ordered quantity (from Purchase Order)
        const orderedQtyInput = row.querySelector('.asn-ordered-qty');
        if (orderedQtyInput) {
            // Find the item in the loaded items to get ordered quantity
            const item = this.items.find(item => item.itemId == selectedItem.id);
            if (item) {
                orderedQtyInput.value = item.orderedQuantity || 0;
            }
        }

        // Update unit price
        const unitPriceInput = row.querySelector('.asn-unit-price');
        if (unitPriceInput) {
            unitPriceInput.value = selectedItem.purchasePrice || 0;
        }

        // Set default shipped quantity to ordered quantity
        const shippedQtyInput = row.querySelector('.asn-shipped-qty');
        if (shippedQtyInput) {
            const orderedQty = orderedQtyInput.value || 0;
            shippedQtyInput.value = orderedQty;
            shippedQtyInput.max = orderedQty; // Set max to ordered quantity
        }

        // Update item ID in hidden input
        const itemIdInput = row.querySelector('.asn-item-id');
        if (itemIdInput) {
            itemIdInput.value = selectedItem.id;
            console.log('ASNManager: Set itemId to hidden input:', selectedItem.id);
        }

        // Update total
        this.updateASNItemTotal(index);

        // Track this item as selected
        if (!this.selectedItemIds.includes(selectedItem.id)) {
            this.selectedItemIds.push(selectedItem.id);
        }

        // Add to current ASN items
        const existingItemIndex = this.currentASNItems.findIndex(item => item.itemId == selectedItem.id);
        if (existingItemIndex === -1) {
            this.currentASNItems.push({
                itemId: selectedItem.id,
                itemCode: selectedItem.itemCode,
                itemName: selectedItem.name,
                orderedQuantity: orderedQtyInput.value || 0,
                shippedQuantity: shippedQtyInput.value || 0,
                unitPrice: selectedItem.purchasePrice || 0
            });
        }

        console.log('ASN Item added to current items:', this.currentASNItems);
    }

    /**
     * Handle ASN item selection change
     */
    onASNItemChange(index, itemId) {
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (!row) return;

        // Get the previously selected item id from this row
        const previouslySelectedId = row.dataset.selectedItemId;
        
        if (itemId) {
            // Remove previously selected item from the tracking list
            if (previouslySelectedId && this.selectedItemIds.includes(parseInt(previouslySelectedId))) {
                this.selectedItemIds = this.selectedItemIds.filter(id => id !== parseInt(previouslySelectedId));
            }
            
            // Add new item to tracking list
            const newItemId = parseInt(itemId);
            if (!this.selectedItemIds.includes(newItemId)) {
                this.selectedItemIds.push(newItemId);
            }
            
            // Update row data attribute
            row.dataset.selectedItemId = itemId;
            
            
            const item = this.items.find(i => i.itemId == itemId);
            if (item) {
                const orderedQty = row.querySelector('.asn-ordered-qty');
                const unitPrice = row.querySelector('.asn-unit-price');
                
                if (orderedQty) orderedQty.value = item.orderedQuantity;
                if (unitPrice) unitPrice.value = item.unitPrice;
                
                this.updateASNItemTotal(index);
            }
        } else {
            // Item was deselected
            if (previouslySelectedId && this.selectedItemIds.includes(parseInt(previouslySelectedId))) {
                this.selectedItemIds = this.selectedItemIds.filter(id => id !== parseInt(previouslySelectedId));
                row.dataset.selectedItemId = '';
            }
        }
    }

    /**
     * Validate ASN item quantity (shipped not exceed ordered)
     */
    validateASNItemQuantity(index) {
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (!row) return;

        const orderedQty = row.querySelector('.asn-ordered-qty');
        const shippedQty = row.querySelector('.asn-shipped-qty');
        
        if (orderedQty && shippedQty) {
            const ordered = parseInt(orderedQty.value) || 0;
            const shipped = parseInt(shippedQty.value) || 0;
            
            if (shipped > ordered) {
                shippedQty.setCustomValidity(`Shipped quantity cannot exceed ordered quantity (${ordered})`);
                shippedQty.classList.add('is-invalid');
            } else {
                shippedQty.setCustomValidity('');
                shippedQty.classList.remove('is-invalid');
            }
        }
    }

    /**
     * Update ASN item total
     */
    updateASNItemTotal(index) {
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (!row) return;

        const shippedQty = row.querySelector('.asn-shipped-qty');
        const unitPrice = row.querySelector('.asn-unit-price');
        const total = row.querySelector('.asn-total');

        if (shippedQty && unitPrice && total) {
            const qty = parseFloat(shippedQty.value) || 0;
            const price = parseFloat(unitPrice.value) || 0;
            const itemTotal = qty * price;
            
            total.value = new Intl.NumberFormat('id-ID', { 
                style: 'currency', 
                currency: 'IDR' 
            }).format(itemTotal);
        }

        this.updateASNSummary();
    }

    /**
     * Validate ASN item quantity for edit mode (shipped not exceed ordered)
     */
    validateASNItemQuantityEditMode(index) {
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (!row) return;

        const orderedQty = row.querySelector('.asn-ordered-qty');
        const shippedQty = row.querySelector('.asn-shipped-qty');
        
        if (orderedQty && shippedQty) {
            const ordered = parseInt(orderedQty.value) || 0;
            const shipped = parseInt(shippedQty.value) || 0;
            
            if (shipped > ordered) {
                shippedQty.setCustomValidity(`Shipped quantity cannot exceed ordered quantity (${ordered})`);
                shippedQty.classList.add('is-invalid');
            } else {
                shippedQty.setCustomValidity('');
                shippedQty.classList.remove('is-invalid');
            }
        }
    }

    /**
     * Update ASN item total for edit mode
     */
    updateASNItemTotalEditMode(index) {
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (!row) return;

        const shippedQty = row.querySelector('.asn-shipped-qty');
        const unitPrice = row.querySelector('.asn-unit-price');
        const total = row.querySelector('.asn-total');

        if (shippedQty && unitPrice && total) {
            const qty = parseFloat(shippedQty.value) || 0;
            const price = parseFloat(unitPrice.value) || 0;
            const itemTotal = qty * price;
            
            total.value = `Rp ${itemTotal.toLocaleString('id-ID')}`;
        }

        this.updateASNSummary();
    }


    /**
     * Remove ASN item
     */
    removeASNItem(index) {
        const row = document.querySelector(`.asn-item-row[data-index="${index}"]`);
        if (row) {
            // Remove selected item from tracking list if it exists
            const itemIdInput = row.querySelector('.asn-item-id');
            if (itemIdInput && itemIdInput.value) {
                this.selectedItemIds = this.selectedItemIds.filter(id => id !== parseInt(itemIdInput.value));
            }
            
            // Remove from current ASN items
            const itemId = itemIdInput ? parseInt(itemIdInput.value) : null;
            if (itemId) {
                this.currentASNItems = this.currentASNItems.filter(item => item.itemId !== itemId);
            }
            
            row.remove();
            this.updateASNSummary();
            
            // Update button state after removing item
            this.updateAddItemButtonState();
        }
    }

    /**
     * Update ASN items container display
     */
    updateASNItemsContainer() {
        const container = document.getElementById('asnItemsContainer');
        if (container) {
            container.innerHTML = '';
        }
        this.currentASNItems = [];
        this.selectedItemIds = []; // Reset selected items tracking
    }

    /**
     * Update ASN summary
     */
    updateASNSummary() {
        const rows = document.querySelectorAll('.asn-item-row');
        const itemCount = rows.length;
        
        const totalItemsEl = document.getElementById('asnTotalItems');
        const itemsCountEl = document.getElementById('asnItemsCount');
        
        if (totalItemsEl) {
            totalItemsEl.textContent = itemCount;
        }
        if (itemsCountEl) {
            itemsCountEl.textContent = `${itemCount} items`;
        }
    }

    /**
     * Populate existing ASN items for edit mode
     */
    async populateASNItems(asinDetails, purchaseOrderId) {
        const container = document.getElementById('asnItemsContainer');
        if (!container) return;

        // Clear container first
        container.innerHTML = '';
        this.currentASNItems = [];
        this.selectedItemIds = [];

        if (asinDetails && asinDetails.length > 0) {
            // Set max allowed items based on ASN details
            this.maxAllowedItems = asinDetails.length;
            
            // Always load items from the selected Purchase Order to ensure we have the latest data
            if (purchaseOrderId) {
                await this.loadItemsForPurchaseOrder(purchaseOrderId);
            }

            // Add existing ASN items to the container
            for (let i = 0; i < asinDetails.length; i++) {
                const detail = asinDetails[i];
                await this.addExistingASNItem(detail, i);
            }

            this.updateASNSummary();
            this.updateAddItemButtonState(); // Update button state
        }
    }

    /**
     * Add existing ASN item row for edit mode
     */
    async addExistingASNItem(detail, index) {
        const container = document.getElementById('asnItemsContainer');
        if (!container) return;

        // Find the item in our loaded items to get full details
        const item = this.items.find(item => item.itemId === detail.itemId);
        
        // If item not found in loaded items, use detail data directly
        const orderedQuantity = item ? item.orderedQuantity : 0;
        const itemCode = detail.itemCode || 'Unknown';
        const itemName = detail.itemName || 'Unknown Item';

        // Generate HTML for the item row - make item selection readonly in edit mode
        const totalAmount = detail.shippedQuantity * detail.actualPricePerItem;
        const rowHtml = `
            <div class="row mb-2 asn-item-row" data-index="${index}">
                <div class="col-md-3">
                    <input type="text" class="form-control" value="${itemCode} - ${itemName}" readonly style="background-color: #f8f9fa;">
                    <input type="hidden" class="asn-item-id" name="items[${index}].itemId" value="${detail.itemId}">
                </div>
                <div class="col-md-2">
                    <input type="number" class="form-control asn-ordered-qty" readonly value="${orderedQuantity}" style="background-color: #f8f9fa;">
                </div>
                <div class="col-md-2">
                    <input type="number" class="form-control asn-shipped-qty" name="items[${index}].shippedQuantity" min="1" required value="${detail.shippedQuantity}">
                </div>
                <div class="col-md-2">
                    <input type="number" class="form-control asn-unit-price" name="items[${index}].actualPricePerItem" step="0.01" min="0.01" required value="${detail.actualPricePerItem}" readonly style="background-color: #f8f9fa;">
                </div>
                <div class="col-md-2">
                    <input type="text" class="form-control asn-total" readonly value="Rp ${totalAmount.toLocaleString('id-ID')}" style="background-color: #f8f9fa;">
                </div>
                <div class="col-md-1">
                    <!-- Remove delete button in edit mode -->
                </div>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', rowHtml);

        // Initialize only the shipped quantity listener for calculation
        this.initializeASNItemRowEditMode(index);

        // Track this item as selected
        this.selectedItemIds.push(detail.itemId);
        this.currentASNItems.push(detail);
    }

    /**
     * Show create modal
     */
    async showCreateModal() {
        try {
            // Reset form state (sudah termasuk restorePurchaseOrderDropdown dan show Add Item button)
            this.resetForm();
            
            // Update modal title untuk create mode
            const modalTitle = document.getElementById('asnModalTitle');
            if (modalTitle) {
                modalTitle.textContent = 'Create New ASN';
            }
            
            // Load Purchase Orders setelah form ter-reset
            await this.loadPurchaseOrders();
            
            // Load Locations untuk holding location
            await this.loadLocations();
            
            const modalElement = document.getElementById('asnModal');
            if (modalElement) {
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
            } else {
                console.error('ASNManager: Modal element not found');
                this.showError('Modal not available');
            }
        } catch (error) {
            console.error('ASNManager: Error showing create modal:', error);
            this.showError('Error opening create modal');
        }
    }

    /**
     * View ASN details
     */
    async viewASN(id) {
        try {
            if (!id || id <= 0) {
                this.showError('Invalid ASN ID');
                return;
            }

            // Navigate to Details page instead of opening modal
            window.location.href = `/ASN/Details/${id}`;
        } catch (error) {
            console.error('Error navigating to ASN details:', error);
            this.showError('Error navigating to ASN details');
        }
    }

    /**
     * Edit ASN
     */
    async editASN(id) {
        try {
            const response = await fetch(`/api/asn/${id}`);
            const result = await response.json();

            if (result.success) {
                const asn = result.data;
                
                // Set currentASNId first untuk populateForm bisa detect edit mode
                this.currentASNId = id;
                
                // Hide Add Item button for edit mode
                const addItemBtn = document.getElementById('addASNItemBtn');
                if (addItemBtn) {
                    addItemBtn.style.display = 'none';
                }
                
                // Load Locations untuk holding location dropdown (PENTING untuk fix bug blank!)
                await this.loadLocations();
                
                // Populate basic form fields (Purchase Order akan jadi static)
                this.populateForm(asn);
                
                // Set holding location value setelah locations loaded
                if (asn.holdingLocationId) {
                    this.setElementValue('holdingLocationId', asn.holdingLocationId);
                }
                
                // Load existing ASN items (tidak perlu load PO dropdown karena sudah static)
                if (asn.details && asn.details.length > 0) {
                    await this.populateASNItems(asn.details, asn.purchaseOrderId);
                }
                
                document.getElementById('asnModalTitle').textContent = 'Edit ASN';
                const modal = new bootstrap.Modal(document.getElementById('asnModal'));
                modal.show();
            } else {
                this.showError(result.message || 'Failed to load ASN for editing');
            }
        } catch (error) {
            console.error('Error loading ASN for editing:', error);
            this.showError('Error loading ASN for editing');
        }
    }

    /**
     * Save ASN (create or update)
     */
    async saveASN() {
        try {
            const formData = new FormData(document.getElementById('asnForm'));
            
            // Collect items data
            const items = [];
            const itemRows = document.querySelectorAll('.asn-item-row');
            
            itemRows.forEach((row, index) => {
                const itemSelect = row.querySelector('.asn-item-select');
                const itemIdInput = row.querySelector('.asn-item-id');
                const shippedQty = row.querySelector('.asn-shipped-qty');
                const unitPrice = row.querySelector('.asn-unit-price');
                const notes = row.querySelector('.asn-notes') || { value: '' };
                
                // For edit mode, use hidden input; for create mode, use select
                const itemId = itemIdInput ? itemIdInput.value : (itemSelect ? itemSelect.value : null);
                
                console.log(`ASNManager: Row ${index} - itemId: ${itemId}, shippedQty: ${shippedQty?.value}, unitPrice: ${unitPrice?.value}`);
                
                if (itemId && shippedQty && shippedQty.value && unitPrice && unitPrice.value) {
                    items.push({
                        itemId: parseInt(itemId),
                        shippedQuantity: parseInt(shippedQty.value),
                        actualPricePerItem: parseFloat(unitPrice.value),
                        notes: notes.value || null
                    });
                }
            });

            // Validate items
            console.log('ASNManager: Collected items for validation:', items);
            if (items.length === 0) {
                this.showError('Please add at least one item to the ASN');
                return;
            }

            // Validate and parse dates
            const expectedArrivalDateStr = formData.get('expectedArrivalDate');
            const shipmentDateStr = formData.get('shipmentDate');

            if (!expectedArrivalDateStr) {
                this.showError('Please enter expected arrival date');
                return;
            }

            const holdingLocationId = formData.get('holdingLocationId');
            if (!holdingLocationId) {
                this.showError('Please select a holding location');
                return;
            }

            // Validate holding location capacity
            const totalQuantity = items.reduce((sum, item) => sum + item.shippedQuantity, 0);
            const holdingLocationSelect = document.getElementById('holdingLocationId');
            const selectedOption = holdingLocationSelect.options[holdingLocationSelect.selectedIndex];
            
            if (selectedOption && selectedOption.textContent.includes('Available:')) {
                const availableCapacityMatch = selectedOption.textContent.match(/Available: (\d+)/);
                if (availableCapacityMatch) {
                    const availableCapacity = parseInt(availableCapacityMatch[1]);
                    if (availableCapacity < totalQuantity) {
                        this.showError(`Insufficient capacity in holding location. Available: ${availableCapacity}, Required: ${totalQuantity}`);
                        return;
                    }
                }
            }

            const expectedArrivalDate = new Date(expectedArrivalDateStr);
            if (isNaN(expectedArrivalDate.getTime())) {
                this.showError('Please enter a valid expected arrival date');
                return;
            }

            let shipmentDate = null;
            if (shipmentDateStr) {
                const parsedShipmentDate = new Date(shipmentDateStr);
                if (isNaN(parsedShipmentDate.getTime())) {
                    this.showError('Please enter a valid shipment date or leave it empty');
                    return;
                }
                shipmentDate = parsedShipmentDate;
            }

            const data = {
                purchaseOrderId: parseInt(formData.get('purchaseOrderId')),
                expectedArrivalDate: expectedArrivalDate.toISOString(),
                shipmentDate: shipmentDate ? shipmentDate.toISOString() : null,
                carrierName: formData.get('carrierName'),
                // trackingNumber will be auto-generated on server side
                notes: formData.get('notes'),
                holdingLocationId: parseInt(formData.get('holdingLocationId')),
                items: items.map(item => ({
                    itemId: item.itemId,
                    shippedQuantity: item.shippedQuantity,
                    actualPricePerItem: parseFloat(item.actualPricePerItem),
                    notes: item.notes
                }))
            };

            // Validate required fields
            if (!data.purchaseOrderId) {
                this.showError('Please select a Purchase Order');
                return;
            }

            const url = this.currentASNId ? `/api/asn/${this.currentASNId}` : '/api/asn';
            const method = this.currentASNId ? 'PUT' : 'POST';

            console.log('Sending ASN data:', data); // Debug log

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data)
            });

            console.log('ASN API response status:', response.status); // Debug log

            if (!response.ok) {
                let errorMessage = 'Failed to save ASN';
                try {
                    const errorData = await response.json();
                    console.log('ASN API error response:', errorData); // Debug log
                    errorMessage = errorData.message || errorMessage;
                    
                    // Handle validation errors
                    if (errorData.errors && typeof errorData.errors === 'object') {
                        const validationErrors = [];
                        for (const [key, value] of Object.entries(errorData.errors)) {
                            if (value.errors && Array.isArray(value.errors)) {
                                validationErrors.push(...value.errors.map(e => e.errorMessage || e));
                            }
                        }
                        if (validationErrors.length > 0) {
                            errorMessage = 'Validation errors: ' + validationErrors.join(', ');
                        }
                    }
                } catch (jsonError) {
                    console.warn('Could not parse error response as JSON:', jsonError);
                    errorMessage = `Server error: ${response.status} ${response.statusText}`;
                }
                throw new Error(errorMessage);
            }

            const result = await response.json();
            console.log('ASN API success response:', result); // Debug log

            if (result.success) {
                this.showSuccess(result.message || 'ASN saved successfully');
                this.resetForm();
                const modalElement = document.getElementById('asnModal');
                if (modalElement) {
                    const modalInstance = bootstrap.Modal.getInstance(modalElement);
                    if (modalInstance) {
                        modalInstance.hide();
                    }
                }
                await this.loadASNs();
                await this.loadDashboard();
            } else {
                this.showError(result.message || 'Failed to save ASN');
            }
        } catch (error) {
            console.error('Error saving ASN:', error);
            // Display more specific error message
            const errorMessage = error.message || 'Error creating ASN';
            this.showError(errorMessage);
        }
    }

    /**
     * Update ASN status
     */
    async updateStatus(id, currentStatus) {
        const newStatus = this.getNextStatus(currentStatus);
        
        // Show confirmation dialog
        const confirmMessage = this.getStatusConfirmMessage(currentStatus, newStatus);
        if (!confirm(confirmMessage)) {
            return; // User cancelled
        }
        
        try {
            const response = await fetch(`/api/asn/${id}/status`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ status: newStatus })
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message || 'ASN status updated successfully');
                await this.loadASNs();
                await this.loadDashboard();
            } else {
                this.showError(result.message || 'Failed to update ASN status');
            }
        } catch (error) {
            console.error('Error updating ASN status:', error);
            this.showError('Error updating ASN status');
        }
    }


    /**
     * Populate form with ASN data
     */
    populateForm(asn) {
        // Set Purchase Order sebagai text input static untuk edit mode
        const poSelect = document.getElementById('purchaseOrderId');
        if (poSelect) {
            if (this.currentASNId) { // Jika sedang edit mode
                // Hanya ganti select element, bukan seluruh container agar label tetap ada
                const selectParent = poSelect.parentElement;
                
                // PENTING: Hide/disable button advanced search saat edit mode
                const poAdvancedSearchBtn = document.getElementById('poAdvancedSearchBtn');
                if (poAdvancedSearchBtn) {
                    poAdvancedSearchBtn.style.display = 'none'; // Hide button
                }
                
                // Buat input baru untuk display
                const displayInput = document.createElement('input');
                displayInput.type = 'text';
                displayInput.className = 'form-control';
                displayInput.id = 'purchaseOrderIdDisplay';
                displayInput.value = asn.purchaseOrderNumber || '';
                displayInput.readOnly = true;
                displayInput.style.backgroundColor = '#f8f9fa';
                
                // Ganti select dengan input display
                poSelect.replaceWith(displayInput);
                
                // Tambahkan hidden input untuk form submission
                const hiddenInput = document.createElement('input');
                hiddenInput.type = 'hidden';
                hiddenInput.name = 'purchaseOrderId';
                hiddenInput.value = asn.purchaseOrderId || '';
                
                selectParent.appendChild(hiddenInput);
            } else {
                // Create mode - pastikan button advanced search terlihat
                const poAdvancedSearchBtn = document.getElementById('poAdvancedSearchBtn');
                if (poAdvancedSearchBtn) {
                    poAdvancedSearchBtn.style.display = 'inline-block'; // Show button
                }
                this.setElementValue('purchaseOrderId', asn.purchaseOrderId);
            }
        }
        
        this.setElementValue('supplierName', asn.supplierName);
        this.setElementValue('supplierContact', asn.supplierContact);
        // Fix timezone issue for date fields - use local timezone formatting
        this.setElementValue('expectedArrivalDate', this.formatDateForInput(asn.expectedArrivalDate));
        this.setElementValue('shipmentDate', this.formatDateForInput(asn.shipmentDate));
        this.setElementValue('carrierName', asn.carrierName);
        this.setElementValue('trackingNumber', asn.trackingNumber);
        this.setElementValue('notes', asn.notes);
    }

    /**
     * Format date for HTML input type="date" in local timezone
     * Fixes timezone conversion issues that cause date to shift by 1 day
     * 
     * Problem: Database stores dates in UTC, but new Date().toISOString().split('T')[0] 
     * causes timezone conversion issues when displaying in local timezone input fields.
     * 
     * Solution: Use local date methods (getFullYear(), getMonth(), getDate()) instead 
     * of UTC methods to avoid timezone shift.
     */
    formatDateForInput(dateString) {
        if (!dateString) return '';
        
        try {
            // Create date object from the string (can be ISO string from database)
            const date = new Date(dateString);
            
            // Check if date is valid
            if (isNaN(date.getTime())) {
                console.warn('Invalid date string provided:', dateString);
                return '';
            }
            
            // Use local timezone methods to avoid timezone shift issues
            // This ensures the date displayed matches what user expects
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            
            return `${year}-${month}-${day}`;
        } catch (error) {
            console.error('Error formatting date for input:', error, 'Input:', dateString);
            return '';
        }
    }

    /**
     * Populate view modal with ASN data
     */
    populateViewModal(asn) {
        document.getElementById('viewASNNumber').textContent = asn.asnNumber;
        document.getElementById('viewPONumber').textContent = asn.purchaseOrderNumber;
        document.getElementById('viewSupplierName').textContent = asn.supplierName;
        document.getElementById('viewSupplierContact').textContent = asn.supplierContact || 'N/A';
        document.getElementById('viewStatus').innerHTML = this.getStatusBadge(asn.status);
        document.getElementById('viewExpectedDate').textContent = new Date(asn.expectedArrivalDate).toLocaleDateString();
        document.getElementById('viewActualDate').textContent = asn.actualArrivalDate ? new Date(asn.actualArrivalDate).toLocaleDateString() : 'N/A';
        document.getElementById('viewShipmentDate').textContent = asn.shipmentDate ? new Date(asn.shipmentDate).toLocaleDateString() : 'N/A';
        document.getElementById('viewCarrierName').textContent = asn.carrierName || 'N/A';
        document.getElementById('viewTrackingNumber').textContent = asn.trackingNumber || 'N/A';
        document.getElementById('viewNotes').textContent = asn.notes || 'N/A';
        document.getElementById('viewCreatedDate').textContent = new Date(asn.createdDate).toLocaleString();
        document.getElementById('viewCreatedBy').textContent = asn.createdBy || 'N/A';
    }


    /**
     * Restore Purchase Order dropdown for create mode
     * Solusi permanen yang membersihkan semua state dari edit mode
     */
    restorePurchaseOrderDropdown() {
        // Cari input-group container yang berisi Purchase Order field
        // PENTING: Gunakan beberapa cara untuk menemukan input-group karena di edit mode
        // #purchaseOrderId sudah diganti dengan purchaseOrderIdDisplay
        let inputGroup = null;
        
        // Cara 1: Cari melalui #purchaseOrderId (jika masih ada, berarti create mode)
        inputGroup = document.querySelector('#purchaseOrderId')?.closest('.input-group');
        
        // Cara 2: Cari melalui purchaseOrderIdDisplay (jika ada, berarti edit mode)
        if (!inputGroup) {
            inputGroup = document.querySelector('#purchaseOrderIdDisplay')?.closest('.input-group');
        }
        
        // Cara 3: Cari melalui poAdvancedSearchBtn
        if (!inputGroup) {
            inputGroup = document.querySelector('#poAdvancedSearchBtn')?.closest('.input-group');
        }
        
        // Cara 4: Cari melalui label
        if (!inputGroup) {
            const label = document.querySelector('label[for="purchaseOrderId"]');
            if (label) {
                const container = label.closest('.mb-3');
                if (container) {
                    inputGroup = container.querySelector('.input-group');
                }
            }
        }
        
        if (!inputGroup) {
            console.warn('ASNManager: Purchase Order input-group not found');
            return;
        }
        
        // PENTING: Bersihkan semua elemen yang mungkin ada dari edit mode
        // 1. Bersihkan display input (purchaseOrderIdDisplay) jika ada
        const displayInput = inputGroup.querySelector('#purchaseOrderIdDisplay');
        if (displayInput) {
            displayInput.remove();
            console.log('ASNManager: Removed purchaseOrderIdDisplay from edit mode');
        }
        
        // 2. Bersihkan hidden input jika ada (dari edit mode)
        const hiddenInputs = inputGroup.querySelectorAll('input[type="hidden"][name="purchaseOrderId"]');
        hiddenInputs.forEach(input => {
            input.remove();
            console.log('ASNManager: Removed hidden input from edit mode');
        });
        
        // 3. Bersihkan select element yang ada (jika ada)
        const existingSelect = inputGroup.querySelector('#purchaseOrderId');
        if (existingSelect) {
            // Hapus event listeners sebelum remove
            if (existingSelect.removeEventListener) {
                existingSelect.removeEventListener('change', this.onPurchaseOrderChange);
            }
            existingSelect.remove();
        }
        
        // Buat select element baru
        const selectElement = document.createElement('select');
        selectElement.className = 'form-select';
        selectElement.id = 'purchaseOrderId';
        selectElement.name = 'purchaseOrderId';
        selectElement.required = true;
        
        // Tambahkan default option sesuai HTML asli
        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = '-- Select Purchase Order --';
        selectElement.appendChild(defaultOption);
        
        // Insert select element di dalam input-group, sebelum tombol search
        const searchButton = inputGroup.querySelector('#poAdvancedSearchBtn');
        if (searchButton) {
            inputGroup.insertBefore(selectElement, searchButton);
        } else {
            // Jika tombol search tidak ada, tambahkan di akhir input-group
            inputGroup.appendChild(selectElement);
        }
        
        // Re-attach event listener untuk handle perubahan Purchase Order
        selectElement.addEventListener('change', (e) => {
            this.onPurchaseOrderChange(e.target.value);
        });
        
        console.log('ASNManager: Purchase Order dropdown restored for create mode within input-group');
    }

    /**
     * Pagination methods
     */
    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadASNs();
        }
    }

    nextPage() {
        this.currentPage++;
        this.loadASNs();
    }

    /**
     * Debounced search
     */
    debounceSearch() {
        clearTimeout(this.searchTimeout);
        this.searchTimeout = setTimeout(() => {
            this.currentPage = 1;
            this.loadASNs();
        }, 500);
    }

    /**
     * Reset form
     */
    resetForm() {
        const form = document.getElementById('asnForm');
        if (form) {
            form.reset();
        }
        
        // Reset current ASN ID FIRST untuk memastikan state clean
        this.currentASNId = null;
        this.selectedPurchaseOrderId = null; // Reset selected PO ID
        
        // Restore Purchase Order dropdown untuk memastikan DOM clean dari edit mode
        this.restorePurchaseOrderDropdown();
        
        // Reset Purchase Order selection and related fields
        this.setElementValue('supplierName', '');
        
        // Clear items container
        this.updateASNItemsContainer();
        
        // Reset data arrays
        this.items = [];
        this.currentASNItems = [];
        this.selectedItemIds = []; // Reset selected items tracking
        
        // Reset summary
        this.updateASNSummary();
        
        // Show Add Item button when resetting form (untuk create mode)
        const addItemBtn = document.getElementById('addASNItemBtn');
        if (addItemBtn) {
            addItemBtn.style.display = 'inline-block';
        }
        
        // PENTING: Restore button advanced search untuk create mode
        const poAdvancedSearchBtn = document.getElementById('poAdvancedSearchBtn');
        if (poAdvancedSearchBtn) {
            poAdvancedSearchBtn.style.display = 'inline-block'; // Show button
        }
        
        // Clear messages
        this.clearError();
        this.clearSuccess();
    }

    /**
     * Utility methods
     */
    getStatusBadge(status) {
        const statusMap = {
            'Pending': 'badge bg-warning',
            'On Delivery': 'badge bg-info',
            'Arrived': 'badge bg-success',
            'Processed': 'badge bg-primary',
            'Cancelled': 'badge bg-danger'
        };
        return `<span class="${statusMap[status] || 'badge bg-secondary'}">${status}</span>`;
    }

    getNextStatus(currentStatus) {
        const statusFlow = {
            'Pending': 'On Delivery',
            'On Delivery': 'Arrived',
            'Arrived': 'Processed'
        };
        return statusFlow[currentStatus] || 'Pending';
    }

    getStatusButtonClass(status) {
        const classMap = {
            'Pending': 'btn-outline-warning',
            'On Delivery': 'btn-outline-info',
            'Arrived': 'btn-outline-success',
            'Processed': 'btn-outline-secondary'
        };
        return classMap[status] || 'btn-outline-warning';
    }

    getStatusIcon(status) {
        const iconMap = {
            'Pending': 'fa-clock',
            'On Delivery': 'fa-truck',
            'Arrived': 'fa-check-circle',
            'Processed': 'fa-check-double'
        };
        return iconMap[status] || 'fa-clock';
    }

    /**
     * Get confirmation message for status change
     */
    getStatusConfirmMessage(currentStatus, newStatus) {
        const messages = {
            'Pending': 'Are you sure you want to mark this ASN as "On Delivery"? The shipment will start tracking.',
            'On Delivery': 'Are you sure you want to mark this ASN as "Arrived"? The shipment has reached the warehouse.',
            'Arrived': 'Are you sure you want to mark this ASN as "Processed"? This will complete the ASN processing.'
        };
        return messages[newStatus] || `Are you sure you want to change status from "${currentStatus}" to "${newStatus}"?`;
    }

    showError(message) {
        const errorElement = document.getElementById('errorMessage');
        if (errorElement) {
            errorElement.innerHTML = `<i class="fas fa-exclamation-triangle me-2"></i>${message}`;
            errorElement.classList.remove('d-none');
        }
        console.error('Error:', message);
    }

    showSuccess(message) {
        const successElement = document.getElementById('successMessage');
        if (successElement) {
            successElement.innerHTML = `<i class="fas fa-check-circle me-2"></i>${message}`;
            successElement.classList.remove('d-none');
            setTimeout(() => this.clearSuccess(), 3000);
        }
    }

    clearError() {
        const errorElement = document.getElementById('errorMessage');
        if (errorElement) {
            errorElement.classList.add('d-none');
            errorElement.innerHTML = '';
        }
    }

    clearSuccess() {
        const successElement = document.getElementById('successMessage');
        if (successElement) {
            successElement.classList.add('d-none');
            successElement.innerHTML = '';
        }
    }
}

// Global functions for onclick handlers
function previousPage() {
    if (window.asnManager) {
        window.asnManager.previousPage();
    }
}

function nextPage() {
    if (window.asnManager) {
        window.asnManager.nextPage();
    }
}

function changePageSize() {
    if (window.asnManager) {
        const pageSizeSelect = document.getElementById('pageSizeSelect');
        if (pageSizeSelect) {
            window.asnManager.pageSize = parseInt(pageSizeSelect.value);
            window.asnManager.currentPage = 1;
            window.asnManager.loadASNs();
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    try {
        window.asnManager = new ASNManager();
        window.asnPOManager = window.asnManager.asnPOManager; // Make it globally accessible
        window.asnLocationManager = window.asnManager.asnLocationManager; // Make it globally accessible
        window.asnItemAutocomplete = window.asnManager.asnItemAutocomplete; // Make it globally accessible
        console.log('ASNManager: Global instance created');
        console.log('ASNPurchaseOrderManager: Global instance created');
        console.log('ASNLocationManager: Global instance created');
        console.log('ASNItemAutocomplete: Global instance created');
    } catch (error) {
        console.error('Failed to initialize ASN Manager:', error);
    }
});

