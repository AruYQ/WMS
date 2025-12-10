/**
 * Inventory Manager - AJAX-based CRUD operations
 * Manages all Inventory operations following the established pattern
 */
class InventoryManager {
    constructor() {
        this.currentInventory = null;
        this.currentInventoryId = null;
        this.currentPage = 1;
        this.pageSize = 10;
        this.currentStatus = null;
        this.currentSearch = '';
        
        this.init();
    }

    /**
     * Initialize the manager
     */
    async init() {
        try {
            console.log('InventoryManager: Initializing...');
            
            // Wait a bit for DOM to be fully ready
            await new Promise(resolve => setTimeout(resolve, 100));
            
            // Bind events first
            this.bindEvents();
            
            // Load initial data
            await this.loadDashboard();
            await this.loadInventories();
            
            console.log('InventoryManager: Initialized successfully');
        } catch (error) {
            console.error('InventoryManager: Initialization failed:', error);
            // Retry initialization after a short delay
            setTimeout(() => {
                if (document.readyState === 'complete') {
                    console.log('InventoryManager: Retrying initialization...');
                    this.init();
                }
            }, 1000);
        }
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        // Search and filter events
        document.getElementById('searchInput')?.addEventListener('input', (e) => {
            this.currentSearch = e.target.value;
            this.debounceSearch();
        });

        document.getElementById('statusFilter')?.addEventListener('change', (e) => {
            this.currentStatus = e.target.value;
            this.currentPage = 1;
            this.loadInventories();
        });

        document.getElementById('pageSizeSelect')?.addEventListener('change', (e) => {
            this.pageSize = parseInt(e.target.value);
            this.currentPage = 1;
            this.loadInventories();
        });


    }

    /**
     * Load dashboard statistics
     */
    async loadDashboard() {
        try {
            const response = await fetch('/api/Inventory/Dashboard');
            const result = await response.json();

            if (result.success && result.data) {
                const data = result.data;
                const totalItemsEl = document.getElementById('totalItems');
                const availableStockEl = document.getElementById('availableStock');
                const lowStockItemsEl = document.getElementById('lowStockItems');
                const outOfStockItemsEl = document.getElementById('outOfStockItems');
                
                if (totalItemsEl) totalItemsEl.textContent = data.totalItems || 0;
                if (availableStockEl) availableStockEl.textContent = (data.availableStock || 0).toLocaleString();
                if (lowStockItemsEl) lowStockItemsEl.textContent = data.lowStockItems || 0;
                if (outOfStockItemsEl) outOfStockItemsEl.textContent = data.outOfStockItems || 0;
            } else {
                console.error('Failed to load dashboard:', result.message);
            }
        } catch (error) {
            console.error('Error loading dashboard:', error);
        }
    }

