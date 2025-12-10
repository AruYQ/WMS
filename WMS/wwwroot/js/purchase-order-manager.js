/**
 * Purchase Order Manager - AJAX-based CRUD operations
 * Manages all Purchase Order operations including items management and email sending
 */
class PurchaseOrderManager {
    constructor() {
        this.currentPurchaseOrder = null;
        this.currentPurchaseOrderId = null;
        this.suppliers = [];
        this.items = [];
        this.currentPage = 1;
        this.pageSize = 10;
        this.currentStatus = null;
        this.currentSearch = '';
        this.currentSupplier = '';
        
        this.init();
    }

    /**
     * Initialize the manager
     */
    async init() {
        try {
            console.log('PurchaseOrderManager: Initializing...');
            
            // Load initial data
            await this.loadSuppliers();
            await this.loadItems();
            await this.loadPurchaseOrders();
            await this.loadDashboard();
            
            // Setup event listeners
            this.setupEventListeners();
            
            console.log('PurchaseOrderManager: Initialized successfully');
        } catch (error) {
            console.error('PurchaseOrderManager: Initialization error:', error);
            this.showToast('Error initializing Purchase Order Manager', 'error');
        }
    }

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        // Search functionality
        const searchInput = document.getElementById('searchPurchaseOrders');
        if (searchInput) {
            searchInput.addEventListener('input', this.debounce((e) => {
                this.currentSearch = e.target.value;
                this.loadPurchaseOrders();
            }, 300));
        }

        // Status filter buttons
        document.addEventListener('click', (e) => {
            if (e.target.matches('.status-filter-btn')) {
                e.preventDefault();
                const status = e.target.dataset.status;
                this.filterByStatus(status);
            }
        });

        // Pagination
        document.addEventListener('click', (e) => {
            if (e.target.matches('.pagination-btn')) {
                e.preventDefault();
                const page = parseInt(e.target.dataset.page);
                if (page && page !== this.currentPage) {
                    this.currentPage = page;
                    this.loadPurchaseOrders();
                }
            }
        });

        // Modal events
        document.addEventListener('click', (e) => {
            if (e.target.matches('[data-action="create-purchase-order"]')) {
                e.preventDefault();
                this.showCreateModal();
            }
            
            if (e.target.matches('[data-action="edit-purchase-order"]')) {
                e.preventDefault();
                const id = parseInt(e.target.dataset.id);
                this.editPurchaseOrder(id);
            }
            
            if (e.target.matches('[data-action="view-purchase-order"]')) {
                e.preventDefault();
                const id = parseInt(e.target.dataset.id);
                this.viewPurchaseOrder(id);
            }
            
            if (e.target.matches('[data-action="send-purchase-order"]')) {
                e.preventDefault();
                const id = parseInt(e.target.dataset.id);
                this.sendPurchaseOrder(id);
            }
            
            if (e.target.matches('[data-action="cancel-purchase-order"]')) {
                e.preventDefault();
                const id = parseInt(e.target.dataset.id);
                const number = e.target.dataset.number;
                if (window.showCancelPOModal) {
                    window.showCancelPOModal(id, number);
                }
            }
        });

        // Form submissions
        document.addEventListener('submit', (e) => {
            if (e.target.matches('#createPurchaseOrderForm')) {
                e.preventDefault();
                this.createPurchaseOrder();
            }
            
            if (e.target.matches('#editPurchaseOrderForm')) {
                e.preventDefault();
                this.updatePurchaseOrder();
            }
        });

        // Modal close events
        document.addEventListener('hidden.bs.modal', (e) => {
            if (e.target.matches('#createPurchaseOrderModal')) {
                this.resetCreateForm();
            }
            
            if (e.target.matches('#editPurchaseOrderModal')) {
                this.resetEditForm();
            }
            
            if (e.target.matches('#viewPurchaseOrderModal')) {
                this.resetViewForm();
            }
        });

        // Item management in forms
        document.addEventListener('click', (e) => {
            if (e.target.matches('[data-action="add-item"]')) {
                e.preventDefault();
                // Deteksi formType dari button location
                const formType = e.target.closest('#editPurchaseOrderModal') ? 'edit' : 'create';
                this.addItem(formType);
            }
            
            if (e.target.matches('[data-action="remove-item"]')) {
                e.preventDefault();
                const index = parseInt(e.target.dataset.index);
                // Deteksi formType dari button location
                const formType = e.target.closest('#editPurchaseOrderModal') ? 'edit' : 'create';
                this.removeItem(index, formType);
            }
        });

        // Item selection and quantity changes
        document.addEventListener('change', (e) => {
            if (e.target.matches('.item-select')) {
                this.onItemSelect(e.target);
            }
            
            if (e.target.matches('.quantity-input')) {
                this.calculateItemTotal(e.target);
            }
        });

        // Item advanced search button
        document.addEventListener('click', (e) => {
            if (e.target.matches('.item-search-btn') || e.target.closest('.item-search-btn')) {
                e.preventDefault();
                const btn = e.target.closest('.item-search-btn');
                const index = parseInt(btn.dataset.index);
                
                // Get current supplier and purchase order context
                const supplierId = this.getCurrentSupplierId();
                const purchaseOrderId = this.currentPurchaseOrderId;
                
                // Open item advanced search modal
                if (window.openPOItemAdvancedSearch) {
                    window.openPOItemAdvancedSearch(supplierId, purchaseOrderId, index);
                } else {
                    console.error('Purchase Order Item Advanced Search not available');
                    this.showToast('Item advanced search not available', 'error');
                }
            }
        });

        // Item input events
        document.addEventListener('input', (e) => {
            if (e.target.matches('.quantity-input')) {
                this.calculateItemTotal(e.target);
            }
        });

