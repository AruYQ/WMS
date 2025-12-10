/**
 * Item Manager - JavaScript untuk handle Item CRUD operations
 * Full AJAX pattern seperti LocationController
 */

class ItemManager {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalPages = 0;
        this.totalCount = 0;
        this.sortBy = 'itemCode';
        this.sortDirection = 'asc';
        this.isEditMode = false;
        this.isShowingError = false; // Flag to prevent duplicate error display
        
        this.init();
    }

    init() {
        this.bindEvents();
        this.loadDashboard();
        this.loadItems();
        this.loadSuppliers();
    }

    bindEvents() {
        // Search input
        document.getElementById('searchInput')?.addEventListener('input', (e) => {
            clearTimeout(this.searchTimeout);
            this.searchTimeout = setTimeout(() => {
                this.currentPage = 1;
                this.loadItems();
            }, 500);
        });

        // Filter dropdowns
        document.getElementById('supplierFilter')?.addEventListener('change', () => {
            this.currentPage = 1;
            this.loadItems();
        });

        document.getElementById('statusFilter')?.addEventListener('change', () => {
            this.currentPage = 1;
            this.loadItems();
        });

        // Form validation
        document.getElementById('itemForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveItem();
        });
    }

    // ===== DASHBOARD FUNCTIONS =====

    async loadDashboard() {
        try {
            const response = await fetch('/api/item/dashboard');
            const data = await response.json();
            
            if (data.success) {
                this.renderDashboard(data.data);
            } else {
                console.error('Error loading dashboard:', data.message);
            }
        } catch (error) {
            console.error('Error loading dashboard:', error);
        }
    }

    renderDashboard(stats) {
        // Update individual stat cards
        document.getElementById('totalItemsCount').textContent = stats.totalItems || '0';
        document.getElementById('activeItemsCount').textContent = stats.activeItems || '0';
        document.getElementById('inactiveItemsCount').textContent = stats.inactiveItems || '0';
        document.getElementById('totalSuppliersCount').textContent = stats.totalSuppliers || '0';
    }

    // ===== ITEM LIST FUNCTIONS =====

    async loadItems() {
        try {
            const search = document.getElementById('searchInput')?.value || '';
            const supplierId = document.getElementById('supplierFilter')?.value || '';
            const isActive = document.getElementById('statusFilter')?.value || '';

            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                search: search,
                supplierId: supplierId,
                isActive: isActive,
                sortBy: this.sortBy,
                sortDirection: this.sortDirection
            });

            const response = await fetch(`/api/item?${params}`);
            const data = await response.json();
            
            if (data.success) {
                this.renderItemsTable(data.data);
                this.updatePagination(data.totalCount, data.totalPages);
            } else {
                console.error('Error loading items:', data.message);
                this.showError(data.message);
            }
        } catch (error) {
            console.error('Error loading items:', error);
            this.showError('Error loading items');
        }
    }

    renderItemsTable(items) {
        const container = document.getElementById('itemsTableContainer');
        if (!container) return;

        if (items.length === 0) {
            container.innerHTML = '<div class="text-center py-5"><p>No items found</p></div>';
            return;
        }

        let html = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>ITEM CODE</th>
                            <th>ITEM NAME</th>
                            <th>DESCRIPTION</th>
                            <th>UNIT</th>
                            <th>PURCHASE PRICE</th>
                            <th>STANDARD PRICE</th>
                            <th>SUPPLIER</th>
                            <th>STOCK</th>
                            <th>STATUS</th>
                            <th>ACTIONS</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        items.forEach(item => {
            html += `
                <tr>
                    <td><strong>${item.itemCode}</strong></td>
                    <td>${item.name}</td>
                    <td>${item.description || '-'}</td>
                    <td><span class="badge bg-secondary">${item.unit}</span></td>
                    <td>Rp ${item.purchasePrice.toLocaleString('id-ID')}</td>
                    <td>Rp ${item.standardPrice.toLocaleString('id-ID')}</td>
                    <td>${item.supplierName}</td>
                    <td><span class="badge ${item.totalStock > 10 ? 'bg-success' : item.totalStock > 0 ? 'bg-warning' : 'bg-danger'}">${item.totalStock}</span></td>
                    <td>
                        <span class="badge ${item.isActive ? 'bg-success' : 'bg-secondary'}">
                            ${item.isActive ? 'Active' : 'Inactive'}
                        </span>
                    </td>
                    <td>
                        <div class="btn-group btn-group-sm" role="group">
                            <button type="button" class="btn btn-sm btn-info" onclick="itemManager.viewItem(${item.id})" title="View">
                                <i class="fas fa-eye"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-primary" onclick="itemManager.editItem(${item.id})" title="Edit">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-warning" onclick="itemManager.toggleItemStatus(${item.id}, ${!item.isActive})" title="Toggle Status">
                                <i class="fas fa-${item.isActive ? 'pause' : 'play'}"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-danger" onclick="itemManager.deleteItem(${item.id})" title="Delete">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `;
        });

        html += `
                    </tbody>
                </table>
            </div>
        `;

        container.innerHTML = html;
        
        // Update pagination controls
        this.updatePagination();
    }

    renderPagination(totalPages, totalCount) {
        // Pagination is now handled in renderItemsTable
        this.totalPages = totalPages;
        this.totalCount = totalCount;
    }

    updatePagination(totalCount, totalPages) {
        if (totalCount !== undefined) this.totalCount = totalCount;
        if (totalPages !== undefined) this.totalPages = totalPages;
        
        // Update pagination info
        const showingStart = document.getElementById('showingStart');
        const showingEnd = document.getElementById('showingEnd');
        const totalRecords = document.getElementById('totalRecords');
        const currentPageNum = document.getElementById('currentPageNum');
        const totalPagesNum = document.getElementById('totalPagesNum');
        const prevPageBtn = document.getElementById('prevPageBtn');
        const nextPageBtn = document.getElementById('nextPageBtn');

        if (showingStart) showingStart.textContent = ((this.currentPage - 1) * this.pageSize) + 1;
        if (showingEnd) showingEnd.textContent = Math.min(this.currentPage * this.pageSize, this.totalCount);
        if (totalRecords) totalRecords.textContent = this.totalCount;
        if (currentPageNum) currentPageNum.textContent = this.currentPage;
        if (totalPagesNum) totalPagesNum.textContent = this.totalPages;
        if (prevPageBtn) prevPageBtn.disabled = this.currentPage <= 1;
        if (nextPageBtn) nextPageBtn.disabled = this.currentPage >= this.totalPages;
    }

    loadPage(page) {
        this.currentPage = page;
        this.loadItems();
    }

    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadItems();
        }
    }

    nextPage() {
        if (this.currentPage < this.totalPages) {
            this.currentPage++;
            this.loadItems();
        }
    }

    changePageSize(newPageSize) {
        this.pageSize = parseInt(newPageSize);
        this.currentPage = 1; // Reset to first page
        this.loadItems();
    }

    sortTable(column) {
        if (this.sortBy === column) {
            this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            this.sortBy = column;
            this.sortDirection = 'asc';
        }
        this.currentPage = 1;
        this.loadItems();
    }

    clearFilters() {
        document.getElementById('searchInput').value = '';
        document.getElementById('supplierFilter').value = '';
        document.getElementById('statusFilter').value = '';
        this.currentPage = 1;
        this.loadItems();
    }

    formatNumber(num) {
        return new Intl.NumberFormat('id-ID').format(num);
    }

    formatCurrency(num) {
        return new Intl.NumberFormat('id-ID', {
            style: 'currency',
            currency: 'IDR',
            minimumFractionDigits: 0
        }).format(num);
    }

    async loadSuppliers() {
        try {
            const response = await fetch('/Item/GetSuppliers');
            const result = await response.json();
            
            if (result.success) {
                // Populate filter dropdown
                this.populateSupplierFilter(result.data);
                
                // Populate form dropdown
                this.populateSupplierFormDropdown(result.data);
            }
        } catch (error) {
            console.error('Error loading suppliers:', error);
        }
    }

    populateSupplierFilter(suppliers) {
        const select = document.getElementById('supplierFilter');
        if (select) {
            select.innerHTML = '<option value="">All Suppliers</option>';
            suppliers.forEach(supplier => {
                select.innerHTML += `<option value="${supplier.id}">${supplier.name}</option>`;
            });
        }
    }

    populateSupplierFormDropdown(suppliers) {
        const select = document.getElementById('supplierId');
        if (select) {
            select.innerHTML = '<option value="">Select Supplier</option>';
            suppliers.forEach(supplier => {
                select.innerHTML += `<option value="${supplier.id}">${supplier.name}</option>`;
            });
        }
    }

    // ===== CRUD OPERATIONS =====

    async toggleItemStatus(id, isActive) {
        try {
            console.log(`Toggling item ${id} to status: ${isActive}`);
            const response = await fetch(`/api/item/${id}/toggle-status`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ isActive: isActive })
            });

            const data = await response.json();
            
            if (data.success) {
                this.showSuccess(data.message);
                this.loadItems();
                this.loadDashboard();
            } else {
                this.showError(data.message);
            }
        } catch (error) {
            console.error('Error updating item status:', error);
            this.showError('Error updating item status');
        }
    }

    async deleteItem(id) {
        if (!confirm('Are you sure you want to delete this item?')) {
            return;
        }

        try {
            const response = await fetch(`/api/item/${id}`, {
                method: 'DELETE'
            });

            const data = await response.json();
            
            if (data.success) {
                this.showSuccess(data.message);
                this.loadItems();
                this.loadDashboard();
            } else {
                this.showError(data.message);
            }
        } catch (error) {
            console.error('Error deleting item:', error);
            this.showError('Error deleting item');
        }
    }

    // ===== MODAL FUNCTIONS =====

    showCreateModal() {
        this.isEditMode = false;
        document.getElementById('itemModalTitle').textContent = 'Add New Item';
        this.resetForm();
        const modal = new bootstrap.Modal(document.getElementById('itemModal'));
        modal.show();
    }

    showEditModal(item) {
        this.isEditMode = true;
        document.getElementById('itemModalTitle').textContent = 'Edit Item';
        this.populateForm(item);
        const modal = new bootstrap.Modal(document.getElementById('itemModal'));
        modal.show();
    }

    showViewModal(item) {
        const container = document.getElementById('itemDetailsContainer');
        if (!container) return;

        const supplier = item.supplier || {};
        const stock = item.stock || { totalQuantity: 0, totalValue: 0, locationCount: 0 };
        const inventoryBreakdown = Array.isArray(item.inventoryBreakdown) ? item.inventoryBreakdown : [];
        const purchaseHistory = Array.isArray(item.purchaseHistory) ? item.purchaseHistory : [];
        const salesHistory = Array.isArray(item.salesHistory) ? item.salesHistory : [];

        const formatDateTime = (value) => value ? new Date(value).toLocaleString('id-ID') : 'N/A';
        const marginValue = (item.standardPrice || 0) - (item.purchasePrice || 0);
        const marginPercent = item.purchasePrice > 0
            ? ((item.standardPrice - item.purchasePrice) / item.purchasePrice) * 100
            : 0;

        const inventoryTable = inventoryBreakdown.length > 0
            ? `
                <div class="table-responsive">
                    <table class="table table-sm table-striped">
                        <thead class="table-light">
                            <tr>
                                <th>Location</th>
                                <th class="text-end">Quantity</th>
                                <th>Last Updated</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${inventoryBreakdown.map(loc => `
                                <tr>
                                    <td>
                                        <strong>${loc.locationCode}</strong><br>
                                        <small class="text-muted">${loc.locationName || '-'}</small>
                                    </td>
                                    <td class="text-end">${this.formatNumber(loc.quantity || 0)}</td>
                                    <td>${formatDateTime(loc.lastUpdated)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>`
            : '<div class="alert alert-light mb-0">No inventory distribution data available.</div>';

        const purchaseTable = purchaseHistory.length > 0
            ? `
                <div class="table-responsive">
                    <table class="table table-sm table-hover">
                        <thead class="table-light">
                            <tr>
                                <th>PO Number</th>
                                <th>Date</th>
                                <th>Status</th>
                                <th class="text-end">Quantity</th>
                                <th class="text-end">Total Cost</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${purchaseHistory.map(po => `
                                <tr>
                                    <td><span class="badge bg-primary">${po.poNumber || '-'}</span></td>
                                    <td>${formatDateTime(po.orderDate)}</td>
                                    <td><span class="badge bg-info">${po.status || '-'}</span></td>
                                    <td class="text-end">${this.formatNumber(po.quantity || 0)}</td>
                                    <td class="text-end">${this.formatCurrency(po.totalCost || 0)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>`
            : '<div class="alert alert-light mb-0">No recent purchase orders for this item.</div>';

        const salesTable = salesHistory.length > 0
            ? `
                <div class="table-responsive">
                    <table class="table table-sm table-hover">
                        <thead class="table-light">
                            <tr>
                                <th>SO Number</th>
                                <th>Date</th>
                                <th>Status</th>
                                <th class="text-end">Quantity</th>
                                <th class="text-end">Total Value</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${salesHistory.map(so => `
                                <tr>
                                    <td><span class="badge bg-success">${so.soNumber || '-'}</span></td>
                                    <td>${formatDateTime(so.orderDate)}</td>
                                    <td><span class="badge bg-secondary">${so.status || '-'}</span></td>
                                    <td class="text-end">${this.formatNumber(so.quantity || 0)}</td>
                                    <td class="text-end">${this.formatCurrency(so.totalValue || 0)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>`
            : '<div class="alert alert-light mb-0">No recent sales orders for this item.</div>';

        const content = `
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-info-circle me-2"></i>Basic Information</h6>
                            <dl class="row mb-0">
                                <dt class="col-sm-4">Item Code</dt>
                                <dd class="col-sm-8"><span class="badge bg-primary">${item.itemCode || 'N/A'}</span></dd>
                                <dt class="col-sm-4">Name</dt>
                                <dd class="col-sm-8">${item.name || '-'}</dd>
                                <dt class="col-sm-4">Description</dt>
                                <dd class="col-sm-8">${item.description || 'No description available'}</dd>
                                <dt class="col-sm-4">Unit</dt>
                                <dd class="col-sm-8">${item.unit || '-'}</dd>
                                <dt class="col-sm-4">Status</dt>
                                <dd class="col-sm-8">
                                    <span class="badge ${item.isActive ? 'bg-success' : 'bg-secondary'}">
                                        ${item.isActive ? 'Active' : 'Inactive'}
                                    </span>
                                </dd>
                                <dt class="col-sm-4">Created</dt>
                                <dd class="col-sm-8">${formatDateTime(item.createdDate)}</dd>
                                ${item.modifiedDate ? `
                                    <dt class="col-sm-4">Modified</dt>
                                    <dd class="col-sm-8">${formatDateTime(item.modifiedDate)}</dd>
                                ` : ''}
                            </dl>
                        </div>
                    </div>
                </div>
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-handshake me-2"></i>Supplier & Pricing</h6>
                            <div class="row">
                                <div class="col-sm-6">
                                    <p class="text-muted mb-1">Purchase Price</p>
                                    <h5>${this.formatCurrency(item.purchasePrice || 0)}</h5>
                                    <p class="text-muted mb-1">Standard Price</p>
                                    <h5>${this.formatCurrency(item.standardPrice || 0)}</h5>
                                    <p class="text-muted mb-1">Margin</p>
                                    <h6>${this.formatCurrency(marginValue)} <small class="text-muted">(${marginPercent.toFixed(2)}%)</small></h6>
                                </div>
                                <div class="col-sm-6">
                                    <p class="text-muted mb-1">Supplier</p>
                                    <h6>${supplier.name || 'Unknown Supplier'}</h6>
                                    <p class="mb-1"><i class="fas fa-envelope me-1"></i>${supplier.email || 'N/A'}</p>
                                    <p class="mb-1"><i class="fas fa-phone me-1"></i>${supplier.phone || 'N/A'}</p>
                                    <p class="mb-0"><i class="fas fa-map-marker-alt me-1"></i>${supplier.city || 'N/A'}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4 mt-1">
                <div class="col-lg-4">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-boxes me-2"></i>Stock Summary</h6>
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <span>Total Quantity</span>
                                <h5 class="mb-0">${this.formatNumber(stock.totalQuantity || 0)} ${item.unit || ''}</h5>
                            </div>
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <span>Total Value</span>
                                <h5 class="mb-0">${this.formatCurrency(stock.totalValue || 0)}</h5>
                            </div>
                            <div class="d-flex justify-content-between align-items-center">
                                <span>Locations</span>
                                <h5 class="mb-0">${stock.locationCount || 0}</h5>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-lg-8">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-warehouse me-2"></i>Inventory Distribution</h6>
                            ${inventoryTable}
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4 mt-1">
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-file-invoice-dollar me-2"></i>Recent Purchase Orders</h6>
                            ${purchaseTable}
                        </div>
                    </div>
                </div>
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-shopping-cart me-2"></i>Recent Sales Orders</h6>
                            ${salesTable}
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4 mt-1">
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-user me-2"></i>Audit Trail</h6>
                            <p class="mb-1"><strong>Created By:</strong> ${item.createdBy || 'System'}</p>
                            <p class="mb-0"><strong>Modified By:</strong> ${item.modifiedBy || 'N/A'}</p>
                        </div>
                    </div>
                </div>
            </div>
        `;

        container.innerHTML = content;
        const modal = new bootstrap.Modal(document.getElementById('viewItemModal'));
        modal.show();
    }

    resetForm() {
        document.getElementById('itemForm').reset();
        document.getElementById('itemId').value = '';
        document.getElementById('isActive').checked = true;
        this.clearValidation();
        this.clearError();
    }

    populateForm(item) {
        document.getElementById('itemId').value = item.id;
        document.getElementById('itemCode').value = item.itemCode;
        document.getElementById('itemName').value = item.name;
        document.getElementById('itemDescription').value = item.description || '';
        document.getElementById('itemUnit').value = item.unit;
        document.getElementById('purchasePrice').value = item.purchasePrice;
        document.getElementById('standardPrice').value = item.standardPrice;
        document.getElementById('supplierId').value = item.supplierId;
        document.getElementById('isActive').checked = item.isActive;
        this.clearValidation();
    }

    clearValidation() {
        const form = document.getElementById('itemForm');
        const inputs = form.querySelectorAll('.is-invalid');
        inputs.forEach(input => input.classList.remove('is-invalid'));
    }

    // ===== CRUD FUNCTIONS =====

    async saveItem() {
        const form = document.getElementById('itemForm');
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        // Use direct element access for better reliability
        const data = {
            itemCode: document.getElementById('itemCode').value,
            name: document.getElementById('itemName').value,
            description: document.getElementById('itemDescription').value,
            unit: document.getElementById('itemUnit').value,
            purchasePrice: parseFloat(document.getElementById('purchasePrice').value),
            standardPrice: parseFloat(document.getElementById('standardPrice').value),
            supplierId: parseInt(document.getElementById('supplierId').value),
            isActive: document.getElementById('isActive').checked
        };

        try {
            const itemId = document.getElementById('itemId').value;
            const url = itemId ? `/api/item/${itemId}` : '/api/item';
            const method = itemId ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(data)
            });

            // Check response status before parsing JSON
            let result;
            try {
                result = await response.json();
            } catch (jsonError) {
                console.error('Error parsing response:', jsonError);
                this.showError('Error processing server response');
                this.resetSaveButton();
                return;
            }
            
            // Handle both camelCase and PascalCase for response
            const isSuccess = response.ok && (result.success || result.Success);
            
            if (isSuccess) {
                this.showSuccess(result.message || result.Message);
                
                // Close modal - use getOrCreateInstance for reliability
                const modalElement = document.getElementById('itemModal');
                if (modalElement) {
                    try {
                        // Try to get existing instance first
                        let modal = bootstrap.Modal.getInstance(modalElement);
                        
                        // If no instance exists, get or create one
                        if (!modal) {
                            modal = bootstrap.Modal.getOrCreateInstance(modalElement);
                        }
                        
                        // Hide the modal
                        if (modal) {
                            modal.hide();
                        } else {
                            // Fallback: Hide modal directly using Bootstrap 5 native methods
                            modalElement.classList.remove('show');
                            modalElement.setAttribute('aria-hidden', 'true');
                            modalElement.removeAttribute('aria-modal');
                            document.body.classList.remove('modal-open');
                            const backdrop = document.querySelector('.modal-backdrop');
                            if (backdrop) {
                                backdrop.remove();
                            }
                        }
                    } catch (modalError) {
                        console.error('Error closing modal:', modalError);
                        // Fallback: Force hide modal
                        modalElement.classList.remove('show');
                        document.body.classList.remove('modal-open');
                        const backdrop = document.querySelector('.modal-backdrop');
                        if (backdrop) backdrop.remove();
                    }
                }
                
                this.resetForm();
                this.loadItems();
                this.loadDashboard();
            } else {
                // Handle error response
                const errorMessage = result.message || 'Failed to save item';
                this.showError(errorMessage);
                if (result.errors) {
                    this.showValidationErrors(result.errors);
                }
                // Reset button state on error
                this.resetSaveButton();
            }
        } catch (error) {
            console.error('Error saving item:', error);
            this.showError('Error saving item');
            // Reset button state on error
            this.resetSaveButton();
        }
    }

    showValidationErrors(errors) {
        Object.keys(errors).forEach(field => {
            const input = document.getElementById(field);
            if (input) {
                input.classList.add('is-invalid');
                const feedback = input.nextElementSibling;
                if (feedback && feedback.classList.contains('invalid-feedback')) {
                    feedback.textContent = errors[field][0];
                }
            }
        });
    }

    // ===== ITEM ACTIONS =====

    async viewItem(id) {
        try {
            const response = await fetch(`/api/item/${id}`);
            const result = await response.json();
            
            if (result.success) {
                this.showViewModal(result.data);
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error loading item:', error);
            this.showError('Error loading item');
        }
    }

    async editItem(id) {
        try {
            const response = await fetch(`/api/item/${id}`);
            const result = await response.json();
            
            if (result.success) {
                this.showEditModal(result.data);
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error loading item:', error);
            this.showError('Error loading item');
        }
    }

    // ===== SUPPLIER FUNCTIONS =====


    // ===== SUPPLIER SELECTION FUNCTIONS =====

    openSupplierAdvancedSearch() {
        if (window.itemSupplierManager) {
            window.itemSupplierManager.openSupplierAdvancedSearch();
        }
    }

    openSupplierDropdown() {
        if (window.itemSupplierManager) {
            window.itemSupplierManager.openSupplierDropdown();
        }
    }

    searchSuppliersAdvanced() {
        if (window.itemSupplierManager) {
            window.itemSupplierManager.searchSuppliersAdvanced();
        }
    }

    clearAdvancedSearch() {
        if (window.itemSupplierManager) {
            window.itemSupplierManager.clearAdvancedSearch();
        }
    }

    selectSupplier(id, name) {
        document.getElementById('supplierId').value = id;
        document.getElementById('supplierName').value = name;
    }

    // ===== UTILITY FUNCTIONS =====

    formatNumber(number) {
        return new Intl.NumberFormat('id-ID').format(number);
    }

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('id-ID', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    showSuccess(message) {
        // You can implement toast notification here
        console.log('Success:', message);
        // Remove alert() - success should not show pop-up
    }

    showError(message) {
        // Prevent duplicate error display
        if (this.isShowingError) {
            console.warn('showError already in progress, ignoring duplicate call:', message);
            return;
        }
        
        this.isShowingError = true;
        console.error('Error:', message);
        
        const modal = document.getElementById('itemModal');
        if (!modal) {
            console.error('itemModal not found');
            this.isShowingError = false;
            return;
        }
        
        const isModalShown = modal.classList.contains('show');
        
        // Only show modal if it's not already shown
        if (!isModalShown) {
            const bootstrapModal = bootstrap.Modal.getInstance(modal) || new bootstrap.Modal(modal);
            bootstrapModal.show();
        }
        
        // Function to display error message (reusable)
        const displayError = (element) => {
            if (element) {
                element.textContent = message;
                element.style.display = 'block';
                element.classList.remove('d-none');
                console.log('Error message displayed in errorMessage element');
                this.isShowingError = false; // Reset flag after successful display
                return true;
            }
            return false;
        };
        
        // Try to find and display error immediately
        let errorElement = modal.querySelector('#errorMessage');
        if (displayError(errorElement)) {
            return; // Successfully displayed, exit early
        }
        
        // If not found, wait for modal to fully render (only one setTimeout, no duplicates)
        const retryDelay = isModalShown ? 100 : 200;
        setTimeout(() => {
            const retryElement = modal.querySelector('#errorMessage');
            if (displayError(retryElement)) {
                return; // Successfully displayed
            }
            // Element not found - log error but don't use alert
            console.error('errorMessage element not found in modal. Error message:', message);
            console.error('Please check that errorMessage element exists in itemModal');
            this.isShowingError = false; // Reset flag even if display failed
        }, retryDelay);
    }

    resetSaveButton() {
        // Try to find save button by ID first
        let saveBtn = document.getElementById('saveItemBtn');
        
        // If not found, try to find submit button in form
        if (!saveBtn) {
            const form = document.getElementById('itemForm');
            if (form) {
                saveBtn = form.querySelector('button[type="submit"]');
            }
        }
        
        if (saveBtn) {
            saveBtn.disabled = false;
            const originalText = saveBtn.getAttribute('data-original-text') || '<i class="fas fa-save me-2"></i>Save Item';
            saveBtn.innerHTML = originalText;
        }
    }

    clearError() {
        let errorElement = document.getElementById('errorMessage');
        
        // If not found, try to find it in the modal
        if (!errorElement) {
            const modal = document.getElementById('itemModal');
            if (modal) {
                errorElement = modal.querySelector('#errorMessage');
            }
        }
        
        if (errorElement) {
            errorElement.textContent = '';
            errorElement.style.display = 'none';
            errorElement.classList.add('d-none');
        }
        
        // Reset error display flag
        this.isShowingError = false;
    }
}

// Global functions untuk onclick handlers
function sortTable(column) {
    if (window.itemManager) {
        window.itemManager.sortTable(column);
    }
}

function loadPage(page) {
    if (window.itemManager) {
        window.itemManager.loadPage(page);
    }
}

function changePageSize(newPageSize) {
    if (window.itemManager) {
        window.itemManager.changePageSize(newPageSize);
    }
}

function showCreateModal() {
    if (window.itemManager) {
        window.itemManager.showCreateModal();
    }
}

function editItem(id) {
    if (window.itemManager) {
        window.itemManager.editItem(id);
    }
}

function viewItem(id) {
    if (window.itemManager) {
        window.itemManager.viewItem(id);
    }
}

function toggleItemStatus(id, isActive) {
    if (window.itemManager) {
        window.itemManager.toggleItemStatus(id, isActive);
    }
}

function deleteItem(id) {
    if (window.itemManager) {
        window.itemManager.deleteItem(id);
    }
}

function saveItem() {
    if (window.itemManager) {
        window.itemManager.saveItem();
    }
}

function openSupplierAdvancedSearch() {
    if (window.itemManager) {
        window.itemManager.openSupplierAdvancedSearch();
    }
}

function openSupplierDropdown() {
    if (window.itemManager) {
        window.itemManager.openSupplierDropdown();
    }
}

function searchSuppliersAdvanced() {
    if (window.itemManager) {
        window.itemManager.searchSuppliersAdvanced();
    }
}

function clearAdvancedSearch() {
    if (window.itemManager) {
        window.itemManager.clearAdvancedSearch();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.itemManager = new ItemManager();
});