    /**
     * Load Inventory list with pagination
     */
    async loadInventories() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                ...(this.currentStatus && { status: this.currentStatus }),
                ...(this.currentSearch && { search: this.currentSearch })
            });

            const response = await fetch(`/api/Inventory/List?${params}`);
            const result = await response.json();

            if (result.success) {
                this.renderInventoriesTable(result.data || []);
                this.updatePagination(result.pagination);
            } else {
                this.showError(result.message || 'Failed to load Inventories');
            }
        } catch (error) {
            console.error('Error loading Inventories:', error);
            this.showError('Error loading Inventories');
        }
    }

    /**
     * Render Inventory table - Split into Storage and Other sections
     */
    renderInventoriesTable(inventories) {
        // Group inventories by location category
        const storageInventories = (inventories || []).filter(inv => inv.locationCategory === 'Storage');
        const otherInventories = (inventories || []).filter(inv => inv.locationCategory === 'Other');

        // Render Storage section
        this.renderInventorySection('storageInventoriesTableContainer', storageInventories, 'Storage');
        
        // Render Other section
        this.renderInventorySection('otherInventoriesTableContainer', otherInventories, 'Other');
    }

    /**
     * Render inventory section for a specific category
     */
    renderInventorySection(containerId, inventories, categoryName) {
        const container = document.getElementById(containerId);
        if (!container) return;

        if (!inventories || inventories.length === 0) {
            container.innerHTML = `
                <div class="text-center py-4 text-muted">
                    <i class="fas fa-box-open fa-2x mb-2"></i><br>
                    No inventory found in ${categoryName} locations
                </div>
            `;
            return;
        }

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead class="table-light">
                        <tr>
                            <th style="width: 15%">Item Code</th>
                            <th style="width: 20%">Item Name</th>
                            <th style="width: 15%">Location</th>
                            <th style="width: 10%">Quantity</th>
                            <th style="width: 10%">Status</th>
                            <th style="width: 15%">Last Updated</th>
                            <th style="width: 15%">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        inventories.forEach(inventory => {
            const statusBadge = this.getStatusBadge(inventory.status);
            const quantityBadge = this.getQuantityBadge(inventory.quantity);
            const lastUpdated = new Date(inventory.lastUpdated).toLocaleDateString();
            
            tableHtml += `
                <tr>
                    <td><strong class="text-primary">${this.escapeHtml(inventory.itemCode)}</strong></td>
                    <td>${this.escapeHtml(inventory.itemName)}</td>
                    <td>
                        <div class="fw-medium">${this.escapeHtml(inventory.locationCode)}</div>
                        <small class="text-muted">${this.escapeHtml(inventory.locationName || '')}</small>
                    </td>
                    <td>${quantityBadge}</td>
                    <td>${statusBadge}</td>
                    <td>${lastUpdated}</td>
                    <td>
                        <div class="btn-group" role="group">
                            <button type="button" class="btn btn-sm btn-outline-primary" onclick="inventoryManager.viewInventory(${inventory.id})" title="View Details">
                                <i class="fas fa-eye"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `;
        });

        tableHtml += `
                    </tbody>
                </table>
            </div>
        `;

        container.innerHTML = tableHtml;
    }

    /**
     * Escape HTML to prevent XSS
     */
    escapeHtml(unsafe) {
        if (unsafe == null) return '';
        return String(unsafe)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    /**
     * Update pagination controls
     */
    updatePagination(pagination) {
        // Add null check before destructuring to prevent "Invalid left-hand side assignment" error
        if (!pagination || typeof pagination !== 'object') {
            console.error('Pagination data is missing or invalid:', pagination);
            return;
        }
        
        const { currentPage, totalPages, totalCount, pageSize } = pagination;
        
        // Additional null checks for destructured values
        const safeCurrentPage = currentPage || 1;
        const safeTotalPages = totalPages || 1;
        const safeTotalCount = totalCount || 0;
        const safePageSize = pageSize || 10;
        
        const start = (safeCurrentPage - 1) * safePageSize + 1;
        const end = Math.min(safeCurrentPage * safePageSize, safeTotalCount);
        
        // Update pagination info with null safety
        const showingStartEl = document.getElementById('showingStart');
        const showingEndEl = document.getElementById('showingEnd');
        const totalRecordsEl = document.getElementById('totalRecords');
        const currentPageNumEl = document.getElementById('currentPageNum');
        const totalPagesNumEl = document.getElementById('totalPagesNum');
        
        if (showingStartEl) showingStartEl.textContent = start;
        if (showingEndEl) showingEndEl.textContent = end;
        if (totalRecordsEl) totalRecordsEl.textContent = safeTotalCount;
        if (currentPageNumEl) currentPageNumEl.textContent = safeCurrentPage;
        if (totalPagesNumEl) totalPagesNumEl.textContent = safeTotalPages;
        
        // Update pagination buttons
        const prevBtn = document.getElementById('prevPageBtn');
        const nextBtn = document.getElementById('nextPageBtn');
        
        if (prevBtn) {
            prevBtn.disabled = safeCurrentPage <= 1;
            prevBtn.onclick = safeCurrentPage <= 1 ? null : () => this.previousPage();
        }
        
        if (nextBtn) {
            nextBtn.disabled = safeCurrentPage >= safeTotalPages;
            nextBtn.onclick = safeCurrentPage >= safeTotalPages ? null : () => this.nextPage();
        }
    }

    /**
     * Show create modal
     */
    showCreateModal() {
        this.resetForm();
        document.getElementById('inventoryModalTitle').textContent = 'Create New Inventory';
        const modal = new bootstrap.Modal(document.getElementById('inventoryModal'));
        modal.show();
    }

    /**
     * View Inventory details
     */
    async viewInventory(id) {
        try {
            const response = await fetch(`/api/Inventory/${id}`);
            const result = await response.json();

            if (result.success) {
                const inventory = result.data;
                this.populateViewModal(inventory);
                const modal = new bootstrap.Modal(document.getElementById('viewInventoryModal'));
                modal.show();
            } else {
                this.showError(result.message || 'Failed to load Inventory details');
            }
        } catch (error) {
            console.error('Error loading Inventory details:', error);
            this.showError('Error loading Inventory details');
        }
    }

    /**
     * Edit Inventory
     */
    async editInventory(id) {
        try {
            const response = await fetch(`/api/Inventory/${id}`);
            const result = await response.json();

            if (result.success) {
                const inventory = result.data;
                this.populateForm(inventory);
                document.getElementById('inventoryModalTitle').textContent = 'Edit Inventory';
                this.currentInventoryId = id;
                const modal = new bootstrap.Modal(document.getElementById('inventoryModal'));
                modal.show();
            } else {
                this.showError(result.message || 'Failed to load Inventory for editing');
            }
        } catch (error) {
            console.error('Error loading Inventory for editing:', error);
            this.showError('Error loading Inventory for editing');
        }
    }

    /**
     * Adjust Inventory quantity
     */
    async adjustInventory(id) {
        try {
            const response = await fetch(`/api/Inventory/${id}`);
            const result = await response.json();

            if (result.success) {
                const inventory = result.data;
                this.populateAdjustModal(inventory);
                const modal = new bootstrap.Modal(document.getElementById('adjustInventoryModal'));
                modal.show();
            } else {
                this.showError(result.message || 'Failed to load Inventory for adjustment');
            }
        } catch (error) {
            console.error('Error loading Inventory for adjustment:', error);
            this.showError('Error loading Inventory for adjustment');
        }
    }

    /**
     * Save Inventory (create or update)
     */
    async saveInventory() {
        try {
            const formData = new FormData(document.getElementById('inventoryForm'));
            const data = {
                itemId: parseInt(formData.get('itemId')),
                locationId: parseInt(formData.get('locationId')),
                quantity: parseFloat(formData.get('quantity')),
                status: formData.get('status'),
                sourceReference: formData.get('sourceReference'),
                notes: formData.get('notes')
            };

            const url = this.currentInventoryId ? `/api/Inventory/${this.currentInventoryId}` : '/api/Inventory';
            const method = this.currentInventoryId ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data)
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message || 'Inventory saved successfully');
                this.resetForm();
                bootstrap.Modal.getInstance(document.getElementById('inventoryModal')).hide();
                await this.loadInventories();
                await this.loadDashboard();
            } else {
                this.showError(result.message || 'Failed to save Inventory');
            }
        } catch (error) {
            console.error('Error saving Inventory:', error);
            this.showError('Error saving Inventory');
        }
    }

    /**
     * Save Inventory adjustment
     */
    async saveAdjustment() {
        try {
            const formData = new FormData(document.getElementById('adjustInventoryForm'));
            const data = {
                quantity: parseFloat(formData.get('adjustmentQuantity')),
                adjustmentType: formData.get('adjustmentType'),
                reason: formData.get('adjustmentReason')
            };

            const response = await fetch(`/api/Inventory/${this.currentAdjustmentId}/adjust`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data)
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message || 'Inventory adjusted successfully');
                bootstrap.Modal.getInstance(document.getElementById('adjustInventoryModal')).hide();
                await this.loadInventories();
                await this.loadDashboard();
            } else {
                this.showError(result.message || 'Failed to adjust Inventory');
            }
        } catch (error) {
            console.error('Error adjusting Inventory:', error);
            this.showError('Error adjusting Inventory');
        }
    }


    /**
     * Populate form with Inventory data
     */
    populateForm(inventory) {
        this.setElementValue('itemId', inventory.itemId || '');
        this.setElementValue('locationId', inventory.locationId || '');
        this.setElementValue('quantity', inventory.quantity || '');
        this.setElementValue('status', inventory.status || 'Available');
        this.setElementValue('sourceReference', inventory.sourceReference || '');
        this.setElementValue('notes', inventory.notes || '');
    }

    /**
     * Helper method to safely set element value
     */
    setElementValue(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            element.value = value;
        }
    }

    /**
     * Populate view modal with Inventory data
     */
    populateViewModal(inventory) {
        document.getElementById('viewItemCode').textContent = inventory.itemCode;
        document.getElementById('viewItemName').textContent = inventory.itemName;
        document.getElementById('viewLocationCode').textContent = inventory.locationCode;
        document.getElementById('viewLocationName').textContent = inventory.locationName;
        const viewQuantityEl = document.getElementById('viewQuantity');
        const viewStatusEl = document.getElementById('viewStatus');
        const viewLastUpdatedEl = document.getElementById('viewLastUpdated');
        
        if (viewQuantityEl) viewQuantityEl.textContent = (inventory.quantity || 0).toLocaleString();
        if (viewStatusEl) viewStatusEl.innerHTML = this.getStatusBadge(inventory.status);
        if (viewLastUpdatedEl && inventory.lastUpdated) {
            viewLastUpdatedEl.textContent = new Date(inventory.lastUpdated).toLocaleString();
        }
        document.getElementById('viewSourceReference').textContent = inventory.sourceReference || 'N/A';
        document.getElementById('viewNotes').textContent = inventory.notes || 'N/A';
    }

    /**
     * Populate adjust modal with Inventory data
     */
    populateAdjustModal(inventory) {
        this.currentAdjustmentId = inventory.id;
        document.getElementById('adjustItemCode').textContent = inventory.itemCode;
        document.getElementById('adjustItemName').textContent = inventory.itemName;
        document.getElementById('adjustLocationCode').textContent = inventory.locationCode;
        const adjustCurrentQuantityEl = document.getElementById('adjustCurrentQuantity');
        if (adjustCurrentQuantityEl) {
            adjustCurrentQuantityEl.textContent = (inventory.quantity || 0).toLocaleString();
        }
        document.getElementById('adjustInventoryForm').reset();
    }

    /**
     * Reset form
     */
    resetForm() {
        document.getElementById('inventoryForm').reset();
        this.currentInventoryId = null;
        this.clearError();
        this.clearSuccess();
    }

    /**
     * Pagination methods
     */
    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadInventories();
        }
    }

    nextPage() {
        this.currentPage++;
        this.loadInventories();
    }

    /**
     * Debounced search
     */
    debounceSearch() {
        clearTimeout(this.searchTimeout);
        this.searchTimeout = setTimeout(() => {
            this.currentPage = 1;
            this.loadInventories();
        }, 500);
    }

    /**
     * Utility methods
     */
    getStatusBadge(status) {
        const statusMap = {
            'Available': 'badge bg-success',
            'Reserved': 'badge bg-warning',
            'Damaged': 'badge bg-danger',
            'Quarantine': 'badge bg-secondary'
        };
        return `<span class="${statusMap[status] || 'badge bg-secondary'}">${status}</span>`;
    }

    getQuantityBadge(quantity) {
        if (quantity === 0) {
            return '<span class="badge bg-danger">Out of Stock</span>';
        } else if (quantity <= 10) {
            return `<span class="badge bg-warning">${quantity}</span>`;
        } else {
            return `<span class="badge bg-success">${quantity}</span>`;
        }
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
    if (window.inventoryManager) {
        window.inventoryManager.previousPage();
    }
}

function nextPage() {
    if (window.inventoryManager) {
        window.inventoryManager.nextPage();
    }
}

function changePageSize() {
    if (window.inventoryManager) {
        const pageSize = document.getElementById('pageSizeSelect')?.value;
        if (pageSize) {
            window.inventoryManager.pageSize = parseInt(pageSize);
            window.inventoryManager.currentPage = 1;
            window.inventoryManager.loadInventories();
        }
    }
}

function saveAdjustment() {
    if (window.inventoryManager) {
        window.inventoryManager.saveAdjustment();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    try {
        window.inventoryManager = new InventoryManager();
    } catch (error) {
        console.error('Error creating InventoryManager:', error);
        // Retry after DOM is fully ready
        setTimeout(() => {
            try {
                window.inventoryManager = new InventoryManager();
            } catch (retryError) {
                console.error('Failed to initialize InventoryManager after retry:', retryError);
            }
        }, 500);
    }
});

