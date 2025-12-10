/**
 * Supplier Manager - AJAX-based Supplier Management
 * Unified JavaScript module for all Supplier operations
 */

class SupplierManager {
    constructor() {
        this.currentSupplierId = null;
        this.suppliers = [];
        this.statistics = {};
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalCount = 0;
        this.totalPages = 0;
        this.filters = {
            search: '',
            status: ''
        };
        
        // Toast instances for better management
        this.errorToast = null;
        this.successToast = null;
        
        this.init();
    }

    init() {
        this.initializeToasts();
        this.bindEvents();
        this.loadDashboard();
        this.loadSuppliers();
        this.setupRealTimeUpdates();
    }

    initializeToasts() {
        // Initialize toast instances once
        const errorToastElement = document.getElementById('errorToast');
        const successToastElement = document.getElementById('successToast');
        
        if (errorToastElement) {
            this.errorToast = new bootstrap.Toast(errorToastElement);
        }
        if (successToastElement) {
            this.successToast = new bootstrap.Toast(successToastElement);
        }
    }

    bindEvents() {
        // Filter events
        document.getElementById('searchInput')?.addEventListener('input', (e) => {
            this.filters.search = e.target.value;
            this.currentPage = 1;
            this.loadSuppliers();
        });

        document.getElementById('statusFilter')?.addEventListener('change', (e) => {
            this.filters.status = e.target.value;
            this.currentPage = 1;
            this.loadSuppliers();
        });

        // Modal events
        document.getElementById('supplierModal')?.addEventListener('hidden.bs.modal', () => {
            this.resetForm();
        });

        // Form submission prevention
        document.getElementById('supplierForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveSupplier();
        });
    }

    async loadDashboard() {
        try {
            const response = await fetch('/api/supplier/dashboard');
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.statistics = data.data;
                this.updateStatisticsCards();
            } else {
                console.error('Dashboard API Error:', data.message);
                this.showError(data.message);
            }
        } catch (error) {
            console.error('Error loading dashboard:', error);
            this.showError('Failed to load dashboard statistics');
        }
    }

    updateStatisticsCards() {
        document.getElementById('totalSuppliersCount').textContent = this.statistics.totalSuppliers || 0;
        document.getElementById('activeSuppliersCount').textContent = this.statistics.activeSuppliers || 0;
        document.getElementById('suppliersWithPO').textContent = this.statistics.suppliersWithPO || 0;
        document.getElementById('inactiveSuppliersCount').textContent = this.statistics.inactiveSuppliers || 0;
    }

    async loadSuppliers() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                search: this.filters.search || '',
                status: this.filters.status || ''
            });

            const response = await fetch(`/api/supplier?${params}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.suppliers = data.data;
                this.totalCount = data.totalCount;
                this.totalPages = data.totalPages;
                this.renderSuppliersTable();
                this.updatePagination();
            } else {
                console.error('Suppliers API Error:', data.message);
                this.showError(data.message);
            }
        } catch (error) {
            console.error('Error loading suppliers:', error);
            this.showError('Failed to load suppliers');
        }
    }

    renderSuppliersTable() {
        const container = document.getElementById('suppliersTableContainer');
        if (!container) return;

        if (this.suppliers.length === 0) {
            container.innerHTML = '<div class="text-center py-5"><p>No suppliers found</p></div>';
            return;
        }

        let html = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>CODE</th>
                            <th>NAME</th>
                            <th>EMAIL</th>
                            <th>PHONE</th>
                            <th>CONTACT PERSON</th>
                            <th>CITY</th>
                            <th>STATUS</th>
                            <th>ACTIONS</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        this.suppliers.forEach(supplier => {
            html += `
                <tr>
                    <td><strong>${supplier.code}</strong></td>
                    <td>${supplier.name}</td>
                    <td>${supplier.email}</td>
                    <td>${supplier.phone || '-'}</td>
                    <td>${supplier.contactPerson || '-'}</td>
                    <td>${supplier.city || '-'}</td>
                    <td>
                        <span class="badge ${supplier.isActive ? 'bg-success' : 'bg-secondary'}">
                            ${supplier.isActive ? 'Active' : 'Inactive'}
                        </span>
                    </td>
                    <td>
                        <div class="btn-group btn-group-sm" role="group">
                            <button type="button" class="btn btn-sm btn-info" onclick="viewSupplier(${supplier.id})" title="View">
                                <i class="fas fa-eye"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-primary" onclick="editSupplier(${supplier.id})" title="Edit">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-warning" onclick="toggleSupplierStatus(${supplier.id}, ${!supplier.isActive})" title="Toggle Status">
                                <i class="fas fa-${supplier.isActive ? 'pause' : 'play'}"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-danger" onclick="deleteSupplier(${supplier.id})" title="Delete">
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
            <div class="d-flex justify-content-between align-items-center mt-3">
                <div>Showing ${this.suppliers.length} suppliers</div>
                <div>
                    ${this.currentPage > 1 ? `<button class="btn btn-sm btn-primary" onclick="supplierManager.previousPage()">Previous</button>` : ''}
                    <span class="mx-2">Page ${this.currentPage} of ${this.totalPages}</span>
                    ${this.currentPage < this.totalPages ? `<button class="btn btn-sm btn-primary" onclick="supplierManager.nextPage()">Next</button>` : ''}
                </div>
            </div>
        `;

        container.innerHTML = html;
    }

    updatePagination() {
        // Update pagination info
        const showingFrom = document.getElementById('showingFrom');
        const showingTo = document.getElementById('showingTo');
        const totalSuppliers = document.getElementById('totalSuppliers');
        
        if (showingFrom && showingTo && totalSuppliers) {
            const start = (this.currentPage - 1) * this.pageSize + 1;
            const end = Math.min(this.currentPage * this.pageSize, this.totalCount);
            
            showingFrom.textContent = this.totalCount > 0 ? start : 0;
            showingTo.textContent = end;
            totalSuppliers.textContent = this.totalCount;
        }
        
        // Update pagination buttons
        const prevBtn = document.getElementById('prevPageBtn');
        const nextBtn = document.getElementById('nextPageBtn');
        
        if (prevBtn) {
            prevBtn.disabled = this.currentPage <= 1;
        }
        
        if (nextBtn) {
            nextBtn.disabled = this.currentPage >= this.totalPages;
        }
    }

    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadSuppliers();
        }
    }

    nextPage() {
        if (this.currentPage < this.totalPages) {
            this.currentPage++;
            this.loadSuppliers();
        }
    }

    clearFilters() {
        document.getElementById('searchInput').value = '';
        document.getElementById('statusFilter').value = '';
        this.filters.search = '';
        this.filters.status = '';
        this.currentPage = 1;
        this.loadSuppliers();
    }

    // ===== MODAL FUNCTIONS =====

    showCreateModal() {
        this.resetForm();
        document.getElementById('supplierModalTitle').textContent = 'Add New Supplier';
        const modal = new bootstrap.Modal(document.getElementById('supplierModal'));
        modal.show();
    }

    showEditModal(supplier) {
        this.currentSupplierId = supplier.id;
        document.getElementById('supplierModalTitle').textContent = 'Edit Supplier';
        this.populateForm(supplier);
        const modal = new bootstrap.Modal(document.getElementById('supplierModal'));
        modal.show();
    }

    showViewModal(supplier) {
        const metrics = supplier.metrics || {};
        const audit = supplier.audit || {};
        const topItems = Array.isArray(supplier.topItems) ? supplier.topItems : [];
        const recentPurchaseOrders = Array.isArray(supplier.recentPurchaseOrders) ? supplier.recentPurchaseOrders : [];

        const formatDateTime = (value) => value ? new Date(value).toLocaleString('id-ID') : 'N/A';

        const itemsTable = topItems.length > 0
            ? `
                <div class="table-responsive">
                    <table class="table table-sm table-striped">
                        <thead class="table-light">
                            <tr>
                                <th>Item</th>
                                <th>Status</th>
                                <th class="text-end">Stock</th>
                                <th>Last Updated</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${topItems.map(item => `
                                <tr>
                                    <td>
                                        <strong>${item.itemCode}</strong><br>
                                        <small class="text-muted">${item.itemName}</small>
                                    </td>
                                    <td>
                                        <span class="badge ${item.isActive ? 'bg-success' : 'bg-secondary'}">
                                            ${item.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </td>
                                    <td class="text-end">${(item.totalStock || 0).toLocaleString('id-ID')}</td>
                                    <td>${formatDateTime(item.lastUpdated)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>`
            : '<div class="alert alert-light mb-0">No items linked to this supplier.</div>';

        const poTable = recentPurchaseOrders.length > 0
            ? `
                <div class="table-responsive">
                    <table class="table table-sm table-hover">
                        <thead class="table-light">
                            <tr>
                                <th>PO Number</th>
                                <th>Date</th>
                                <th>Status</th>
                                <th class="text-end">Items</th>
                                <th class="text-end">Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${recentPurchaseOrders.map(po => `
                                <tr>
                                    <td><span class="badge bg-primary">${po.poNumber || '-'}</span></td>
                                    <td>${formatDateTime(po.orderDate)}</td>
                                    <td><span class="badge bg-info">${po.status || '-'}</span></td>
                                    <td class="text-end">${po.itemsCount || 0}</td>
                                    <td class="text-end">Rp ${po.totalAmount ? po.totalAmount.toLocaleString('id-ID') : '0'}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>`
            : '<div class="alert alert-light mb-0">No recent purchase orders for this supplier.</div>';

        const content = `
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-info-circle me-2"></i>Basic Information</h6>
                            <dl class="row mb-0">
                                <dt class="col-sm-4">Supplier Code</dt>
                                <dd class="col-sm-8"><span class="badge bg-primary">${supplier.code || 'N/A'}</span></dd>
                                <dt class="col-sm-4">Name</dt>
                                <dd class="col-sm-8">${supplier.name || '-'}</dd>
                                <dt class="col-sm-4">Email</dt>
                                <dd class="col-sm-8">${supplier.email || '-'}</dd>
                                <dt class="col-sm-4">Phone</dt>
                                <dd class="col-sm-8">${supplier.phone || 'N/A'}</dd>
                                <dt class="col-sm-4">Contact</dt>
                                <dd class="col-sm-8">${supplier.contactPerson || 'N/A'}</dd>
                                <dt class="col-sm-4">Status</dt>
                                <dd class="col-sm-8">
                                    <span class="badge ${supplier.isActive ? 'bg-success' : 'bg-secondary'}">
                                        ${supplier.isActive ? 'Active' : 'Inactive'}
                                    </span>
                                </dd>
                            </dl>
                        </div>
                    </div>
                </div>
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-map-marker-alt me-2"></i>Address & Audit</h6>
                            <p class="mb-1"><strong>Address:</strong> ${supplier.address || 'N/A'}</p>
                            <p class="mb-1"><strong>City:</strong> ${supplier.city || 'N/A'}</p>
                            <p class="mb-1"><strong>Created:</strong> ${formatDateTime(audit.createdDate)}</p>
                            ${audit.modifiedDate ? `<p class="mb-1"><strong>Modified:</strong> ${formatDateTime(audit.modifiedDate)}</p>` : ''}
                            <p class="mb-1"><strong>Created By:</strong> ${audit.createdBy || 'System'}</p>
                            <p class="mb-0"><strong>Modified By:</strong> ${audit.modifiedBy || 'N/A'}</p>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4 mt-1">
                <div class="col-lg-4">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-chart-line me-2"></i>Business Metrics</h6>
                            <div class="d-flex justify-content-between mb-2">
                                <span>Total Purchase Orders</span>
                                <strong>${metrics.totalPurchaseOrders || 0}</strong>
                            </div>
                            <div class="d-flex justify-content-between mb-2">
                                <span>Total Spend</span>
                                <strong>Rp ${metrics.totalPurchaseValue ? metrics.totalPurchaseValue.toLocaleString('id-ID') : '0'}</strong>
                            </div>
                            <div class="d-flex justify-content-between mb-2">
                                <span>Items Supplied</span>
                                <strong>${metrics.totalItemsSupplied || 0}</strong>
                            </div>
                            <div class="d-flex justify-content-between">
                                <span>Active Items</span>
                                <strong>${metrics.activeItems || 0}</strong>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-lg-8">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-layer-group me-2"></i>Top Items</h6>
                            ${itemsTable}
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4 mt-1">
                <div class="col-12">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-file-invoice-dollar me-2"></i>Recent Purchase Orders</h6>
                            ${poTable}
                        </div>
                    </div>
                </div>
            </div>
        `;
        document.getElementById('supplierDetailsContainer').innerHTML = content;
        const modal = new bootstrap.Modal(document.getElementById('viewSupplierModal'));
        modal.show();
    }

    resetForm() {
        const form = document.getElementById('supplierForm');
        if (form) {
            form.reset();
        }
        document.getElementById('supplierId').value = '';
        this.clearValidation();
    }

    populateForm(supplier) {
        document.getElementById('supplierId').value = supplier.id;
        document.getElementById('supplierCode').value = supplier.code || '';
        document.getElementById('supplierName').value = supplier.name;
        document.getElementById('supplierEmail').value = supplier.email;
        document.getElementById('supplierPhone').value = supplier.phone || '';
        document.getElementById('contactPerson').value = supplier.contactPerson || '';
        document.getElementById('supplierCity').value = supplier.city || '';
        document.getElementById('supplierAddress').value = supplier.address || '';
        document.getElementById('isActive').checked = supplier.isActive;
        this.clearValidation();
    }

    clearValidation() {
        const form = document.getElementById('supplierForm');
        if (form) {
            const inputs = form.querySelectorAll('.is-invalid');
            inputs.forEach(input => input.classList.remove('is-invalid'));
        }
    }

    // ===== CRUD FUNCTIONS =====

    async saveSupplier() {
        const form = document.getElementById('supplierForm');
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        const formData = new FormData(form);
        const data = {
            code: formData.get('Code'),
            name: formData.get('Name'),
            email: formData.get('Email'),
            phone: formData.get('Phone'),
            contactPerson: formData.get('ContactPerson'),
            city: formData.get('City'),
            address: formData.get('Address'),
            isActive: formData.get('IsActive') === 'on'
        };

        try {
            const supplierId = document.getElementById('supplierId').value;
            const url = supplierId ? `/api/supplier/${supplierId}` : '/api/supplier';
            const method = supplierId ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(data)
            });

            const result = await response.json();
            
            if (result.success) {
                this.showSuccess(result.message);
                const modal = bootstrap.Modal.getInstance(document.getElementById('supplierModal'));
                modal.hide();
                this.loadSuppliers();
                this.loadDashboard();
            } else {
                this.showError(result.message);
                if (result.errors) {
                    this.showValidationErrors(result.errors);
                }
            }
        } catch (error) {
            console.error('Error saving supplier:', error);
            this.showError('Error saving supplier');
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

    async viewSupplier(id) {
        try {
            const response = await fetch(`/api/supplier/${id}`);
            const result = await response.json();
            
            if (result.success) {
                this.showViewModal(result.data);
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error loading supplier:', error);
            this.showError('Error loading supplier');
        }
    }

    async editSupplier(id) {
        try {
            const response = await fetch(`/api/supplier/${id}`);
            const result = await response.json();
            
            if (result.success) {
                this.showEditModal(result.data);
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error loading supplier:', error);
            this.showError('Error loading supplier');
        }
    }

    async toggleSupplierStatus(id, isActive) {
        try {
            const response = await fetch(`/api/supplier/${id}/toggle-status`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ isActive })
            });

            const result = await response.json();
            
            if (result.success) {
                this.showSuccess(result.message);
                this.loadSuppliers();
                this.loadDashboard();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error updating supplier status:', error);
            this.showError('Error updating supplier status');
        }
    }

    async deleteSupplier(id) {
        if (!confirm('Are you sure you want to delete this supplier?')) {
            return;
        }

        try {
            const response = await fetch(`/api/supplier/${id}`, {
                method: 'DELETE'
            });

            const result = await response.json();
            
            if (result.success) {
                this.showSuccess(result.message);
                this.loadSuppliers();
                this.loadDashboard();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error deleting supplier:', error);
            this.showError('Error deleting supplier');
        }
    }

    // ===== UTILITY FUNCTIONS =====

    showSuccess(message) {
        if (this.successToast) {
            document.querySelector('#successToast .toast-body').textContent = message;
            this.successToast.show();
        } else {
            alert('Success: ' + message);
        }
    }

    showError(message) {
        if (this.errorToast) {
            document.querySelector('#errorToast .toast-body').textContent = message;
            this.errorToast.show();
        } else {
            alert('Error: ' + message);
        }
    }

    setupRealTimeUpdates() {
        // Auto-refresh every 30 seconds
        setInterval(() => {
            this.loadDashboard();
            this.loadSuppliers();
        }, 30000);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.supplierManager = new SupplierManager();
});

// Global functions for onclick handlers
function openCreateModal() {
    window.supplierManager.showCreateModal();
}

function saveSupplier() {
    window.supplierManager.saveSupplier();
}

function clearFilters() {
    window.supplierManager.clearFilters();
}

function previousPage() {
    window.supplierManager.previousPage();
}

function nextPage() {
    window.supplierManager.nextPage();
}

function editSupplier(id) {
    window.supplierManager.editSupplier(id);
}

function viewSupplier(id) {
    window.supplierManager.viewSupplier(id);
}

function deleteSupplier(id) {
    window.supplierManager.deleteSupplier(id);
}

function toggleSupplierStatus(id, isActive) {
    window.supplierManager.toggleSupplierStatus(id, isActive);
}
