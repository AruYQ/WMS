/**
 * Picking Manager - AJAX-based Picking operations
 */
class PickingManager {
    constructor() {
        this.currentPicking = null;
        this.currentPickingId = null;
        this.currentPage = 1;
        this.pageSize = 10;
        this.currentStatus = null;
        this.currentSearch = '';
        
        this.init();
    }

    async init() {
        try {
            await this.loadPickings();
            this.bindEvents();
        } catch (error) {
            console.error('PickingManager: Initialization failed:', error);
        }
    }

    bindEvents() {
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            let searchTimeout;
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    this.currentSearch = e.target.value;
                    this.currentPage = 1;
                    this.loadPickings();
                }, 300);
            });
        }

        const statusFilter = document.getElementById('statusFilter');
        if (statusFilter) {
            statusFilter.addEventListener('change', (e) => {
                this.currentStatus = e.target.value;
                this.currentPage = 1;
                this.loadPickings();
            });
        }

        const pageSizeSelect = document.getElementById('pageSizeSelect');
        if (pageSizeSelect) {
            pageSizeSelect.addEventListener('change', (e) => {
                this.pageSize = parseInt(e.target.value);
                this.currentPage = 1;
                this.loadPickings();
            });
        }

        const processPickingForm = document.getElementById('processPickingForm');
        if (processPickingForm) {
            processPickingForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.submitProcessPicking();
            });
        }

        const processPickingModal = document.getElementById('processPickingModal');
        if (processPickingModal) {
            processPickingModal.addEventListener('hidden.bs.modal', () => {
                this.currentPicking = null;
                this.currentPickingId = null;
            });
        }
    }

    async loadPickings() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                ...(this.currentStatus && { status: this.currentStatus }),
                ...(this.currentSearch && { search: this.currentSearch })
            });

            const response = await fetch(`/api/picking?${params}`);
            const result = await response.json();

            if (result.success) {
                this.renderPickingsTable(result.data);
                if (result.pagination) {
                    this.updatePagination(result.pagination);
                }
            } else {
                this.showError(result.message || 'Failed to load Pickings');
                this.renderPickingsTable([]);
            }
        } catch (error) {
            console.error('Error loading Pickings:', error);
            this.showError('Error loading Pickings');
            this.renderPickingsTable([]);
        }
    }

    renderPickingsTable(pickings) {
        const container = document.getElementById('pickingsTableContainer');
        if (!container) return;

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead class="table-light">
                        <tr>
                            <th>Picking Number</th>
                            <th>Sales Order</th>
                            <th>Customer</th>
                            <th>Picking Date</th>
                            <th>Status</th>
                            <th>Progress</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        if (pickings && pickings.length > 0) {
            pickings.forEach(picking => {
                const statusBadge = this.getStatusBadge(picking.status);
                const pickingDate = new Date(picking.pickingDate).toLocaleDateString();
                const completionPct = picking.completionPercentage ? picking.completionPercentage.toFixed(1) : 0;
                
                tableHtml += `
                    <tr>
                        <td><strong>${picking.pickingNumber}</strong></td>
                        <td>${picking.salesOrderNumber || 'N/A'}</td>
                        <td>${picking.customerName || 'N/A'}</td>
                        <td>${pickingDate}</td>
                        <td>${statusBadge}</td>
                        <td>
                            <div class="progress" style="height: 20px;">
                                <div class="progress-bar ${this.getProgressBarClass(picking.status)}" 
                                     role="progressbar" 
                                     style="width: ${completionPct}%">
                                    ${completionPct}%
                                </div>
                            </div>
                            <small class="text-muted">${picking.totalQuantityPicked || 0} / ${picking.totalQuantityRequired || 0}</small>
                        </td>
                        <td>
                            <div class="btn-group" role="group">
                                <button type="button" class="btn btn-sm btn-outline-primary" 
                                        onclick="window.location.href='/Picking/Details/${picking.id}'" 
                                        title="View Details">
                                    <i class="fas fa-eye"></i>
                                </button>
                                ${(picking.status === 'Pending' || picking.status === 'In Progress' || picking.status === 'InProgress') ? `
                                    <button type="button" class="btn btn-sm btn-outline-success" 
                                            onclick="window.pickingManager ? window.pickingManager.processPicking(${picking.id}) : alert('Please wait for page to load completely')" 
                                            title="Process">
                                        <i class="fas fa-play"></i>
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
                    <td colspan="7" class="text-center text-muted py-4">
                        <i class="fas fa-inbox fa-2x mb-2"></i><br>
                        No Pickings found
                    </td>
                </tr>
            `;
        }

        tableHtml += `</tbody></table></div>`;
        container.innerHTML = tableHtml;
    }

    getStatusBadge(status) {
        const badges = {
            'Pending': '<span class="badge bg-secondary">Pending</span>',
            'InProgress': '<span class="badge bg-warning">In Progress</span>',
            'In Progress': '<span class="badge bg-warning">In Progress</span>',
            'Completed': '<span class="badge bg-success">Completed</span>',
            'Cancelled': '<span class="badge bg-danger">Cancelled</span>'
        };
        return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
    }

    getProgressBarClass(status) {
        if (status === 'Completed') return 'bg-success';
        if (status === 'InProgress' || status === 'In Progress') return 'bg-warning';
        return 'bg-secondary';
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
            this.loadPickings();
        }
    }

    nextPage() {
        this.currentPage++;
        this.loadPickings();
    }

    async viewPicking(id) {
        if (!id) {
            console.error('Picking ID is required');
            this.showError('Invalid picking ID');
            return;
        }

        // Navigate to Details page instead of opening modal
        window.location.href = `/Picking/Details/${id}`;
    }

    async processPicking(id) {
        if (!id) {
            console.error('Picking ID is required');
            this.showError('Invalid picking ID');
            return;
        }

        try {
            console.log('Loading picking with ID:', id);
            const response = await fetch(`/api/picking/${id}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const result = await response.json();

            if (result.success) {
                this.currentPicking = result.data;
                this.currentPickingId = id;
                
                const modalElement = document.getElementById('processPickingModal');
                if (!modalElement) {
                    console.error('Process picking modal not found');
                    this.showError('Modal not found. Please refresh the page.');
                    return;
                }
                
                // Wait for async populate to complete before showing modal
                await this.populateProcessPickingModal(result.data);
                
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
            } else {
                this.showError(result.message || 'Failed to load Picking');
            }
        } catch (error) {
            console.error('Error loading Picking:', error);
            this.showError('Error loading Picking details: ' + error.message);
        }
    }

    async populateProcessPickingModal(picking) {
        document.getElementById('processPickingNumber').textContent = picking.pickingNumber;
        document.getElementById('processSONumber').textContent = picking.salesOrderNumber;
        document.getElementById('processCustomerName').textContent = picking.customerName;
        document.getElementById('processHoldingLocation').textContent = picking.holdingLocationName || 'N/A';

        const itemsToPickBody = document.getElementById('processPickingDetailsBody');
        const completedItemsBody = document.getElementById('processCompletedItemsBody');
        if (!itemsToPickBody || !completedItemsBody) return;

        // Separate items
        const itemsToPick = picking.details.filter(d => d.status !== 'Picked');
        const completedItems = picking.details.filter(d => d.status === 'Picked');

        // Update counts
        document.getElementById('itemsToPickCount').textContent = itemsToPick.length;
        document.getElementById('completedItemsCount').textContent = completedItems.length;

        // Render items to pick with location dropdowns
        let itemsToPickHtml = '';
        if (itemsToPick.length === 0) {
            itemsToPickHtml = '<tr><td colspan="7" class="text-center py-4 text-muted">All items have been picked</td></tr>';
        } else {
            for (const detail of itemsToPick) {
                // Load available locations for this item
                const locations = await this.loadAvailableLocations(detail.itemId, detail.remainingQuantity || detail.quantityRequired);
                
                itemsToPickHtml += this.createItemToPickRow(detail, locations);
            }
        }

        // Render completed items (read-only)
        let completedHtml = '';
        if (completedItems.length === 0) {
            completedHtml = '<tr><td colspan="4" class="text-center py-4 text-muted">No completed items yet</td></tr>';
        } else {
            completedItems.forEach(detail => {
                completedHtml += `
                    <tr class="table-success">
                        <td>
                            <strong>${detail.itemCode}</strong><br>
                            <small class="text-muted">${detail.itemName}</small><br>
                            <small class="text-muted">${detail.itemUnit}</small>
                        </td>
                        <td class="text-center">
                            <span class="badge bg-info">${detail.quantityRequired}</span>
                        </td>
                        <td class="text-center">
                            <span class="badge bg-success">${detail.quantityPicked}</span>
                        </td>
                        <td class="text-center">
                            ${this.getStatusBadge(detail.status)}
                        </td>
                    </tr>
                `;
            });
        }

        itemsToPickBody.innerHTML = itemsToPickHtml;
        completedItemsBody.innerHTML = completedHtml;

        // Bind location change handlers
        this.bindLocationChangeHandlers();
    }

    createItemToPickRow(detail, locations) {
        const currentLocationId = detail.locationId || '';
        const remainingQty = detail.remainingQuantity || detail.quantityRequired;
        
        let locationOptions = '<option value="">-- Select Location --</option>';
        locations.forEach(loc => {
            const selected = loc.locationId === currentLocationId ? 'selected' : '';
            locationOptions += `<option value="${loc.locationId}" data-stock="${loc.availableStock}" ${selected}>${loc.locationCode} - ${loc.locationName} (Stock: ${loc.availableStock})</option>`;
        });

        return `
            <tr>
                <td>
                    <strong>${detail.itemCode}</strong><br>
                    <small class="text-muted">${detail.itemName}</small><br>
                    <small class="text-muted">${detail.itemUnit}</small>
                </td>
                <td class="text-center">
                    <select class="form-select form-select-sm source-location-select" 
                            data-picking-detail-id="${detail.id}"
                            data-item-id="${detail.itemId}"
                            style="min-width: 200px;">
                        ${locationOptions}
                    </select>
                    <small class="text-danger location-error d-none" data-item="${detail.id}"></small>
                </td>
                <td class="text-center">
                    <span class="badge bg-info">${detail.quantityRequired}</span>
                </td>
                <td class="text-center">
                    <span class="badge bg-success">${detail.quantityPicked || 0}</span>
                </td>
                <td class="text-center">
                    <span class="badge bg-warning">${remainingQty}</span>
                </td>
                <td class="text-center">
                    <input type="number" 
                           class="form-control form-control-sm text-center quantity-to-pick" 
                           data-picking-detail-id="${detail.id}"
                           data-remaining="${remainingQty}"
                           data-max-stock="0"
                           min="1" 
                           max="${remainingQty}" 
                           value="${remainingQty}"
                           style="width: 100px;" />
                </td>
                <td class="text-center">
                    ${this.getStatusBadge(detail.status)}
                </td>
            </tr>
        `;
    }

    async loadAvailableLocations(itemId, quantityRequired) {
        try {
            const response = await fetch(`/api/picking/locations/${itemId}?quantityRequired=${quantityRequired || 0}`);
            const result = await response.json();
            
            if (result.success) {
                return result.data || [];
            }
            return [];
        } catch (error) {
            console.error('Error loading available locations:', error);
            return [];
        }
    }

    bindLocationChangeHandlers() {
        const locationSelects = document.querySelectorAll('.source-location-select');
        locationSelects.forEach(select => {
            select.addEventListener('change', (e) => {
                const selectedOption = e.target.options[e.target.selectedIndex];
                const availableStock = selectedOption ? parseInt(selectedOption.dataset.stock) || 0 : 0;
                const pickingDetailId = e.target.dataset.pickingDetailId;
                
                // Update max quantity based on available stock
                const quantityInput = document.querySelector(`.quantity-to-pick[data-picking-detail-id="${pickingDetailId}"]`);
                const remainingQty = quantityInput ? parseInt(quantityInput.dataset.remaining) || 0 : 0;
                
                if (quantityInput) {
                    const maxQty = Math.min(remainingQty, availableStock);
                    quantityInput.setAttribute('max', maxQty);
                    quantityInput.dataset.maxStock = availableStock;
                    
                    // Adjust value if it exceeds max
                    if (parseInt(quantityInput.value) > maxQty) {
                        quantityInput.value = maxQty;
                    }
                }

                // Show/hide error message
                const errorElement = document.querySelector(`.location-error[data-item="${pickingDetailId}"]`);
                if (errorElement) {
                    if (!e.target.value) {
                        errorElement.classList.add('d-none');
                    } else if (availableStock < remainingQty) {
                        errorElement.textContent = `Insufficient stock! Available: ${availableStock}`;
                        errorElement.classList.remove('d-none');
                    } else {
                        errorElement.classList.add('d-none');
                    }
                }
            });
        });

        // Quantity input validation
        const quantityInputs = document.querySelectorAll('.quantity-to-pick');
        quantityInputs.forEach(input => {
            input.addEventListener('input', (e) => {
                const pickingDetailId = e.target.dataset.pickingDetailId;
                const maxStock = parseInt(e.target.dataset.maxStock) || 999999;
                const remaining = parseInt(e.target.dataset.remaining) || 0;
                const value = parseInt(e.target.value) || 0;
                
                const maxQty = Math.min(remaining, maxStock);
                if (value > maxQty) {
                    e.target.value = maxQty;
                }
            });
        });
    }

    async submitProcessPicking() {
        if (!this.currentPickingId) {
            this.showError('No picking selected. Please select a picking to process.');
            return;
        }

        const quantityInputs = document.querySelectorAll('.quantity-to-pick');
        const details = [];
        let hasErrors = false;

        quantityInputs.forEach(input => {
            const quantity = parseInt(input.value) || 0;
            const pickingDetailId = parseInt(input.getAttribute('data-picking-detail-id'));
            
            if (quantity > 0) {
                // Get selected location
                const locationSelect = document.querySelector(`.source-location-select[data-picking-detail-id="${pickingDetailId}"]`);
                const locationId = locationSelect ? parseInt(locationSelect.value) : null;
                
                if (!locationId) {
                    hasErrors = true;
                    const errorElement = document.querySelector(`.location-error[data-item="${pickingDetailId}"]`);
                    if (errorElement) {
                        errorElement.textContent = 'Please select a source location';
                        errorElement.classList.remove('d-none');
                    }
                    locationSelect?.classList.add('is-invalid');
                } else {
                    locationSelect?.classList.remove('is-invalid');
                    const errorElement = document.querySelector(`.location-error[data-item="${pickingDetailId}"]`);
                    if (errorElement) {
                        errorElement.classList.add('d-none');
                    }
                }

                details.push({
                    pickingDetailId: pickingDetailId,
                    quantityToPick: quantity,
                    locationId: locationId
                });
            }
        });

        if (hasErrors) {
            this.showError('Please select source location for all items');
            return;
        }

        if (details.length === 0) {
            this.showError('Please enter quantity to pick for at least one item');
            return;
        }

        const saveBtn = document.getElementById('saveProcessPickingBtn');
        if (!saveBtn) {
            console.error('Save button not found');
            this.showError('Save button not found. Please refresh the page.');
            return;
        }

        saveBtn.disabled = true;
        
        // Null check untuk spinner
        const spinner = saveBtn.querySelector('.spinner-border');
        const btnText = saveBtn.querySelector('.btn-text') || saveBtn.querySelector('span');
        
        if (spinner) {
            spinner.classList.remove('d-none');
        } else if (btnText) {
            // Fallback: ubah text button
            const originalText = btnText.textContent;
            btnText.textContent = 'Processing...';
        }

        try {
            const response = await fetch(`/api/picking/${this.currentPickingId}/process`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ details })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message || 'Picking processed successfully');
                setTimeout(() => {
                    const modalElement = document.getElementById('processPickingModal');
                    if (modalElement) {
                        const modal = bootstrap.Modal.getInstance(modalElement);
                        if (modal) {
                            modal.hide();
                        }
                    }
                    this.loadPickings();
                }, 1000);
            } else {
                this.showError(result.message || 'Failed to process Picking');
            }
        } catch (error) {
            console.error('Error processing Picking:', error);
            this.showError('Error processing Picking: ' + error.message);
        } finally {
            if (saveBtn) {
                saveBtn.disabled = false;
                
                // Null check untuk spinner
                const spinner = saveBtn.querySelector('.spinner-border');
                const btnText = saveBtn.querySelector('.btn-text') || saveBtn.querySelector('span');
                
                if (spinner) {
                    spinner.classList.add('d-none');
                } else if (btnText && btnText.textContent === 'Processing...') {
                    // Fallback: restore text button
                    btnText.textContent = 'Process Picking';
                }
            }
        }
    }

    showError(message) {
        const errorDiv = document.getElementById('processPickingErrorMessage');
        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.classList.remove('d-none');
            setTimeout(() => errorDiv.classList.add('d-none'), 5000);
        } else {
            alert(message);
        }
    }

    showSuccess(message) {
        const successDiv = document.getElementById('processPickingSuccessMessage');
        if (successDiv) {
            successDiv.textContent = message;
            successDiv.classList.remove('d-none');
            setTimeout(() => successDiv.classList.add('d-none'), 3000);
        }
    }
}

// Initialize when DOM is ready
let pickingManager;
document.addEventListener('DOMContentLoaded', () => {
    pickingManager = new PickingManager();
    window.pickingManager = pickingManager;
});

