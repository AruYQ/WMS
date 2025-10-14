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
        const container = document.getElementById('itemStatistics');
        if (!container) return;

        container.innerHTML = `
            <div class="col-md-3">
                <div class="card bg-primary text-white">
                    <div class="card-body">
                        <div class="d-flex justify-content-between">
                            <div>
                                <h4 class="mb-0">${stats.totalItems}</h4>
                                <p class="mb-0">Total Item</p>
                            </div>
                            <div class="align-self-center">
                                <i class="fas fa-boxes fa-2x"></i>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card bg-success text-white">
                    <div class="card-body">
                        <div class="d-flex justify-content-between">
                            <div>
                                <h4 class="mb-0">${stats.activeItems}</h4>
                                <p class="mb-0">Item Aktif</p>
                            </div>
                            <div class="align-self-center">
                                <i class="fas fa-check-circle fa-2x"></i>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card bg-warning text-white">
                    <div class="card-body">
                        <div class="d-flex justify-content-between">
                            <div>
                                <h4 class="mb-0">${stats.inactiveItems}</h4>
                                <p class="mb-0">Item Tidak Aktif</p>
                            </div>
                            <div class="align-self-center">
                                <i class="fas fa-times-circle fa-2x"></i>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card bg-info text-white">
                    <div class="card-body">
                        <div class="d-flex justify-content-between">
                            <div>
                                <h4 class="mb-0">${stats.totalSuppliers}</h4>
                                <p class="mb-0">Total Supplier</p>
                            </div>
                            <div class="align-self-center">
                                <i class="fas fa-truck fa-2x"></i>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
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
                this.renderPagination(data.totalPages, data.totalCount);
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
        const tbody = document.getElementById('itemsTableBody');
        if (!tbody) return;

        if (items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="10" class="text-center text-muted py-4">No items found</td></tr>';
            return;
        }

        tbody.innerHTML = items.map(item => `
            <tr>
                <td><strong>${item.itemCode}</strong></td>
                <td>${item.name}</td>
                <td>${item.description || '-'}</td>
                <td><span class="badge bg-secondary">${item.unit}</span></td>
                <td>Rp ${this.formatNumber(item.standardPrice)}</td>
                <td>${item.supplierName}</td>
                <td><span class="badge ${item.totalStock > 10 ? 'bg-success' : item.totalStock > 0 ? 'bg-warning' : 'bg-danger'}">${item.totalStock}</span></td>
                <td>Rp ${this.formatNumber(item.totalValue)}</td>
                <td>
                    <span class="badge ${item.isActive ? 'bg-success' : 'bg-secondary'}">
                        ${item.isActive ? 'Active' : 'Inactive'}
                    </span>
                </td>
                <td>
                    <div class="btn-group btn-group-sm" role="group">
                        <button type="button" class="btn btn-outline-info" onclick="itemManager.viewItem(${item.id})" title="View">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button type="button" class="btn btn-outline-primary" onclick="itemManager.editItem(${item.id})" title="Edit">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button type="button" class="btn btn-outline-warning" onclick="itemManager.toggleItemStatus(${item.id}, ${!item.isActive})" title="Toggle Status">
                            <i class="fas fa-${item.isActive ? 'pause' : 'play'}"></i>
                        </button>
                        <button type="button" class="btn btn-outline-danger" onclick="itemManager.deleteItem(${item.id})" title="Delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    renderPagination(totalPages, totalCount) {
        const container = document.getElementById('itemsPagination');
        if (!container) return;

        if (totalPages <= 1) {
            container.innerHTML = '';
            return;
        }

        let paginationHTML = '<ul class="pagination pagination-sm justify-content-center">';
        
        // Previous button
        if (this.currentPage > 1) {
            paginationHTML += `<li class="page-item">
                <button class="page-link" onclick="itemManager.loadPage(${this.currentPage - 1})">Previous</button>
            </li>`;
        }

        // Page numbers
        const startPage = Math.max(1, this.currentPage - 2);
        const endPage = Math.min(totalPages, this.currentPage + 2);

        for (let i = startPage; i <= endPage; i++) {
            paginationHTML += `<li class="page-item ${i === this.currentPage ? 'active' : ''}">
                <button class="page-link" onclick="itemManager.loadPage(${i})">${i}</button>
            </li>`;
        }

        // Next button
        if (this.currentPage < totalPages) {
            paginationHTML += `<li class="page-item">
                <button class="page-link" onclick="itemManager.loadPage(${this.currentPage + 1})">Next</button>
            </li>`;
        }

        paginationHTML += '</ul>';
        container.innerHTML = paginationHTML;
    }

    loadPage(page) {
        this.currentPage = page;
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

    // ===== SUPPLIER FUNCTIONS =====

    async loadSuppliers() {
        try {
            const response = await fetch('/Item/GetSuppliers');
            const data = await response.json();
            
            if (data.success) {
                this.populateSupplierFilter(data.data);
            }
        } catch (error) {
            console.error('Error loading suppliers:', error);
        }
    }

    populateSupplierFilter(suppliers) {
        const select = document.getElementById('supplierFilter');
        if (!select) return;

        select.innerHTML = '<option value="">All Suppliers</option>' +
            suppliers.map(supplier => 
                `<option value="${supplier.id}">${supplier.name}</option>`
            ).join('');
    }

    // ===== MODAL FUNCTIONS =====

    showCreateModal() {
        this.isEditMode = false;
        document.getElementById('itemModalTitle').innerHTML = '<i class="fas fa-plus me-2"></i>Tambah Item';
        document.getElementById('itemForm').reset();
        document.getElementById('itemId').value = '';
        document.getElementById('supplierId').value = '';
        document.getElementById('supplierName').value = '';
        
        const modal = new bootstrap.Modal(document.getElementById('itemModal'));
        modal.show();
    }

    async editItem(id) {
        try {
            const response = await fetch(`/api/item/${id}`);
            const data = await response.json();
            
            if (data.success) {
                this.isEditMode = true;
                document.getElementById('itemModalTitle').innerHTML = '<i class="fas fa-edit me-2"></i>Edit Item';
                
                const item = data.data;
                document.getElementById('itemId').value = item.id;
                document.getElementById('itemCode').value = item.itemCode;
                document.getElementById('itemName').value = item.name;
                document.getElementById('itemDescription').value = item.description || '';
                document.getElementById('itemUnit').value = item.unit;
                document.getElementById('itemPrice').value = item.standardPrice;
                document.getElementById('supplierId').value = item.supplierId;
                document.getElementById('supplierName').value = item.supplierName;
                document.getElementById('itemIsActive').checked = item.isActive;
                
                const modal = new bootstrap.Modal(document.getElementById('itemModal'));
                modal.show();
            } else {
                this.showError(data.message);
            }
        } catch (error) {
            console.error('Error loading item:', error);
            this.showError('Error loading item');
        }
    }

    async viewItem(id) {
        try {
            const response = await fetch(`/api/item/${id}`);
            const data = await response.json();
            
            if (data.success) {
                const item = data.data;
                const content = document.getElementById('viewItemContent');
                content.innerHTML = `
                    <div class="row">
                        <div class="col-md-6">
                            <h6>Basic Information</h6>
                            <table class="table table-sm">
                                <tr><td><strong>Item Code:</strong></td><td>${item.itemCode}</td></tr>
                                <tr><td><strong>Name:</strong></td><td>${item.name}</td></tr>
                                <tr><td><strong>Description:</strong></td><td>${item.description || '-'}</td></tr>
                                <tr><td><strong>Unit:</strong></td><td><span class="badge bg-secondary">${item.unit}</span></td></tr>
                                <tr><td><strong>Price:</strong></td><td>Rp ${this.formatNumber(item.standardPrice)}</td></tr>
                                <tr><td><strong>Status:</strong></td><td><span class="badge ${item.isActive ? 'bg-success' : 'bg-secondary'}">${item.isActive ? 'Active' : 'Inactive'}</span></td></tr>
                            </table>
                        </div>
                        <div class="col-md-6">
                            <h6>Supplier Information</h6>
                            <table class="table table-sm">
                                <tr><td><strong>Supplier:</strong></td><td>${item.supplierName}</td></tr>
                                <tr><td><strong>Total Stock:</strong></td><td><span class="badge ${item.totalStock > 10 ? 'bg-success' : item.totalStock > 0 ? 'bg-warning' : 'bg-danger'}">${item.totalStock}</span></td></tr>
                                <tr><td><strong>Total Value:</strong></td><td>Rp ${this.formatNumber(item.totalValue)}</td></tr>
                            </table>
                            
                            <h6>Audit Information</h6>
                            <table class="table table-sm">
                                <tr><td><strong>Created:</strong></td><td>${this.formatDate(item.createdDate)} by ${item.createdBy || '-'}</td></tr>
                                <tr><td><strong>Modified:</strong></td><td>${item.modifiedDate ? this.formatDate(item.modifiedDate) + ' by ' + (item.modifiedBy || '-') : '-'}</td></tr>
                            </table>
                        </div>
                    </div>
                `;
                
                const modal = new bootstrap.Modal(document.getElementById('viewItemModal'));
                modal.show();
            } else {
                this.showError(data.message);
            }
        } catch (error) {
            console.error('Error loading item:', error);
            this.showError('Error loading item');
        }
    }

    // ===== CRUD OPERATIONS =====

    async saveItem() {
        try {
            const form = document.getElementById('itemForm');
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return;
            }

            const itemData = {
                itemCode: document.getElementById('itemCode').value,
                name: document.getElementById('itemName').value,
                description: document.getElementById('itemDescription').value,
                unit: document.getElementById('itemUnit').value,
                standardPrice: parseFloat(document.getElementById('itemPrice').value),
                supplierId: parseInt(document.getElementById('supplierId').value),
                isActive: document.getElementById('itemIsActive').checked
            };

            const itemId = document.getElementById('itemId').value;
            const url = itemId ? `/api/item/${itemId}` : '/api/item';
            const method = itemId ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(itemData)
            });

            const data = await response.json();
            
            if (data.success) {
                this.showSuccess(data.message);
                const modal = bootstrap.Modal.getInstance(document.getElementById('itemModal'));
                modal.hide();
                this.loadItems();
                this.loadDashboard();
            } else {
                this.showError(data.message);
            }
        } catch (error) {
            console.error('Error saving item:', error);
            this.showError('Error saving item');
        }
    }

    async toggleItemStatus(id, isActive) {
        try {
            const response = await fetch(`/Item/UpdateStatus?id=${id}&isActive=${isActive}`, {
                method: 'POST'
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
        alert(message); // Fallback to alert
    }

    showError(message) {
        // You can implement toast notification here
        console.error('Error:', message);
        alert(message); // Fallback to alert
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
