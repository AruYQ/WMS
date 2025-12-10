/**
 * Sales Order Manager - AJAX-based Sales Order CRUD operations
 */
class SalesOrderManager {
    constructor() {
        this.currentSO = null;
        this.currentSOId = null;
        this.currentPage = 1;
        this.pageSize = 10;
        this.currentStatus = null;
        this.currentSearch = '';
        this.currentSOItems = []; // Items being added to SO
        this.selectedItemIds = []; // Track which items are already selected (prevent duplicates)
        
        this.init();
    }

    async init() {
        try {
            await this.loadDashboard();
            await this.loadSalesOrders();
            await this.loadCustomers();
            await this.loadLocations();
            this.bindEvents();
        } catch (error) {
            console.error('SalesOrderManager: Initialization failed:', error);
        }
    }

    bindEvents() {
        // Search and filter
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            let searchTimeout;
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    this.currentSearch = e.target.value;
                    this.currentPage = 1;
                    this.loadSalesOrders();
                }, 300);
            });
        }

        const statusFilter = document.getElementById('statusFilter');
        if (statusFilter) {
            statusFilter.addEventListener('change', (e) => {
                this.currentStatus = e.target.value;
                this.currentPage = 1;
                this.loadSalesOrders();
            });
        }

        const pageSizeSelect = document.getElementById('pageSizeSelect');
        if (pageSizeSelect) {
            pageSizeSelect.addEventListener('change', (e) => {
                this.pageSize = parseInt(e.target.value);
                this.currentPage = 1;
                this.loadSalesOrders();
            });
        }

        // Create button
        const createSOBtn = document.getElementById('createSOBtn');
        if (createSOBtn) {
            createSOBtn.addEventListener('click', () => this.showCreateModal());
        }

        // Form submission
        const soForm = document.getElementById('soForm');
        if (soForm) {
            soForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.saveSalesOrder();
            });
        }

        // Add Item button
        const addSOItemBtn = document.getElementById('addSOItemBtn');
        if (addSOItemBtn) {
            addSOItemBtn.addEventListener('click', () => this.addSOItem());
        }

        // Edit Add Item button
        const editAddSOItemBtn = document.getElementById('editAddSOItemBtn');
        if (editAddSOItemBtn) {
            editAddSOItemBtn.addEventListener('click', () => this.addSOItem('edit'));
        }

        // Edit form submission
        const editSOForm = document.getElementById('editSOForm');
        if (editSOForm) {
            editSOForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.updateSalesOrder();
            });
        }

        // Modal events
        const soModal = document.getElementById('soModal');
        if (soModal) {
            soModal.addEventListener('hidden.bs.modal', () => this.resetForm());
        }

        const editSOModal = document.getElementById('editSOModal');
        if (editSOModal) {
            editSOModal.addEventListener('hidden.bs.modal', () => this.resetEditForm());
        }

        // Customer and Location Advanced Search (Create)
        const customerSearchBtn = document.getElementById('customerAdvancedSearchBtn');
        if (customerSearchBtn && window.salesOrderCustomerManager) {
            customerSearchBtn.addEventListener('click', () => window.salesOrderCustomerManager.openAdvancedSearch());
        }

        const locationSearchBtn = document.getElementById('locationAdvancedSearchBtn');
        if (locationSearchBtn && window.salesOrderLocationManager) {
            locationSearchBtn.addEventListener('click', () => window.salesOrderLocationManager.openAdvancedSearch());
        }

        // Location Advanced Search (Edit) - Customer tidak bisa diubah
        const editLocationSearchBtn = document.getElementById('editLocationAdvancedSearchBtn');
        if (editLocationSearchBtn && window.salesOrderLocationManager) {
            editLocationSearchBtn.addEventListener('click', () => window.salesOrderLocationManager.openAdvancedSearch('edit'));
        }
    }

    async loadDashboard() {
        try {
            const response = await fetch('/api/salesorder/dashboard');
            const result = await response.json();
            if (result.success) {
                const data = result.data;
                document.getElementById('totalSOs')?.setAttribute('textContent', data.totalSOs || 0);
                document.getElementById('totalSOs').textContent = data.totalSOs || 0;
                document.getElementById('pendingSOs').textContent = data.pendingSOs || 0;
                document.getElementById('inProgressSOs').textContent = data.inProgressSOs || 0;
                document.getElementById('pickedSOs').textContent = data.pickedSOs || 0;
                document.getElementById('shippedSOs').textContent = data.shippedSOs || 0;
            }
        } catch (error) {
            console.error('Error loading dashboard:', error);
        }
    }

    async loadSalesOrders() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                ...(this.currentStatus && { status: this.currentStatus }),
                ...(this.currentSearch && { search: this.currentSearch })
            });

            const response = await fetch(`/api/salesorder?${params}`);
            const result = await response.json();

            if (result.success) {
                this.renderSalesOrdersTable(result.data);
                if (result.pagination) {
                    this.updatePagination(result.pagination);
                }
            } else {
                this.showError(result.message || 'Failed to load Sales Orders');
                this.renderSalesOrdersTable([]);
            }
        } catch (error) {
            console.error('Error loading Sales Orders:', error);
            this.showError('Error loading Sales Orders');
            this.renderSalesOrdersTable([]);
        }
    }

    renderSalesOrdersTable(salesOrders) {
        const container = document.getElementById('salesOrdersTableContainer');
        if (!container) return;

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead class="table-light">
                        <tr>
                            <th>SO Number</th>
                            <th>Customer</th>
                            <th>Order Date</th>
                            <th>Expected Arrival</th>
                            <th>Status</th>
                            <th>Total Amount</th>
                            <th>Items</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        if (salesOrders && salesOrders.length > 0) {
            salesOrders.forEach(so => {
                const statusBadge = this.getStatusBadge(so.status);
                const orderDate = new Date(so.orderDate).toLocaleString();
                const expectedDate = so.requiredDate ? new Date(so.requiredDate).toLocaleDateString() : '-';
                
                tableHtml += `
                    <tr>
                        <td><strong>${so.soNumber}</strong></td>
                        <td>${so.customerName || 'N/A'}</td>
                        <td>${orderDate}</td>
                        <td>${expectedDate}</td>
                        <td>${statusBadge}</td>
                        <td>Rp ${parseFloat(so.totalAmount).toLocaleString('id-ID')}</td>
                        <td><span class="badge bg-info">${so.totalItems}</span></td>
                        <td>
                            <div class="btn-group" role="group">
                                <button type="button" class="btn btn-sm btn-outline-primary" onclick="salesOrderManager.viewSalesOrder(${so.id})" title="View">
                                    <i class="fas fa-eye"></i>
                                </button>
                                ${so.status === 'Pending' ? `
                                    <button type="button" class="btn btn-sm btn-outline-warning" onclick="salesOrderManager.editSalesOrder(${so.id})" title="Edit">
                                        <i class="fas fa-edit"></i>
                                    </button>
                                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.showCancelSOModal(${so.id}, '${so.soNumber}', false)" title="Cancel">
                                        <i class="fas fa-ban"></i>
                                    </button>
                                ` : ''}
                                ${so.status === 'In Progress' ? `
                                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.showCancelSOModal(${so.id}, '${so.soNumber}', true)" title="Cancel">
                                        <i class="fas fa-ban"></i>
                                    </button>
                                ` : ''}
                                ${so.status === 'Picked' ? `
                                    <button type="button" class="btn btn-sm btn-outline-success" onclick="salesOrderManager.shipSalesOrder(${so.id})" title="Ship Order">
                                        <i class="fas fa-truck"></i>
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
                        No Sales Orders found
                    </td>
                </tr>
            `;
        }

        tableHtml += `</tbody></table></div>`;
        container.innerHTML = tableHtml;
    }

    getStatusBadge(status) {
        const badges = {
            'Pending': '<span class="badge bg-warning">Pending</span>',
            'In Progress': '<span class="badge bg-info">In Progress</span>',
            'Picked': '<span class="badge bg-primary">Picked</span>',
            'Shipped': '<span class="badge bg-success">Shipped</span>',
            'Cancelled': '<span class="badge bg-danger">Cancelled</span>'
        };
        return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
    }

    updatePagination(pagination) {
        if (!pagination) return;

        const start = ((pagination.currentPage - 1) * pagination.pageSize) + 1;
        const end = Math.min(pagination.currentPage * pagination.pageSize, pagination.totalCount);

        document.getElementById('showingStart').textContent = start;
        document.getElementById('showingEnd').textContent = end;
        document.getElementById('totalRecords').textContent = pagination.totalCount;
        document.getElementById('currentPageNum').textContent = pagination.currentPage;
        document.getElementById('totalPagesNum').textContent = pagination.totalPages;

        const prevBtn = document.getElementById('prevPageBtn');
        const nextBtn = document.getElementById('nextPageBtn');

        if (prevBtn) {
            prevBtn.disabled = pagination.currentPage <= 1;
        }
        if (nextBtn) {
            nextBtn.disabled = pagination.currentPage >= pagination.totalPages;
        }
    }

    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadSalesOrders();
        }
    }

    nextPage() {
        this.currentPage++;
        this.loadSalesOrders();
    }

    async loadCustomers() {
        try {
            const response = await fetch('/api/salesorder/customers');
            const result = await response.json();
            if (result.success) {
                this.populateCustomerDropdown(result.data);
            }
        } catch (error) {
            console.error('Error loading customers:', error);
        }
    }

    populateCustomerDropdown(customers) {
        // Populate create form
        const select = document.getElementById('customerId');
        if (select) {
            select.innerHTML = '<option value="">-- Select Customer --</option>';
            customers.forEach(customer => {
                const option = document.createElement('option');
                option.value = customer.id;
                option.textContent = customer.name;
                select.appendChild(option);
            });
        }

        // Edit form customer is readonly, no need to populate dropdown
    }

    async loadLocations() {
        try {
            const response = await fetch('/api/salesorder/locations');
            const result = await response.json();
            if (result.success) {
                this.populateLocationDropdown(result.data);
            }
        } catch (error) {
            console.error('Error loading locations:', error);
        }
    }

    populateLocationDropdown(locations) {
        // Populate create form
        const select = document.getElementById('holdingLocationId');
        if (select) {
            select.innerHTML = '<option value="">-- Select Holding Location --</option>';
            locations.forEach(location => {
                const option = document.createElement('option');
                option.value = location.id;
                option.textContent = `${location.name} (${location.code}) - Available: ${location.availableCapacity}/${location.maxCapacity}`;
                select.appendChild(option);
            });
        }

        // Populate edit form
        const editSelect = document.getElementById('editHoldingLocationId');
        if (editSelect) {
            const currentValue = editSelect.value; // Preserve current selection
            editSelect.innerHTML = '<option value="">-- Select Holding Location --</option>';
            locations.forEach(location => {
                const option = document.createElement('option');
                option.value = location.id;
                option.textContent = `${location.name} (${location.code}) - Available: ${location.availableCapacity}/${location.maxCapacity}`;
                if (location.id.toString() === currentValue) {
                    option.selected = true;
                }
                editSelect.appendChild(option);
            });
        }
    }

    showCreateModal() {
        this.currentSOId = null;
        this.resetForm();
        document.getElementById('soModalTitle').textContent = 'Create New Sales Order';
        
        // Set default order date to now
        const now = new Date();
        const localDateTime = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
        document.getElementById('orderDate').value = localDateTime;

        const modal = new bootstrap.Modal(document.getElementById('soModal'));
        modal.show();
    }

    resetForm() {
        const form = document.getElementById('soForm');
        if (form) form.reset();
        this.currentSOItems = [];
        this.selectedItemIds = [];
        this.updateSOItemsContainer();
        this.updateSOSummary();
        this.hideMessages();
    }

    addSOItem(formType = 'create') {
        // Open Advanced Search Modal to select item from inventory
        if (window.salesOrderItemManager) {
            window.salesOrderItemManager.openItemAdvancedSearch(null);
        } else {
            alert('Item manager not initialized. Please refresh the page.');
        }
    }

    removeSOItem(itemId) {
        const item = this.currentSOItems.find(i => i.id === itemId);
        if (item && item.itemId) {
            const index = this.selectedItemIds.indexOf(item.itemId);
            if (index > -1) {
                this.selectedItemIds.splice(index, 1);
            }
        }
        this.currentSOItems = this.currentSOItems.filter(i => i.id !== itemId);
        
        // Determine which form is active
        const editModal = document.getElementById('editSOModal');
        const formType = editModal && editModal.classList.contains('show') ? 'edit' : 'create';
        
        this.updateSOItemsContainer(formType);
        this.updateSOSummary(formType);
    }

    updateSOItemsContainer(formType = 'create') {
        const containerId = formType === 'edit' ? 'editSOItemsContainer' : 'soItemsContainer';
        const container = document.getElementById(containerId);
        if (!container) return;

        if (this.currentSOItems.length === 0) {
            container.innerHTML = '<div class="text-center text-muted py-3">No items added yet. Click "Add Item" to start.</div>';
            return;
        }

        let html = '';
        this.currentSOItems.forEach(item => {
            const stockWarning = item.availableStock < item.quantity ? 
                '<span class="badge bg-warning ms-2">Insufficient Stock</span>' : '';
            
            html += `
                <div class="row mb-2 align-items-center" data-item-id="${item.id}">
                    <div class="col-md-3">
                        <input type="text" class="form-control" 
                            value="${item.itemCode ? item.itemCode + ' - ' + item.itemName : 'N/A'}" 
                            readonly
                            style="background-color: #f8f9fa;">
                        ${item.itemId ? `<small class="text-muted d-block mt-1">Stock: ${item.availableStock}</small>${stockWarning}` : ''}
                    </div>
                    <div class="col-md-1">
                        <span class="badge bg-secondary">${item.availableStock}</span>
                    </div>
                    <div class="col-md-2">
                        <input type="number" class="form-control item-quantity" 
                            value="${item.quantity}" 
                            min="1" 
                            max="${item.availableStock}"
                            onchange="salesOrderManager.updateItemQuantity(${item.id}, this.value)">
                    </div>
                    <div class="col-md-2">
                        <input type="number" class="form-control item-unit-price" 
                            value="${item.unitPrice}" 
                            step="0.01" 
                            min="0"
                            onchange="salesOrderManager.updateItemPrice(${item.id}, this.value)">
                        <small class="text-muted">Standard Price</small>
                    </div>
                    <div class="col-md-2">
                        <input type="text" class="form-control item-total-price" 
                            value="Rp ${(item.totalPrice || 0).toLocaleString('id-ID')}" 
                            readonly>
                    </div>
                    <div class="col-md-2">
                        <button type="button" class="btn btn-sm btn-danger" onclick="salesOrderManager.removeSOItem(${item.id})">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>
            `;
        });

        container.innerHTML = html;
    }

    updateItemQuantity(itemId, quantity) {
        const item = this.currentSOItems.find(i => i.id === itemId);
        if (item) {
            quantity = parseInt(quantity) || 1;
            if (quantity > item.availableStock) {
                this.showError(`Quantity cannot exceed available stock (${item.availableStock})`);
                quantity = item.availableStock;
            }
            item.quantity = quantity;
            item.totalPrice = item.quantity * item.unitPrice;
            
            // Determine which form is active
            const editModal = document.getElementById('editSOModal');
            const formType = editModal && editModal.classList.contains('show') ? 'edit' : 'create';
            
            this.updateSOItemsContainer(formType);
            this.updateSOSummary(formType);
        }
    }

    updateItemPrice(itemId, price) {
        const item = this.currentSOItems.find(i => i.id === itemId);
        if (item) {
            item.unitPrice = parseFloat(price) || 0;
            item.totalPrice = item.quantity * item.unitPrice;
            
            // Determine which form is active
            const editModal = document.getElementById('editSOModal');
            const formType = editModal && editModal.classList.contains('show') ? 'edit' : 'create';
            
            this.updateSOItemsContainer(formType);
            this.updateSOSummary(formType);
        }
    }

    updateSOSummary(formType = 'create') {
        const totalAmount = this.currentSOItems.reduce((sum, item) => sum + (item.totalPrice || 0), 0);
        const itemsCount = this.currentSOItems.length;

        if (formType === 'edit') {
            const editTotalEl = document.getElementById('editSOTotalAmount');
            const editCountEl = document.getElementById('editSOItemsCount');
            if (editTotalEl) editTotalEl.textContent = `Rp ${totalAmount.toLocaleString('id-ID')}`;
            if (editCountEl) editCountEl.textContent = `${itemsCount} item${itemsCount !== 1 ? 's' : ''}`;
        } else {
            const totalEl = document.getElementById('soTotalAmount');
            const countEl = document.getElementById('soItemsCount');
            if (totalEl) totalEl.textContent = `Rp ${totalAmount.toLocaleString('id-ID')}`;
            if (countEl) countEl.textContent = `${itemsCount} item${itemsCount !== 1 ? 's' : ''}`;
        }
    }

    async saveSalesOrder() {
        const form = document.getElementById('soForm');
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        const customerId = document.getElementById('customerId').value;
        const orderDate = document.getElementById('orderDate').value;
        const expectedArrivalDate = document.getElementById('expectedArrivalDate').value;
        const holdingLocationId = document.getElementById('holdingLocationId').value;
        const notes = document.getElementById('notes').value;

        if (this.currentSOItems.length === 0) {
            this.showError('Please add at least one item');
            return;
        }

        // Check for duplicate items
        const itemIds = this.currentSOItems.map(i => i.itemId).filter(id => id);
        const duplicates = itemIds.filter((id, index) => itemIds.indexOf(id) !== index);
        if (duplicates.length > 0) {
            this.showError('Duplicate items are not allowed');
            return;
        }

        const saveBtn = document.getElementById('saveSOBtn');
        if (!saveBtn) {
            console.error('Save button not found');
            this.showError('Save button not found. Please refresh the page.');
            return;
        }

        saveBtn.disabled = true;
        
        // Null check sebelum akses classList
        const spinner = saveBtn.querySelector('.spinner-border');
        const btnText = saveBtn.querySelector('.btn-text');
        
        if (spinner) {
            spinner.classList.remove('d-none');
        } else if (btnText) {
            // Fallback: ubah text button jika spinner tidak ditemukan
            btnText.textContent = 'Loading...';
        }

        try {
            const requestData = {
                customerId: parseInt(customerId),
                orderDate: orderDate ? new Date(orderDate).toISOString() : null,
                expectedArrivalDate: expectedArrivalDate ? new Date(expectedArrivalDate).toISOString() : null,
                holdingLocationId: parseInt(holdingLocationId),
                notes: notes,
                items: this.currentSOItems.map(item => ({
                    itemId: item.itemId,
                    quantity: parseInt(item.quantity),
                    unitPrice: parseFloat(item.unitPrice),
                    notes: item.notes || null
                }))
            };

            const url = this.currentSOId ? `/api/salesorder/${this.currentSOId}` : '/api/salesorder';
            const method = this.currentSOId ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message || 'Sales Order saved successfully');
                setTimeout(() => {
                    bootstrap.Modal.getInstance(document.getElementById('soModal')).hide();
                    this.loadSalesOrders();
                    this.loadDashboard();
                }, 1000);
            } else {
                this.showError(result.message || 'Failed to save Sales Order');
            }
        } catch (error) {
            console.error('Error saving Sales Order:', error);
            this.showError('Error saving Sales Order');
        } finally {
            if (saveBtn) {
                saveBtn.disabled = false;
                
                // Null check sebelum akses classList
                const spinner = saveBtn.querySelector('.spinner-border');
                const btnText = saveBtn.querySelector('.btn-text');
                
                if (spinner) {
                    spinner.classList.add('d-none');
                } else if (btnText) {
                    // Fallback: restore text button
                    btnText.textContent = 'Save Sales Order';
                }
            }
        }
    }

    async viewSalesOrder(id) {
        try {
            if (!id || id <= 0) {
                this.showError('Invalid Sales Order ID');
                return;
            }

            // Navigate to Details page instead of opening modal
            window.location.href = `/SalesOrder/Details/${id}`;
        } catch (error) {
            console.error('Error navigating to Sales Order details:', error);
            this.showError('Error navigating to Sales Order details');
        }
    }

    async editSalesOrder(id) {
        try {
            console.log('SalesOrderManager: Editing sales order ID:', id);
            
            if (!id || id <= 0) {
                console.error('SalesOrderManager: Invalid sales order ID:', id);
                this.showError('Invalid sales order ID');
                return;
            }

            const response = await fetch(`/api/salesorder/${id}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.currentSO = result.data;
                this.currentSOId = result.data.id;
                await this.populateEditForm(result.data);
                
                const modal = new bootstrap.Modal(document.getElementById('editSOModal'));
                modal.show();
            } else {
                console.error('SalesOrderManager: Error loading sales order:', result.message);
                this.showError('Error loading sales order: ' + result.message);
            }
        } catch (error) {
            console.error('SalesOrderManager: Error editing sales order:', error);
            this.showError('Error loading sales order');
        }
    }

    /**
     * Populate edit form with Sales Order data
     */
    async populateEditForm(salesOrder) {
        console.log('SalesOrderManager: Populating edit form for SO:', salesOrder.soNumber);
        
        const form = document.getElementById('editSOForm');
        if (!form) return;

        // Customer (readonly display)
        const customerDisplay = document.getElementById('editCustomerIdDisplay');
        const customerHidden = document.getElementById('editCustomerId');
        if (customerDisplay) {
            customerDisplay.value = salesOrder.customerName || 'N/A';
        }
        if (customerHidden) {
            customerHidden.value = salesOrder.customerId || '';
        }

        // Order Date (readonly)
        const orderDateInput = document.getElementById('editOrderDate');
        if (orderDateInput && salesOrder.orderDate) {
            const orderDate = new Date(salesOrder.orderDate);
            const localDateTime = new Date(orderDate.getTime() - orderDate.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
            orderDateInput.value = localDateTime;
        }

        // Expected Arrival Date
        const expectedDateInput = document.getElementById('editExpectedArrivalDate');
        if (expectedDateInput && salesOrder.requiredDate) {
            const expectedDate = new Date(salesOrder.requiredDate);
            expectedDateInput.value = expectedDate.toISOString().split('T')[0];
        }

        // Holding Location
        const holdingLocationSelect = document.getElementById('editHoldingLocationId');
        if (holdingLocationSelect) {
            // Load locations if not already loaded
            await this.loadLocations();
            holdingLocationSelect.value = salesOrder.holdingLocationId || '';
        }

        // Notes
        const notesInput = document.getElementById('editNotes');
        if (notesInput) {
            notesInput.value = salesOrder.notes || '';
        }

        // Items - populate existing items
        this.currentSOItems = [];
        this.selectedItemIds = [];
        
        if (salesOrder.details && salesOrder.details.length > 0) {
            // Load available stock for each item
            for (const detail of salesOrder.details) {
                try {
                    // Get available stock for this item
                    const stockResponse = await fetch(`/api/salesorder/items/${detail.itemId}/stock`);
                    const stockResult = await stockResponse.json();
                    const availableStock = stockResult.success ? stockResult.data.totalStock : 0;

                    const item = {
                        id: Date.now() + Math.random(), // Unique ID
                        itemId: detail.itemId,
                        itemCode: detail.itemCode,
                        itemName: detail.itemName,
                        unit: detail.itemUnit,
                        quantity: detail.quantity,
                        unitPrice: detail.unitPrice,
                        totalPrice: detail.totalPrice,
                        availableStock: availableStock,
                        notes: detail.notes || ''
                    };

                    this.currentSOItems.push(item);
                    this.selectedItemIds.push(detail.itemId);
                } catch (error) {
                    console.error('Error loading stock for item:', detail.itemId, error);
                    // Add item without stock info
                    const item = {
                        id: Date.now() + Math.random(),
                        itemId: detail.itemId,
                        itemCode: detail.itemCode,
                        itemName: detail.itemName,
                        unit: detail.itemUnit,
                        quantity: detail.quantity,
                        unitPrice: detail.unitPrice,
                        totalPrice: detail.totalPrice,
                        availableStock: 0,
                        notes: detail.notes || ''
                    };
                    this.currentSOItems.push(item);
                    this.selectedItemIds.push(detail.itemId);
                }
            }
        }

        // Update UI
        this.updateSOItemsContainer('edit');
        this.updateSOSummary('edit');
    }

    /**
     * Update Sales Order
     */
    async updateSalesOrder() {
        try {
            console.log('SalesOrderManager: Updating sales order...');
            
            if (!this.currentSOId) {
                console.error('SalesOrderManager: No current sales order ID');
                this.showError('No sales order selected for update');
                return;
            }

            const form = document.getElementById('editSOForm');
            if (!form) {
                console.error('SalesOrderManager: Edit form not found');
                this.showError('Edit form not found');
                return;
            }

            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return;
            }

            const customerId = document.getElementById('editCustomerId').value;
            const expectedArrivalDate = document.getElementById('editExpectedArrivalDate').value;
            const holdingLocationId = document.getElementById('editHoldingLocationId').value;
            const notes = document.getElementById('editNotes').value;

            if (this.currentSOItems.length === 0) {
                this.showError('Please add at least one item');
                return;
            }

            // Check for duplicate items
            const itemIds = this.currentSOItems.map(i => i.itemId).filter(id => id);
            const duplicates = itemIds.filter((id, index) => itemIds.indexOf(id) !== index);
            if (duplicates.length > 0) {
                this.showError('Duplicate items are not allowed');
                return;
            }

            const updateBtn = document.getElementById('updateSOBtn');
            if (!updateBtn) {
                console.error('Update button not found');
                this.showError('Update button not found. Please refresh the page.');
                return;
            }

            updateBtn.disabled = true;
            const spinner = updateBtn.querySelector('.spinner-border');
            const btnText = updateBtn.querySelector('.btn-text');
            
            if (spinner) {
                spinner.classList.remove('d-none');
            } else if (btnText) {
                btnText.textContent = 'Updating...';
            }

            const requestData = {
                expectedArrivalDate: expectedArrivalDate ? new Date(expectedArrivalDate).toISOString() : null,
                holdingLocationId: parseInt(holdingLocationId),
                notes: notes,
                items: this.currentSOItems.map(item => ({
                    itemId: item.itemId,
                    quantity: parseInt(item.quantity),
                    unitPrice: parseFloat(item.unitPrice),
                    notes: item.notes || null
                }))
            };

            console.log('SalesOrderManager: Update request:', requestData);

            const response = await fetch(`/api/salesorder/${this.currentSOId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message || 'Sales Order updated successfully');
                setTimeout(() => {
                    const modal = bootstrap.Modal.getInstance(document.getElementById('editSOModal'));
                    if (modal) modal.hide();
                    this.loadSalesOrders();
                    this.loadDashboard();
                }, 1000);
            } else {
                this.showError(result.message || 'Failed to update Sales Order');
            }
        } catch (error) {
            console.error('Error updating Sales Order:', error);
            this.showError('Error updating Sales Order');
        } finally {
            const updateBtn = document.getElementById('updateSOBtn');
            if (updateBtn) {
                updateBtn.disabled = false;
                const spinner = updateBtn.querySelector('.spinner-border');
                const btnText = updateBtn.querySelector('.btn-text');
                
                if (spinner) {
                    spinner.classList.add('d-none');
                } else if (btnText) {
                    btnText.textContent = 'Update Sales Order';
                }
            }
        }
    }

    /**
     * Reset edit form
     */
    resetEditForm() {
        const form = document.getElementById('editSOForm');
        if (form) form.reset();
        this.currentSOItems = [];
        this.selectedItemIds = [];
        this.currentSOId = null;
        this.currentSO = null;
        this.updateSOItemsContainer('edit');
        this.updateSOSummary('edit');
        this.hideMessages();
    }


    async createPicking(salesOrderId) {
        if (!confirm('Create picking for this Sales Order?')) return;

        try {
            const response = await fetch('/api/picking', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    salesOrderId: salesOrderId
                })
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess('Picking created successfully');
                bootstrap.Modal.getInstance(document.getElementById('viewSOModal')).hide();
                // Redirect to picking page or refresh
                window.location.href = '/Picking';
            } else {
                this.showError(result.message || 'Failed to create Picking');
            }
        } catch (error) {
            console.error('Error creating Picking:', error);
            this.showError('Error creating Picking');
        }
    }

    async shipSalesOrder(id) {
        if (!id) {
            this.showError('Invalid Sales Order ID');
            return;
        }

        // Confirmation dialog
        if (!confirm('Are you sure you want to mark this Sales Order as Shipped? This action cannot be undone.')) {
            return;
        }

        try {
            const response = await fetch(`/api/salesorder/${id}/status`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ status: 'Shipped' })
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message || 'Sales Order marked as Shipped successfully');
                // Reload sales orders list and dashboard
                setTimeout(() => {
                    this.loadSalesOrders();
                    this.loadDashboard();
                }, 1000);
            } else {
                this.showError(result.message || 'Failed to ship Sales Order');
            }
        } catch (error) {
            console.error('Error shipping Sales Order:', error);
            this.showError('Error shipping Sales Order');
        }
    }

    showError(message) {
        // Check if edit modal is open
        const editModal = document.getElementById('editSOModal');
        const isEditMode = editModal && editModal.classList.contains('show');
        
        if (isEditMode) {
            const errorDiv = document.getElementById('editSOErrorMessage');
            if (errorDiv) {
                errorDiv.textContent = message;
                errorDiv.classList.remove('d-none');
                setTimeout(() => errorDiv.classList.add('d-none'), 5000);
                return;
            }
        }
        
        const errorDiv = document.getElementById('errorMessage');
        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.classList.remove('d-none');
            setTimeout(() => errorDiv.classList.add('d-none'), 5000);
        } else {
            alert(message);
        }
    }

    showSuccess(message) {
        // Check if edit modal is open
        const editModal = document.getElementById('editSOModal');
        const isEditMode = editModal && editModal.classList.contains('show');
        
        if (isEditMode) {
            const successDiv = document.getElementById('editSOSuccessMessage');
            if (successDiv) {
                successDiv.textContent = message;
                successDiv.classList.remove('d-none');
                setTimeout(() => successDiv.classList.add('d-none'), 3000);
                return;
            }
        }
        
        const successDiv = document.getElementById('successMessage');
        if (successDiv) {
            successDiv.textContent = message;
            successDiv.classList.remove('d-none');
            setTimeout(() => successDiv.classList.add('d-none'), 3000);
        }
    }

    hideMessages() {
        document.getElementById('errorMessage')?.classList.add('d-none');
        document.getElementById('successMessage')?.classList.add('d-none');
        document.getElementById('editSOErrorMessage')?.classList.add('d-none');
        document.getElementById('editSOSuccessMessage')?.classList.add('d-none');
    }
}

// Initialize when DOM is ready
let salesOrderManager;
document.addEventListener('DOMContentLoaded', () => {
    salesOrderManager = new SalesOrderManager();
    window.salesOrderManager = salesOrderManager;
});