        // Supplier change event
        document.addEventListener('change', (e) => {
            // Handle SearchableDropdown supplier change
            if (e.target.matches('.selected-id-input')) {
                const container = e.target.closest('.searchable-dropdown-container');
                if (container && container.dataset.fieldName === 'supplierId') {
                    this.onSupplierChange(e.target.value);
                }
            }
        });
    }

    /**
     * Load suppliers from API
     */
    async loadSuppliers() {
        try {
            console.log('PurchaseOrderManager: Loading suppliers...');
            
            const response = await fetch('/api/purchaseorder/suppliers', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.suppliers = result.data;
                console.log('PurchaseOrderManager: Suppliers loaded:', this.suppliers.length);
                this.populateSupplierDropdowns();
            } else {
                console.error('PurchaseOrderManager: Error loading suppliers:', result.message);
                this.showToast('Error loading suppliers: ' + result.message, 'error');
            }
        } catch (error) {
            console.error('PurchaseOrderManager: Error loading suppliers:', error);
            this.showToast('Error loading suppliers', 'error');
        }
    }

    /**
     * Load items from API
     */
    async loadItems(supplierId = null) {
        try {
            console.log('PurchaseOrderManager: Loading items for supplier:', supplierId);
            
            // Clear items first if no supplier selected
            if (!supplierId) {
                this.items = [];
                this.populateItemDropdowns();
                return;
            }
            
            let url = '/api/purchaseorder/items';
            url += `?supplierId=${supplierId}`;
            
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.items = result.data || [];
                console.log('PurchaseOrderManager: Items loaded for supplier', supplierId, ':', this.items.length);
                this.populateItemDropdowns();
            } else {
                console.error('PurchaseOrderManager: Error loading items:', result.message);
                this.showToast('Error loading items: ' + result.message, 'error');
                this.items = [];
            }
        } catch (error) {
            console.error('PurchaseOrderManager: Error loading items:', error);
            this.showToast('Error loading items', 'error');
            this.items = [];
        }
    }

    /**
     * Load purchase orders from API
     */
    async loadPurchaseOrders() {
        try {
            console.log('PurchaseOrderManager: Loading purchase orders...');
            
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize
            });

            if (this.currentSearch) {
                params.append('search', this.currentSearch);
            }

            if (this.currentStatus) {
                params.append('status', this.currentStatus);
            }

            if (this.currentSupplier) {
                params.append('supplier', this.currentSupplier);
            }

            const response = await fetch(`/api/purchaseorder?${params}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            const result = await response.json();
            
            if (result.success) {
                console.log('PurchaseOrderManager: Purchase orders loaded:', result.data.length);
                this.updatePurchaseOrdersTable(result.data, result.pagination);
            } else {
                console.error('PurchaseOrderManager: Error loading purchase orders:', result.message);
                this.showToast('Error loading purchase orders: ' + result.message, 'error');
            }
        } catch (error) {
            console.error('PurchaseOrderManager: Error loading purchase orders:', error);
            this.showToast('Error loading purchase orders', 'error');
        }
    }

    /**
     * Load dashboard statistics
     */
    async loadDashboard() {
        try {
            console.log('PurchaseOrderManager: Loading dashboard...');
            
            const response = await fetch('/api/purchaseorder/dashboard', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            const result = await response.json();
            
            if (result.success) {
                console.log('PurchaseOrderManager: Dashboard loaded:', result.data);
                this.updateDashboard(result.data);
            } else {
                console.error('PurchaseOrderManager: Error loading dashboard:', result.message);
            }
        } catch (error) {
            console.error('PurchaseOrderManager: Error loading dashboard:', error);
        }
    }

    /**
     * Update purchase orders table
     */
    updatePurchaseOrdersTable(purchaseOrders, pagination) {
        const tbody = document.querySelector('#purchaseOrdersTable tbody');
        if (!tbody) return;

        if (!purchaseOrders || purchaseOrders.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-4">
                        <div class="text-muted">
                            <i class="fas fa-inbox fa-2x mb-2"></i>
                            <p>No purchase orders found</p>
                        </div>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = purchaseOrders.map(po => `
            <tr>
                <td>
                    <div class="fw-medium text-primary">${po.poNumber}</div>
                    <div class="small text-muted">
                        <i class="fas fa-calendar me-1"></i>
                        Created: ${new Date(po.createdDate).toLocaleDateString()}
                    </div>
                </td>
                <td>
                    <div class="fw-medium">${po.supplierName}</div>
                    ${po.supplierEmail ? `
                        <div class="small text-muted">
                            <i class="fas fa-envelope me-1"></i>
                            ${po.supplierEmail}
                        </div>
                    ` : ''}
                </td>
                <td>
                    <div class="fw-medium">${new Date(po.orderDate).toLocaleDateString()}</div>
                </td>
                <td>
                    ${po.expectedDeliveryDate ? `
                        <div class="fw-medium">${new Date(po.expectedDeliveryDate).toLocaleDateString()}</div>
                    ` : '<span class="text-muted">Not set</span>'}
                </td>
                <td>
                    <span class="badge ${this.getStatusBadgeClass(po.status)}">${po.status}</span>
                </td>
                <td>
                    <div class="fw-medium">${new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR' }).format(po.totalAmount)}</div>
                </td>
                <td>
                    <div class="fw-medium">${po.itemCount} items</div>
                </td>
                <td>
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-sm btn-outline-info" data-action="view-purchase-order" data-id="${po.id}" title="View Details">
                            <i class="fas fa-eye"></i>
                        </button>
                        ${po.status === 'Draft' ? `
                            <button type="button" class="btn btn-sm btn-outline-warning" data-action="edit-purchase-order" data-id="${po.id}" title="Edit">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-success" data-action="send-purchase-order" data-id="${po.id}" title="Send to Supplier">
                                <i class="fas fa-paper-plane"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger" data-action="cancel-purchase-order" data-id="${po.id}" data-number="${po.poNumber}" title="Cancel">
                                <i class="fas fa-ban"></i>
                            </button>
                        ` : ''}
                    </div>
                </td>
            </tr>
        `).join('');

        // Update pagination
        this.updatePagination(pagination);
    }

    /**
     * Update pagination controls
     */
    updatePagination(pagination) {
        const paginationContainer = document.getElementById('purchaseOrderPagination');
        if (!paginationContainer || !pagination) return;

        let paginationHTML = '<nav><ul class="pagination justify-content-center">';
        
        // Previous button
        paginationHTML += `
            <li class="page-item ${!pagination.hasPreviousPage ? 'disabled' : ''}">
                <a class="page-link pagination-btn" href="#" data-page="${pagination.currentPage - 1}">
                    <i class="fas fa-chevron-left"></i>
                </a>
            </li>
        `;

        // Page numbers
        const startPage = Math.max(1, pagination.currentPage - 2);
        const endPage = Math.min(pagination.totalPages, pagination.currentPage + 2);

        for (let i = startPage; i <= endPage; i++) {
            paginationHTML += `
                <li class="page-item ${i === pagination.currentPage ? 'active' : ''}">
                    <a class="page-link pagination-btn" href="#" data-page="${i}">${i}</a>
                </li>
            `;
        }

        // Next button
        paginationHTML += `
            <li class="page-item ${!pagination.hasNextPage ? 'disabled' : ''}">
                <a class="page-link pagination-btn" href="#" data-page="${pagination.currentPage + 1}">
                    <i class="fas fa-chevron-right"></i>
                </a>
            </li>
        `;

        paginationHTML += '</ul></nav>';
        paginationContainer.innerHTML = paginationHTML;
    }

    /**
     * Update dashboard statistics
     */
    updateDashboard(stats) {
        console.log('PurchaseOrderManager: Updating dashboard with stats:', stats);
        
        // Update dashboard cards
        const totalElement = document.getElementById('totalPurchaseOrders');
        if (totalElement) totalElement.textContent = stats.totalPurchaseOrders;

        const draftElement = document.getElementById('draftOrders');
        if (draftElement) draftElement.textContent = stats.draftOrders;

        const sentElement = document.getElementById('sentOrders');
        if (sentElement) sentElement.textContent = stats.sentOrders;

        const receivedElement = document.getElementById('receivedOrders');
        if (receivedElement) receivedElement.textContent = stats.receivedOrders;

        // Update status filter buttons with counts
        const statusButtons = document.querySelectorAll('.status-filter-btn');
        statusButtons.forEach(btn => {
            const status = btn.dataset.status;
            let count = 0;
            
            switch (status) {
                case 'Draft':
                    count = stats.draftOrders;
                    break;
                case 'Sent':
                    count = stats.sentOrders;
                    break;
                case 'Received':
                    count = stats.receivedOrders;
                    break;
                case 'Cancelled':
                    count = stats.cancelledOrders;
                    break;
                default:
                    count = stats.totalPurchaseOrders;
            }
            
            const badge = btn.querySelector('.badge');
            if (badge) {
                badge.textContent = count;
            }
        });
    }

    /**
     * Filter by status
     */
    filterByStatus(status) {
        console.log('PurchaseOrderManager: Filtering by status:', status);
        
        // Update active button
        document.querySelectorAll('.status-filter-btn').forEach(btn => {
            btn.classList.remove('active');
            if (btn.dataset.status === status) {
                btn.classList.add('active');
            }
        });

        this.currentStatus = status === 'All' ? null : status;
        this.currentPage = 1;
        this.loadPurchaseOrders();
    }

    /**
     * Show create modal
     */
    showCreateModal() {
        console.log('PurchaseOrderManager: Showing create modal');
        
        const modal = new bootstrap.Modal(document.getElementById('createPurchaseOrderModal'));
        this.resetCreateForm();
        this.setDefaultDates(); // Set default dates
        // No auto-add item - user must select supplier first
        modal.show();
    }

    /**
     * Show edit modal
     */
    async editPurchaseOrder(id) {
        try {
            console.log('PurchaseOrderManager: Editing purchase order ID:', id);
            
            if (!id || id <= 0) {
                console.error('PurchaseOrderManager: Invalid purchase order ID:', id);
                this.showToast('Invalid purchase order ID', 'error');
                return;
            }

            const response = await fetch(`/api/purchaseorder/${id}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.currentPurchaseOrder = result.data;
                this.currentPurchaseOrderId = result.data.id;
                await this.populateEditForm(result.data);
                
                const modal = new bootstrap.Modal(document.getElementById('editPurchaseOrderModal'));
                modal.show();
            } else {
                console.error('PurchaseOrderManager: Error loading purchase order:', result.message);
                this.showToast('Error loading purchase order: ' + result.message, 'error');
            }
        } catch (error) {
            console.error('PurchaseOrderManager: Error editing purchase order:', error);
            this.showToast('Error loading purchase order', 'error');
        }
    }

    /**
     * Show view modal
     */
    async viewPurchaseOrder(id) {
        try {
            console.log('PurchaseOrderManager: Viewing purchase order ID:', id);
            
            if (!id || id <= 0) {
                console.error('PurchaseOrderManager: Invalid purchase order ID:', id);
                this.showToast('Invalid purchase order ID', 'error');
                return;
            }

            // Navigate to Details page instead of opening modal
            window.location.href = `/PurchaseOrder/Details/${id}`;
        } catch (error) {
            console.error('PurchaseOrderManager: Error viewing purchase order:', error);
            this.showToast('Error navigating to purchase order details', 'error');
        }
    }

    /**
     * Create purchase order
     */
    async createPurchaseOrder() {
        try {
            console.log('PurchaseOrderManager: Creating purchase order...');
            
            const form = document.getElementById('createPurchaseOrderForm');
            if (!form) {
                console.error('PurchaseOrderManager: Create form not found');
                this.showToast('Create form not found', 'error');
                return;
            }

            const formData = new FormData(form);
            const request = {
                supplierId: parseInt(formData.get('supplierId')),
                orderDate: formData.get('orderDate') ? new Date(formData.get('orderDate')) : null,
                expectedDeliveryDate: formData.get('expectedDeliveryDate') ? new Date(formData.get('expectedDeliveryDate')) : null,
                notes: formData.get('notes'),
                details: this.getFormItems('create')
            };

            console.log('PurchaseOrderManager: Create request:', request);

            // Validate
            if (!request.supplierId || request.supplierId <= 0) {
                this.showToast('Please select a supplier', 'error');
                return;
            }

            if (!request.details || request.details.length === 0) {
                this.showToast('Please add at least one item', 'error');
                return;
            }

            const response = await fetch('/api/purchaseorder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(request)
            });

            const result = await response.json();
            
            if (result.success) {
                console.log('PurchaseOrderManager: Purchase order created successfully:', result.data);
                this.showToast(result.message, 'success');
                
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('createPurchaseOrderModal'));
                if (modal) modal.hide();
                
                // Reload data
                await this.loadPurchaseOrders();
                await this.loadDashboard();
            } else {
                console.error('PurchaseOrderManager: Error creating purchase order:', result.message);
                this.showToast('Error creating purchase order: ' + result.message, 'error');
            }
        } catch (error) {
            console.error('PurchaseOrderManager: Error creating purchase order:', error);
            this.showToast('Error creating purchase order', 'error');
        }
    }

    /**
     * Update purchase order
     */
    async updatePurchaseOrder() {
        try {
            console.log('PurchaseOrderManager: Updating purchase order...');
            
            if (!this.currentPurchaseOrderId) {
                console.error('PurchaseOrderManager: No current purchase order ID');
                this.showToast('No purchase order selected for update', 'error');
                return;
            }

            const form = document.getElementById('editPurchaseOrderForm');
            if (!form) {
                console.error('PurchaseOrderManager: Edit form not found');
                this.showToast('Edit form not found', 'error');
                return;
            }

            // PENTING: Ambil supplierId langsung dari selected-id-input element, bukan dari FormData
            // karena FormData mungkin tidak membaca value yang baru ter-set
            const supplierContainer = form.querySelector('[data-field-name="supplierId"]');
            let supplierId = null;
            
            if (supplierContainer) {
                const selectedIdInput = supplierContainer.querySelector('.selected-id-input');
                if (selectedIdInput && selectedIdInput.value) {
                    supplierId = parseInt(selectedIdInput.value);
                    console.log('PurchaseOrderManager: Supplier ID from element:', supplierId);
                }
            }
            
            // Fallback ke FormData jika element tidak ditemukan
            if (!supplierId) {
                const formData = new FormData(form);
                supplierId = parseInt(formData.get('supplierId'));
                console.log('PurchaseOrderManager: Supplier ID from FormData (fallback):', supplierId);
            }

            const formData = new FormData(form);
            const request = {
                supplierId: supplierId, // Gunakan value dari element
                orderDate: formData.get('orderDate') ? new Date(formData.get('orderDate')) : null,
                expectedDeliveryDate: formData.get('expectedDeliveryDate') ? new Date(formData.get('expectedDeliveryDate')) : null,
                notes: formData.get('notes'),
                details: this.getFormItems('edit')
            };

            console.log('PurchaseOrderManager: Update request:', request);

            // Validate
            if (!request.supplierId || request.supplierId <= 0) {
                this.showToast('Please select a supplier', 'error');
                return;
            }

            if (!request.details || request.details.length === 0) {
                this.showToast('Please add at least one item', 'error');
                return;
            }

            const response = await fetch(`/api/purchaseorder/${this.currentPurchaseOrderId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(request)
            });

            const result = await response.json();
            
            if (result.success) {
                console.log('PurchaseOrderManager: Purchase order updated successfully');
                this.showToast(result.message, 'success');
                
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('editPurchaseOrderModal'));
                if (modal) modal.hide();
                
                // Reload data
                await this.loadPurchaseOrders();
                await this.loadDashboard();
            } else {
                console.error('PurchaseOrderManager: Error updating purchase order:', result.message);
                this.showToast('Error updating purchase order: ' + result.message, 'error');
            }
        } catch (error) {
            console.error('PurchaseOrderManager: Error updating purchase order:', error);
            this.showToast('Error updating purchase order', 'error');
        }
    }

    /**
     * Send purchase order via email
     */
    async sendPurchaseOrder(id) {
        try {
            console.log('PurchaseOrderManager: Sending purchase order ID:', id);
            
            if (!id || id <= 0) {
                console.error('PurchaseOrderManager: Invalid purchase order ID:', id);
                this.showToast('Invalid purchase order ID', 'error');
                return;
            }

            if (!confirm('Are you sure you want to send this purchase order to the supplier? This will change the status to "Sent".')) {
                return;
            }

            const response = await fetch(`/api/purchaseorder/${id}/send`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            const result = await response.json();
            
            if (result.success) {
                console.log('PurchaseOrderManager: Purchase order sent successfully');
                this.showToast(result.message, 'success');
                
                // Reload data
                await this.loadPurchaseOrders();
                await this.loadDashboard();
            } else {
                console.error('PurchaseOrderManager: Error sending purchase order:', result.message);
                this.showToast('Error sending purchase order: ' + result.message, 'error');
            }
        } catch (error) {
            console.error('PurchaseOrderManager: Error sending purchase order:', error);
            this.showToast('Error sending purchase order', 'error');
        }
    }

    /**
     * Add item to form
     */
    addItem(formType = 'create') {
        console.log('PurchaseOrderManager: Adding item to', formType, 'form');
        
        // Check if supplier is selected
        const supplierId = this.getCurrentSupplierId();
        if (!supplierId) {
            this.showToast('Please select a supplier first before adding items', 'warning');
            return;
        }
        
        const container = document.getElementById(`${formType}ItemsContainer`);
        if (!container) return;

        const itemIndex = container.children.length;
        const itemHtml = this.getItemRowHtml(itemIndex, formType);
        
        container.insertAdjacentHTML('beforeend', itemHtml);
        this.updateItemNumbers(formType);
        this.calculateTotal(formType);
    }

    /**
     * Remove item from form
     */
    removeItem(index, formType = 'create') {
        console.log('PurchaseOrderManager: Removing item', index, 'from', formType, 'form');
        
        const container = document.getElementById(`${formType}ItemsContainer`);
        if (!container) return;

        const itemRow = container.children[index];
        if (itemRow) {
            itemRow.remove();
            this.updateItemNumbers(formType);
            this.calculateTotal(formType);
        }
    }

    /**
     * Get item row HTML with dropdown and advanced search
     */
    getItemRowHtml(index, formType) {
        // Check if supplier is selected
        const supplierId = this.getCurrentSupplierId();
        if (!supplierId) {
            return `
                <div class="row item-row mb-2" data-index="${index}">
                    <div class="col-md-12">
                        <div class="alert alert-warning">
                            <i class="fas fa-exclamation-triangle"></i>
                            Please select a supplier first before adding items.
                        </div>
                    </div>
                </div>
            `;
        }

        // Only use items for the currently selected supplier
        // If items haven't been loaded for this supplier yet, return empty options
        const relevantItems = this.items && this.items.length > 0 ? this.items : [];
        
        const itemsOptions = relevantItems.map(item => 
            `<option value="${item.id}" data-unit="${item.unit}" data-price="${item.purchasePrice}">${item.name} (${item.code})</option>`
        ).join('');

        return `
            <div class="row item-row mb-2" data-index="${index}">
                <div class="col-md-4">
                    <div class="input-group">
                        <select class="form-select item-select" name="details[${index}].itemId" required>
                            <option value="">-- Select Item --</option>
                            ${itemsOptions}
                        </select>
                        <button class="btn btn-outline-secondary item-search-btn" type="button" title="Advanced Search Item" data-index="${index}">
                            <i class="fas fa-search"></i>
                        </button>
                    </div>
                </div>
                <div class="col-md-2">
                    <input type="number" class="form-control quantity-input" name="details[${index}].quantity" 
                           min="1" step="1" value="1" required>
                </div>
                <div class="col-md-2">
                    <input type="number" class="form-control unit-price-input" name="details[${index}].unitPrice" 
                           min="0" step="0.01" value="0" readonly>
                </div>
                <div class="col-md-2">
                    <input type="text" class="form-control total-price-display" readonly>
                </div>
                <div class="col-md-2">
                    <button type="button" class="btn btn-sm btn-outline-danger" data-action="remove-item" data-index="${index}">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `;
    }

    /**
     * Update item numbers in form
     */
    updateItemNumbers(formType) {
        const container = document.getElementById(`${formType}ItemsContainer`);
        if (!container) return;

        Array.from(container.children).forEach((row, index) => {
            row.setAttribute('data-index', index);
            row.querySelector('.item-select').setAttribute('name', `details[${index}].itemId`);
            row.querySelector('.quantity-input').setAttribute('name', `details[${index}].quantity`);
            row.querySelector('.unit-price-input').setAttribute('name', `details[${index}].unitPrice`);
            row.querySelector('[data-action="remove-item"]').setAttribute('data-index', index);
        });
    }

    /**
     * Calculate total for form
     */
    calculateTotal(formType) {
        const container = document.getElementById(`${formType}ItemsContainer`);
        if (!container) return;

        let total = 0;
        Array.from(container.children).forEach(row => {
            const quantity = parseFloat(row.querySelector('.quantity-input').value) || 0;
            const unitPrice = parseFloat(row.querySelector('.unit-price-input').value) || 0;
            const itemTotal = quantity * unitPrice;
            
            row.querySelector('.total-price-display').value = new Intl.NumberFormat('id-ID', { 
                style: 'currency', 
                currency: 'IDR' 
            }).format(itemTotal);
            
            total += itemTotal;
        });

        const totalElement = document.getElementById(`${formType}TotalAmount`);
        if (totalElement) {
            totalElement.textContent = new Intl.NumberFormat('id-ID', { 
                style: 'currency', 
                currency: 'IDR' 
            }).format(total);
        }

        // Update item count
        const itemCount = container.children.length;
        const itemCountElement = document.getElementById(`${formType}ItemsCount`);
        if (itemCountElement) {
            itemCountElement.textContent = `${itemCount} items`;
        }
    }

    /**
     * Get form items data
     */
    getFormItems(formType) {
        const container = document.getElementById(`${formType}ItemsContainer`);
        if (!container) return [];

        const items = [];
        Array.from(container.children).forEach(row => {
            const itemId = parseInt(row.querySelector('.item-select').value);
            const quantity = parseInt(row.querySelector('.quantity-input').value);
            const unitPrice = parseFloat(row.querySelector('.unit-price-input').value);
            const notes = row.querySelector('.item-notes')?.value || '';

            if (itemId && quantity && unitPrice) {
                items.push({
                    itemId: itemId,
                    quantity: quantity,
                    unitPrice: unitPrice,
                    notes: notes
                });
            }
        });

        return items;
    }

    /**
     * Populate supplier dropdowns
     * Note: Suppliers now use SearchableDropdown component, this method is for backward compatibility
     */
    populateSupplierDropdowns() {
        // SearchableDropdown handles supplier population via advanced search
        console.log('PurchaseOrderManager: Suppliers will be populated via SearchableDropdown');
    }

    /**
     * Populate item dropdowns
     */
    populateItemDropdowns() {
        // Update existing item dropdowns with loaded items
        // This ensures dropdowns are populated when items are loaded
        this.updateExistingItemDropdowns();
    }

    /**
     * Populate edit form
     */
    async populateEditForm(purchaseOrder) {
        console.log('PurchaseOrderManager: Populating edit form for PO:', purchaseOrder.poNumber);
        
        const form = document.getElementById('editPurchaseOrderForm');
        if (!form) return;

        // Supplier - using SearchableDropdown
        // PENTING: Hanya set value jika belum ada value yang ter-set (untuk menghindari override saat edit)
        const supplierContainer = form.querySelector('[data-field-name="supplierId"]');
        if (supplierContainer) {
            const supplierInput = supplierContainer.querySelector('.searchable-dropdown-input');
            const supplierIdInput = supplierContainer.querySelector('.selected-id-input');
            
            if (supplierInput && supplierIdInput) {
                // PENTING: Hanya set value saat pertama kali populate (value masih kosong)
                // JANGAN override jika user sudah mengubah supplier via advanced search
                const currentSupplierId = supplierIdInput.value;
                const currentSupplierName = supplierInput.value;
                
                // Hanya set jika value benar-benar kosong (belum pernah di-set)
                // Atau jika value masih sama dengan purchaseOrder (belum diubah user)
                if (!currentSupplierId || currentSupplierId === '' || 
                    (currentSupplierId === String(purchaseOrder.supplierId) && 
                     currentSupplierName === purchaseOrder.supplierName)) {
                    supplierInput.value = `${purchaseOrder.supplierName}`;
                supplierIdInput.value = purchaseOrder.supplierId;
                    supplierInput.setAttribute('data-selected-id', purchaseOrder.supplierId);
                    console.log('PurchaseOrderManager: Populating supplier - ID:', purchaseOrder.supplierId, 'Name:', purchaseOrder.supplierName);
                } else {
                    console.log('PurchaseOrderManager: Supplier already set by user, skipping populate - Current ID:', currentSupplierId, 'PO ID:', purchaseOrder.supplierId);
                }
                // Jangan trigger change event di sini karena akan clear item selections
                // Change event akan ter-trigger otomatis saat user mengubah supplier
            }
        }

        // Basic fields
        form.querySelector('#editOrderDate').value = purchaseOrder.orderDate ? new Date(purchaseOrder.orderDate).toISOString().split('T')[0] : '';
        form.querySelector('#editExpectedDeliveryDate').value = purchaseOrder.expectedDeliveryDate ? new Date(purchaseOrder.expectedDeliveryDate).toISOString().split('T')[0] : '';
        form.querySelector('#editNotes').value = purchaseOrder.notes || '';

        // PENTING: Load items untuk supplier sebelum populate items
        // Jangan panggil onSupplierChange karena akan clear item selections
        await this.loadItems(purchaseOrder.supplierId);

        // Enable Add Item button since supplier is selected
        this.toggleAddItemButton(purchaseOrder.supplierId);

        // Items - populate setelah items loaded
        const container = document.getElementById('editItemsContainer');
        if (container) {
            container.innerHTML = '';
            
            purchaseOrder.details.forEach((detail, index) => {
                const itemHtml = this.getItemRowHtml(index, 'edit');
                container.insertAdjacentHTML('beforeend', itemHtml);
                
                // Set values
                const row = container.children[index];
                const itemSelect = row.querySelector('.item-select');
                const quantityInput = row.querySelector('.quantity-input');
                const unitPriceInput = row.querySelector('.unit-price-input');
                
                if (itemSelect) {
                    itemSelect.value = detail.itemId;
                    
                    // Set unit price dari detail (jika ada) atau dari item option
                    if (unitPriceInput) {
                        if (detail.unitPrice && detail.unitPrice > 0) {
                            // Gunakan unit price dari detail
                            unitPriceInput.value = detail.unitPrice;
                        } else {
                            // Coba ambil dari option
                            const selectedOption = itemSelect.options[itemSelect.selectedIndex];
                            if (selectedOption && selectedOption.dataset.price) {
                                unitPriceInput.value = parseFloat(selectedOption.dataset.price);
                            }
                        }
                    }
                }
                
                if (quantityInput) {
                    quantityInput.value = detail.quantity;
                }
                
                // Calculate total untuk row ini
                if (quantityInput || unitPriceInput) {
                    this.calculateItemTotal(quantityInput || unitPriceInput);
                }
            });
            
            // Calculate total untuk semua items
                this.calculateTotal('edit');
        }
    }

    /**
     * Populate view form
     */
    populateViewForm(purchaseOrder) {
        console.log('PurchaseOrderManager: Populating view form for PO:', purchaseOrder.poNumber);
        
        // Basic info
        document.getElementById('viewPONumber').textContent = purchaseOrder.poNumber;
        document.getElementById('viewSupplierName').textContent = purchaseOrder.supplierName;
        document.getElementById('viewOrderDate').textContent = purchaseOrder.orderDate ? new Date(purchaseOrder.orderDate).toLocaleDateString() : 'Not set';
        document.getElementById('viewExpectedDeliveryDate').textContent = purchaseOrder.expectedDeliveryDate ? new Date(purchaseOrder.expectedDeliveryDate).toLocaleDateString() : 'Not set';
        const statusElement = document.getElementById('viewStatus');
        statusElement.textContent = purchaseOrder.status;
        statusElement.className = `badge ${this.getStatusBadgeClass(purchaseOrder.status)}`;
        document.getElementById('viewTotalAmount').textContent = new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR' }).format(purchaseOrder.totalAmount);
        document.getElementById('viewNotes').textContent = purchaseOrder.notes || 'No notes';

        // Items
        const tbody = document.querySelector('#viewItemsTable tbody');
        if (tbody) {
            tbody.innerHTML = purchaseOrder.details.map(detail => `
                <tr>
                    <td>${detail.itemCode}</td>
                    <td>${detail.itemName}</td>
                    <td>${detail.quantity}</td>
                    <td>${detail.itemUnit}</td>
                    <td>${new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR' }).format(detail.unitPrice)}</td>
                    <td>${new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR' }).format(detail.totalPrice)}</td>
                </tr>
            `).join('');
        }

        // Update view summary
        document.getElementById('viewSummaryTotalAmount').textContent = new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR' }).format(purchaseOrder.totalAmount);
        document.getElementById('viewSummaryItemsCount').textContent = `${purchaseOrder.details.length} items`;
    }

    /**
     * Reset create form
     */
    resetCreateForm() {
        const form = document.getElementById('createPurchaseOrderForm');
        if (form) {
            form.reset();
        }

        // Reset SearchableDropdown for supplier
        const supplierInput = document.querySelector('#supplierSelect');
        if (supplierInput) {
            const supplierContainer = supplierInput.closest('.searchable-dropdown-container');
            if (supplierContainer) {
                const selectedIdInput = supplierContainer.querySelector('.selected-id-input');
                const input = supplierContainer.querySelector('.searchable-dropdown-input');
                if (selectedIdInput) selectedIdInput.value = '';
                if (input) input.value = '';
                
                // Trigger change event to update onSupplierChange
                const changeEvent = new Event('change', { bubbles: true });
                selectedIdInput.dispatchEvent(changeEvent);
            }
        }

        const container = document.getElementById('createItemsContainer');
        if (container) {
            container.innerHTML = '';
            // No auto-add item - start with empty container
        }

        // Clear items for supplier selection
        this.items = [];

        // Disable Add Item button (no supplier selected)
        this.toggleAddItemButton(null);

        // Reset totals to 0
        this.calculateTotal('create');

        this.currentPurchaseOrder = null;
        this.currentPurchaseOrderId = null;
        
        // Set default dates after reset
        this.setDefaultDates();
    }

    /**
     * Reset edit form
     */
    resetEditForm() {
        const form = document.getElementById('editPurchaseOrderForm');
        if (form) {
            form.reset();
        }

        const container = document.getElementById('editItemsContainer');
        if (container) {
            container.innerHTML = '';
        }

        // Disable Add Item button
        this.toggleAddItemButton(null);

        this.currentPurchaseOrder = null;
        this.currentPurchaseOrderId = null;
    }

    /**
     * Reset view form
     */
    resetViewForm() {
        this.currentPurchaseOrder = null;
    }

    /**
     * Handle supplier change
     */
    async onSupplierChange(supplierId) {
        // PENTING: Validasi supplier ID
        if (!supplierId || supplierId === '' || supplierId === '0') {
            console.warn('PurchaseOrderManager: onSupplierChange called with invalid supplier ID:', supplierId);
            return;
        }
        
        // PENTING: Gunakan parameter supplierId yang diterima, bukan value dari form
        // karena value dari form mungkin belum ter-update saat ini (timing issue)
        const actualSupplierId = String(supplierId);
        
        // Deteksi formType dari supplier input yang ada
        const editSupplierInput = document.querySelector('#editSupplierSelect');
        const createSupplierInput = document.querySelector('#supplierSelect');
        
        let formType = 'create';
        
        // Deteksi formType
        if (editSupplierInput) {
            const editModal = editSupplierInput.closest('.modal');
            if (editModal && editModal.classList.contains('show')) {
                formType = 'edit';
            }
        }
        
        // Verifikasi value di form (untuk logging/debugging)
        const supplierContainer = formType === 'edit' 
            ? editSupplierInput?.closest('.searchable-dropdown-container')
            : createSupplierInput?.closest('.searchable-dropdown-container');
        
        let formValue = '';
        if (supplierContainer) {
            const selectedIdInput = supplierContainer.querySelector('.selected-id-input');
            if (selectedIdInput) {
                formValue = selectedIdInput.value;
            }
        }
        
        console.log('PurchaseOrderManager: Supplier changed to:', actualSupplierId, '(Form value:', formValue, ')', 'FormType:', formType);
        
        // PENTING: Pastikan value di form ter-update dengan benar
        // TAPI jangan override searchInput.value karena sudah ter-set di selectSupplierFromAdvanced
        if (supplierContainer) {
            const selectedIdInput = supplierContainer.querySelector('.selected-id-input');
            const searchInput = supplierContainer.querySelector('.searchable-dropdown-input');
            
            if (selectedIdInput && selectedIdInput.value !== actualSupplierId) {
                console.warn('PurchaseOrderManager: Form value mismatch! Updating form value from', selectedIdInput.value, 'to', actualSupplierId);
                selectedIdInput.value = actualSupplierId;
                
                // Update data-selected-id attribute juga
                // PENTING: Jangan reset searchInput.value karena akan menghapus display text
                // searchInput.value sudah ter-set di selectSupplierFromAdvanced dengan supplier name
                if (searchInput) {
                    searchInput.setAttribute('data-selected-id', actualSupplierId);
                    // Jangan set searchInput.value di sini karena akan menghapus supplier name yang sudah ter-set
                }
            }
        }
        
        // Enable/disable Add Item button based on supplier selection
        this.toggleAddItemButton(actualSupplierId);
        
        // PENTING: Clear items container (remove semua rows) saat supplier berubah
        const container = document.getElementById(`${formType}ItemsContainer`);
        if (container) {
            container.innerHTML = ''; // Remove semua item rows
        }
        
        // Reload items based on supplier (gunakan actualSupplierId dari parameter)
        await this.loadItems(actualSupplierId);
        
        // PENTING: Populate item dropdowns untuk memastikan dropdown ter-update
        // Meskipun container sudah di-clear, ini memastikan items ter-load untuk dropdown baru
        this.populateItemDropdowns();
        
        // Recalculate total setelah container di-clear
        this.calculateTotal(formType);
    }

    /**
     * Toggle Add Item button based on supplier selection
     */
    toggleAddItemButton(supplierId) {
        const addItemButtons = document.querySelectorAll('[data-action="add-item"]');
        addItemButtons.forEach(button => {
            button.disabled = !supplierId;
        });
    }

    /**
     * Update existing item dropdowns with new items
     */
    updateExistingItemDropdowns() {
        // Update dropdowns in both create and edit forms
        const containers = [
            document.getElementById('createItemsContainer'),
            document.getElementById('editItemsContainer')
        ];
        
        containers.forEach(container => {
            if (!container) return;
            
            const itemSelects = container.querySelectorAll('.item-select');
        itemSelects.forEach(select => {
            const currentValue = select.value;
            
            // Clear existing options except the first one
            const defaultOption = select.querySelector('option[value=""]');
            select.innerHTML = '';
            if (defaultOption) {
                    select.appendChild(defaultOption.cloneNode(true));
                } else {
                    // Create default option if not exists
                    const defaultOpt = document.createElement('option');
                    defaultOpt.value = '';
                    defaultOpt.textContent = '-- Select Item --';
                    select.appendChild(defaultOpt);
            }
            
            // Add new options based on loaded items
            this.items.forEach(item => {
                const option = document.createElement('option');
                option.value = item.id;
                option.textContent = `${item.name} (${item.code})`;
                option.setAttribute('data-unit', item.unit);
                option.setAttribute('data-price', item.purchasePrice);
                select.appendChild(option);
            });
            
            // Restore previous selection if it's still valid
            if (currentValue && this.items.some(item => item.id === parseInt(currentValue))) {
                select.value = currentValue;
            }
            });
        });
    }
    
    /**
     * Clear all item selections
     */
    clearItemSelections() {
        const itemRows = document.querySelectorAll('.item-row');
        itemRows.forEach(row => {
            const itemSelect = row.querySelector('.item-select');
            const unitPriceInput = row.querySelector('.unit-price-input');
            
            if (itemSelect) itemSelect.value = '';
            if (unitPriceInput) unitPriceInput.value = '0';
        });
        
        // Recalculate totals for both forms
        this.calculateTotal('create');
        this.calculateTotal('edit');
    }

    /**
     * Validate item selection for duplicates
     */
    validateItemSelection(selectedItemId, currentIndex, formType = 'create') {
        const containerId = formType === 'create' ? '#createItemsContainer' : '#editItemsContainer';
        const itemRows = document.querySelectorAll(`${containerId} .item-row`);
        let isDuplicate = false;
        
        itemRows.forEach((row, index) => {
            if (index !== currentIndex) {
                const itemSelect = row.querySelector('.item-select');
                if (itemSelect && itemSelect.value == selectedItemId) {
                    isDuplicate = true;
                }
            }
        });
        
        return isDuplicate;
    }

    /**
     * Set default dates for create form
     */
    setDefaultDates() {
        const today = new Date();
        const fiveDaysLater = new Date(today);
        fiveDaysLater.setDate(today.getDate() + 5);
        
        // Format dates to YYYY-MM-DD for input[type="date"]
        const todayStr = today.toISOString().split('T')[0];
        const fiveDaysLaterStr = fiveDaysLater.toISOString().split('T')[0];
        
        // Set Order Date to today and make it readonly
        const orderDateInput = document.getElementById('orderDate');
        if (orderDateInput) {
            orderDateInput.value = todayStr;
            orderDateInput.readOnly = true;
            orderDateInput.style.backgroundColor = '#f8f9fa';
        }
        
        // Set Expected Delivery Date to 5 days later (editable)
        const expectedDeliveryInput = document.getElementById('expectedDeliveryDate');
        if (expectedDeliveryInput) {
            expectedDeliveryInput.value = fiveDaysLaterStr;
        }
    }

    /**
     * Handle item selection
     */
    onItemSelect(selectElement) {
        console.log('PurchaseOrderManager: Item selected:', selectElement.value);
        
        const selectedItemId = selectElement.value;
        const currentRow = selectElement.closest('.item-row');
        const currentIndex = Array.from(currentRow.parentNode.children).indexOf(currentRow);
        
        // Determine form type
        const isCreateForm = selectElement.closest('#createItemsContainer') !== null;
        const formType = isCreateForm ? 'create' : 'edit';
        
        // Check for duplicate items
        if (selectedItemId && this.validateItemSelection(selectedItemId, currentIndex, formType)) {
            this.showToast('This item has already been selected. Please choose a different item.', 'error');
            selectElement.value = '';
            return;
        }
        
        const selectedOption = selectElement.options[selectElement.selectedIndex];
        if (selectedOption && selectedOption.dataset.price) {
            const row = selectElement.closest('.item-row');
            if (row) {
                const unitPriceInput = row.querySelector('.unit-price-input');
                if (unitPriceInput) {
                    unitPriceInput.value = parseFloat(selectedOption.dataset.price);
                    this.calculateItemTotal(unitPriceInput);
                }
            }
        }
    }

    // Advanced search methods will be implemented here

    /**
     * Get current supplier ID from form
     */
    getCurrentSupplierId() {
        // Try to get from create form first
        let supplierInput = document.querySelector('#supplierSelect');
        if (supplierInput) {
            const supplierContainer = supplierInput.closest('.searchable-dropdown-container');
            if (supplierContainer) {
                const selectedIdInput = supplierContainer.querySelector('.selected-id-input');
                if (selectedIdInput && selectedIdInput.value) {
                    return selectedIdInput.value;
                }
            }
        }
        
        // Try to get from edit form
        supplierInput = document.querySelector('#editSupplierSelect');
        if (supplierInput) {
            const supplierContainer = supplierInput.closest('.searchable-dropdown-container');
            if (supplierContainer) {
                const selectedIdInput = supplierContainer.querySelector('.selected-id-input');
                if (selectedIdInput && selectedIdInput.value) {
                    return selectedIdInput.value;
                }
            }
        }
        
        return null;
    }

    /**
     * Calculate item total
     */
    calculateItemTotal(inputElement) {
        const row = inputElement.closest('.item-row');
        if (!row) return;

        const quantityInput = row.querySelector('.quantity-input');
        const unitPriceInput = row.querySelector('.unit-price-input');
        const totalDisplay = row.querySelector('.total-price-display');

        if (quantityInput && unitPriceInput && totalDisplay) {
            const quantity = parseFloat(quantityInput.value) || 0;
            const unitPrice = parseFloat(unitPriceInput.value) || 0;
            const total = quantity * unitPrice;

            totalDisplay.value = new Intl.NumberFormat('id-ID', { 
                style: 'currency', 
                currency: 'IDR' 
            }).format(total);

            // Update form total
            const formType = row.closest('.modal').id.includes('create') ? 'create' : 'edit';
            this.calculateTotal(formType);
        }
    }

    /**
     * Get status badge class
     */
    getStatusBadgeClass(status) {
        switch (status) {
            case 'Draft':
                return 'bg-secondary';
            case 'Sent':
                return 'bg-primary';
            case 'Received':
                return 'bg-success';
            case 'Cancelled':
                return 'bg-danger';
            default:
                return 'bg-secondary';
        }
    }

    /**
     * Show toast notification
     */
    showToast(message, type = 'info') {
        // Create toast element
        const toastHtml = `
            <div class="toast align-items-center text-white bg-${type === 'error' ? 'danger' : type} border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;

        // Add to toast container
        let toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toastContainer';
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);

        // Show toast
        const toastElement = toastContainer.lastElementChild;
        const toast = new bootstrap.Toast(toastElement);
        toast.show();

        // Remove toast element after it's hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }

    /**
     * Get anti-forgery token
     */
    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    /**
     * Debounce function
     */
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
}

// Global functions for backward compatibility
window.createPurchaseOrder = function() {
    if (window.purchaseOrderManager) {
        window.purchaseOrderManager.showCreateModal();
    }
};

window.editPurchaseOrder = function(id) {
    if (window.purchaseOrderManager) {
        window.purchaseOrderManager.editPurchaseOrder(id);
    }
};

window.viewPurchaseOrder = function(id) {
    if (window.purchaseOrderManager) {
        window.purchaseOrderManager.viewPurchaseOrder(id);
    }
};

window.sendPurchaseOrder = function(id) {
    if (window.purchaseOrderManager) {
        window.purchaseOrderManager.sendPurchaseOrder(id);
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('PurchaseOrderManager: DOM loaded, initializing...');
    window.purchaseOrderManager = new PurchaseOrderManager();
});
