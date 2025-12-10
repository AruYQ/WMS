/**
 * Location Manager - AJAX-based Location Management
 * Unified JavaScript module for all Location operations
 */

class LocationManager {
    constructor() {
        this.currentLocationId = null;
        this.locations = [];
        this.statistics = {};
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalCount = 0;
        this.totalPages = 0;
        this.filters = {
            status: '',
            capacity: '',
            category: '',
            search: ''
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
        this.loadLocations();
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
        // Filter events - HANYA element yang ada di HTML
        document.getElementById('statusFilter')?.addEventListener('change', (e) => {
            this.filters.status = e.target.value;
            this.currentPage = 1; // Reset to first page
            this.loadLocations();
        });

        document.getElementById('capacityFilter')?.addEventListener('change', (e) => {
            this.filters.capacity = e.target.value;
            this.currentPage = 1; // Reset to first page
            this.loadLocations();
        });

        document.getElementById('categoryFilter')?.addEventListener('change', (e) => {
            this.filters.category = e.target.value;
            this.currentPage = 1; // Reset to first page
            this.loadLocations();
        });

        document.getElementById('searchInput')?.addEventListener('input', 
            this.debounce((e) => {
                this.filters.search = e.target.value;
                this.currentPage = 1; // Reset to first page
                this.loadLocations();
            }, 300)
        );

        // Code validation - HANYA element yang ada di HTML
        document.getElementById('code')?.addEventListener('blur', (e) => {
            this.validateCode(e.target.value);
        });

        document.getElementById('code')?.addEventListener('input', (e) => {
            this.formatCode(e.target);
        });

        // Modal events - HANYA element yang ada di HTML
        document.getElementById('locationModal')?.addEventListener('hidden.bs.modal', () => {
            this.resetForm();
        });

        // Form submission prevention - HANYA element yang ada di HTML
        document.getElementById('locationForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveLocation();
        });

        // HAPUS semua binding untuk element yang tidak ada:
        // - pageSizeSelect (tidak ada di HTML)
        // - refreshBtn (tidak ada di HTML)
        // - paginationNav (tidak ada di HTML)
        // - locationsTableBody (tidak ada di HTML)
    }

    async loadDashboard() {
        try {
            const response = await fetch('/api/location/dashboard');
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.statistics = data.data;
                this.updateStatisticsCards();
                this.clearError(); // Clear any previous errors
            } else {
                // Handle API response with success = false
                const errorMessage = data.message || 'Failed to load dashboard statistics';
                console.error('Dashboard API Error:', errorMessage, data);
                this.showError(errorMessage);
            }
        } catch (error) {
            console.error('Error loading dashboard:', error);
            this.showError('Failed to load dashboard statistics');
        }
    }

    async loadLocations() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                ...this.filters
            });

            const response = await fetch(`/api/location?${params}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.locations = data.data.items;
                this.totalCount = data.data.totalCount;
                this.totalPages = data.data.totalPages;
                
                this.renderLocationsTable();
                this.updatePagination(this.totalCount, this.totalPages);
                this.clearError(); // Clear any previous errors
            } else {
                // Handle API response with success = false
                const errorMessage = data.message || 'Failed to load locations';
                console.error('Locations API Error:', errorMessage, data);
                this.showError(errorMessage);
            }
        } catch (error) {
            console.error('Error loading locations:', error);
            this.showError('Failed to load locations');
        }
    }

    updateStatisticsCards() {
        document.getElementById('totalLocationsCount').textContent = this.statistics.totalLocations || 0;
        document.getElementById('activeLocationsCount').textContent = this.statistics.activeLocations || 0;
        document.getElementById('nearFullLocationsCount').textContent = this.statistics.nearFullLocations || 0;
        document.getElementById('fullLocationsCount').textContent = this.statistics.fullLocations || 0;
    }

    updatePagination(totalCount, totalPages) {
        // Update pagination info
        const startRecord = totalCount > 0 ? ((this.currentPage - 1) * this.pageSize) + 1 : 0;
        const endRecord = Math.min(this.currentPage * this.pageSize, totalCount);
        
        // Update pagination info elements
        const showingStartEl = document.getElementById('showingStart');
        const showingEndEl = document.getElementById('showingEnd');
        const totalRecordsEl = document.getElementById('totalRecords');
        const currentPageNumEl = document.getElementById('currentPageNum');
        const totalPagesNumEl = document.getElementById('totalPagesNum');
        const prevPageBtnEl = document.getElementById('prevPageBtn');
        const nextPageBtnEl = document.getElementById('nextPageBtn');
        
        if (showingStartEl) showingStartEl.textContent = startRecord;
        if (showingEndEl) showingEndEl.textContent = endRecord;
        if (totalRecordsEl) totalRecordsEl.textContent = totalCount;
        if (currentPageNumEl) currentPageNumEl.textContent = this.currentPage;
        if (totalPagesNumEl) totalPagesNumEl.textContent = totalPages;
        
        // Update button states
        if (prevPageBtnEl) {
            prevPageBtnEl.disabled = this.currentPage === 1;
        }
        if (nextPageBtnEl) {
            nextPageBtnEl.disabled = this.currentPage === totalPages || totalPages === 0;
        }
    }

    goToPage(page) {
        if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
            this.currentPage = page;
            this.loadLocations();
        }
    }

    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadLocations();
        }
    }

    nextPage() {
        if (this.currentPage < this.totalPages) {
            this.currentPage++;
            this.loadLocations();
        }
    }

    changePageSize(newPageSize) {
        this.pageSize = parseInt(newPageSize);
        this.currentPage = 1; // Reset to first page
        this.loadLocations();
    }

    renderLocationsTable() {
        const container = document.getElementById('locationsTableContainer');
        if (!container) return;

        if (this.locations.length === 0) {
            container.innerHTML = '<div class="text-center py-5"><p>No locations found</p></div>';
            return;
        }

        let html = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>CODE</th>
                            <th>NAME</th>
                            <th>CATEGORY</th>
                            <th>CURRENT STOCK</th>
                            <th>STATUS</th>
                            <th>UTILIZATION</th>
                            <th>MODIFIED</th>
                            <th>ACTIONS</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        this.locations.forEach(location => {
            html += `
                <tr>
                    <td>
                        <div class="fw-medium text-primary">${location.code}</div>
                        ${location.description ? `<div class="small text-muted">${location.description}</div>` : ''}
                    </td>
                    <td>
                        <div class="fw-medium">${location.name}</div>
                        <div class="small text-muted">
                            <i class="fas fa-warehouse me-1"></i>
                            Max: ${location.maxCapacity} units
                        </div>
                    </td>
                    <td>
                        <span class="badge ${location.category === 'Storage' ? 'bg-primary' : 'bg-secondary'}">${location.category || 'N/A'}</span>
                    </td>
                    <td>
                        <div class="fw-medium">${location.currentCapacity} / ${location.maxCapacity}</div>
                        <div class="small text-muted">
                            <i class="fas fa-boxes me-1"></i>
                            Available: ${location.availableCapacity}
                        </div>
                    </td>
                    <td>
                        <span class="badge ${this.getStatusBadgeClass(location)}">${location.capacityStatus}</span>
                        ${!location.isActive ? '<div class="small text-muted"><i class="fas fa-pause-circle me-1"></i>Inactive</div>' : ''}
                    </td>
                    <td>
                        <div class="d-flex align-items-center">
                            <div class="progress flex-grow-1 me-2" style="height: 8px;">
                                <div class="progress-bar ${this.getCapacityBarClass(location)}" 
                                     style="width: ${location.capacityPercentage}%"></div>
                            </div>
                            <small class="text-muted">${location.capacityPercentage.toFixed(0)}%</small>
                        </div>
                    </td>
                    <td>
                        <span class="fw-medium">${this.formatDate(location.modifiedDate)}</span>
                        <div class="small text-muted">${this.formatTime(location.modifiedDate)}</div>
                    </td>
                    <td>
                        <div class="btn-group" role="group">
                            <button type="button" class="btn btn-sm btn-info" 
                                    onclick="locationManager.viewLocation(${location.id})" title="View Details">
                                <i class="fas fa-eye"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-primary" 
                                    onclick="locationManager.editLocation(${location.id})" title="Edit">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-warning" 
                                    onclick="locationManager.toggleStatus(${location.id})" 
                                    title="${location.isActive ? 'Deactivate' : 'Activate'}">
                                <i class="fas ${location.isActive ? 'fa-pause' : 'fa-play'}"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-danger" 
                                    onclick="locationManager.deleteLocation(${location.id})" title="Delete">
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
    }

    async viewLocation(id) {
        try {
            const response = await fetch(`/api/location/${id}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.renderLocationDetails(data.data);
                const modal = new bootstrap.Modal(document.getElementById('viewLocationModal'));
                modal.show();
            } else {
                this.showError(data.message || 'Failed to load location details');
            }
        } catch (error) {
            console.error('Error loading location details:', error);
            this.showError('Failed to load location details');
        }
    }

    async editLocation(id) {
        try {
            const response = await fetch(`/api/location/${id}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                console.log('Edit location data loaded:', data.data); // Debug log
                this.populateForm(data.data);
                
                // Update modal title and show current status section
                const modalTitleEl = document.getElementById('locationModalTitle');
                const currentStatusEl = document.getElementById('currentStatus');
                
                if (modalTitleEl) modalTitleEl.textContent = 'Edit Location';
                if (currentStatusEl) currentStatusEl.classList.remove('d-none');
                
                this.currentLocationId = id;
                
                const modal = new bootstrap.Modal(document.getElementById('locationModal'));
                modal.show();
            } else {
                console.error('API Error:', data); // Debug log
                this.showError(data.message || 'Failed to load location data');
            }
        } catch (error) {
            console.error('Error loading location for edit:', error);
            this.showError('Failed to load location data');
        }
    }

    async saveLocation() {
        const form = document.getElementById('locationForm');
        const formData = new FormData(form);
        
        const locationData = {
            id: formData.get('Id') || null,
            code: formData.get('Code'),
            name: formData.get('Name'),
            description: formData.get('Description'),
            category: formData.get('Category'),
            maxCapacity: parseInt(formData.get('MaxCapacity')),
            isActive: formData.get('IsActive') === 'on'
        };

        try {
            const saveBtn = document.getElementById('saveLocationBtn');
            const spinner = saveBtn?.querySelector('.spinner-border');
            const icon = saveBtn?.querySelector('i');
            const btnText = saveBtn?.querySelector('.btn-text');
            
            if (saveBtn) {
                saveBtn.disabled = true;
                if (spinner) spinner.classList.remove('d-none');
                if (icon) icon.classList.add('d-none');
                if (btnText) btnText.textContent = 'Saving...';
            }

            const url = locationData.id ? `/api/location/${locationData.id}` : '/api/location';
            const method = locationData.id ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(locationData)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                const modal = bootstrap.Modal.getInstance(document.getElementById('locationModal'));
                modal.hide();
                this.loadDashboard();
                this.loadLocations();
            } else {
                this.showFormErrors(result.errors);
            }
        } catch (error) {
            console.error('Error saving location:', error);
            this.showError('Failed to save location');
        } finally {
            const saveBtn = document.getElementById('saveLocationBtn');
            const spinner = saveBtn?.querySelector('.spinner-border');
            const icon = saveBtn?.querySelector('i');
            const btnText = saveBtn?.querySelector('.btn-text');
            
            if (saveBtn) {
                saveBtn.disabled = false;
                if (spinner) spinner.classList.add('d-none');
                if (icon) icon.classList.remove('d-none');
                if (btnText) btnText.textContent = 'Save Location';
            }
        }
    }

    async deleteLocation(id) {
        try {
            const response = await fetch(`/api/location/${id}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.renderDeleteConfirmation(data.data);
                this.currentLocationId = id;
                
                const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
                modal.show();
            } else {
                this.showError(data.message || 'Failed to load location data');
            }
        } catch (error) {
            console.error('Error loading location for delete:', error);
            this.showError('Failed to load location data');
        }
    }

    async confirmDeleteLocation() {
        try {
            const deleteBtn = document.getElementById('confirmDeleteBtn');
            
            // Check if button is disabled (has inventory)
            if (deleteBtn.disabled) {
                this.showError('Cannot delete location that contains inventory. Please move all inventory first.');
                return;
            }
            
            const spinner = deleteBtn.querySelector('.spinner-border');
            const icon = deleteBtn.querySelector('i');
            
            deleteBtn.disabled = true;
            if (spinner) spinner.classList.remove('d-none');
            if (icon) icon.classList.add('d-none');

            const response = await fetch(`/api/location/${this.currentLocationId}`, {
                method: 'DELETE',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                const modal = bootstrap.Modal.getInstance(document.getElementById('deleteModal'));
                modal.hide();
                this.loadDashboard();
                this.loadLocations();
            } else {
                // Show error message with more details for delete failures
                this.showError(result.message || 'Failed to delete location');
                
                // If delete failed due to inventory, show additional info
                if (result.message && result.message.includes('inventory')) {
                    console.warn('Delete failed due to existing inventory:', result.message);
                }
            }
        } catch (error) {
            console.error('Error deleting location:', error);
            this.showError('Failed to delete location');
        } finally {
            const deleteBtn = document.getElementById('confirmDeleteBtn');
            const spinner = deleteBtn.querySelector('.spinner-border');
            const icon = deleteBtn.querySelector('i');
            
            // Only re-enable if not disabled due to inventory
            if (!deleteBtn.disabled || !deleteBtn.innerHTML.includes('Cannot Delete')) {
                deleteBtn.disabled = false;
                deleteBtn.setAttribute('onclick', 'confirmDeleteLocation()'); // Ensure onclick is restored
            }
            if (spinner) spinner.classList.add('d-none');
            if (icon) icon.classList.remove('d-none');
        }
    }

    async toggleStatus(id) {
        try {
            const response = await fetch(`/api/location/${id}/toggle-status`, {
                method: 'PATCH',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                this.loadDashboard();
                this.loadLocations();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error toggling status:', error);
            this.showError('Failed to update location status');
        }
    }

    async refreshCapacity(id) {
        try {
            const response = await fetch(`/api/location/${id}/update-capacity`, {
                method: 'PATCH',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                this.loadDashboard();
                this.loadLocations();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error refreshing capacity:', error);
            this.showError('Failed to refresh capacity');
        }
    }

    async refreshCapacities() {
        try {
            const response = await fetch('/api/location/refresh-all-capacities', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                this.loadDashboard();
                this.loadLocations();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error refreshing all capacities:', error);
            this.showError('Failed to refresh capacities');
        }
    }

    async validateCode(code) {
        if (!code) return;

        try {
            const excludeId = this.currentLocationId || '';
            const response = await fetch(`/api/location/check-code?code=${encodeURIComponent(code)}&excludeId=${excludeId}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            const feedback = document.getElementById('codeValidation');
            feedback.innerHTML = data.isUnique 
                ? `<i class="fas fa-check text-success"></i> ${data.message}`
                : `<i class="fas fa-times text-danger"></i> ${data.message}`;
        } catch (error) {
            console.error('Error validating code:', error);
        }
    }

    formatCode(input) {
        let value = input.value.toUpperCase();
        value = value.replace(/[^A-Z0-9-]/g, '');
        input.value = value;
    }

    populateForm(location) {
        try {
            console.log('Populating form with location data:', location); // Debug log
            
            // Fill basic form fields - PERBAIKI ID sesuai HTML
            const locationIdEl = document.getElementById('locationId');
            const codeEl = document.getElementById('locationCode');
            const nameEl = document.getElementById('locationName');
            const descriptionEl = document.getElementById('locationDescription');
            const categoryEl = document.getElementById('locationCategory');
            const maxCapacityEl = document.getElementById('maxCapacity');
            const isActiveEl = document.getElementById('isActive');
            
            if (locationIdEl) locationIdEl.value = location.id || '';
            if (codeEl) codeEl.value = location.code || '';
            if (nameEl) nameEl.value = location.name || '';
            if (descriptionEl) descriptionEl.value = location.description || '';
            if (categoryEl) categoryEl.value = location.category || 'Storage';
            if (maxCapacityEl) maxCapacityEl.value = location.maxCapacity || '';
            if (isActiveEl) isActiveEl.checked = location.isActive !== undefined ? location.isActive : true;
            
            // Update current status display (these elements are in hidden div)
            const currentCapacityEl = document.getElementById('currentCapacity');
            const currentMaxCapacityEl = document.getElementById('currentMaxCapacity');
            const currentUtilizationEl = document.getElementById('currentUtilization');
            
            if (currentCapacityEl) currentCapacityEl.textContent = location.currentCapacity || 0;
            if (currentMaxCapacityEl) currentMaxCapacityEl.textContent = location.maxCapacity || 0;
            if (currentUtilizationEl) currentUtilizationEl.textContent = `${(location.capacityPercentage || 0).toFixed(1)}%`;
            
            console.log('Form populated successfully'); // Debug log
        } catch (error) {
            console.error('Error populating form:', error);
            this.showError('Error loading location data for editing');
        }
    }

    showCreateModal() {
        this.resetForm();
        document.getElementById('locationModalTitle').textContent = 'Add New Location';
        const modal = new bootstrap.Modal(document.getElementById('locationModal'));
        modal.show();
    }

    resetForm() {
        const locationFormEl = document.getElementById('locationForm');
        const locationIdEl = document.getElementById('locationId');
        const codeValidationEl = document.getElementById('codeValidation');
        const formErrorsEl = document.getElementById('formErrors');
        const currentStatusEl = document.getElementById('currentStatus');
        const modalTitleEl = document.getElementById('locationModalTitle');
        
        if (locationFormEl) locationFormEl.reset();
        if (locationIdEl) locationIdEl.value = '';
        if (codeValidationEl) codeValidationEl.innerHTML = '';
        if (formErrorsEl) formErrorsEl.classList.add('d-none');
        if (currentStatusEl) currentStatusEl.classList.add('d-none');
        if (modalTitleEl) modalTitleEl.textContent = 'Add New Location';
        
        // Clear error and success messages
        this.clearError();
        this.clearSuccess();
        
        this.currentLocationId = null;
    }

    renderLocationDetails(location) {
        const container = document.getElementById('locationDetailsContainer');
        if (!container) return;

        const capacity = location.capacity || {};
        const audit = location.audit || {};
        const inventoryItems = Array.isArray(location.inventoryItems) ? location.inventoryItems : [];
        const capacityPercentage = capacity.percentage || 0;
        const statusText = capacity.status || (location.isActive ? 'IN USE' : 'INACTIVE');

        const inventoryTable = inventoryItems.length > 0
            ? `
                <div class="table-responsive">
                    <table class="table table-sm table-striped">
                        <thead class="table-light">
                            <tr>
                                <th>Item</th>
                                <th>Unit</th>
                                <th class="text-end">Quantity</th>
                                <th>Last Updated</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${inventoryItems.map(item => `
                                <tr>
                                    <td>
                                        <strong>${item.itemCode}</strong><br>
                                        <small class="text-muted">${item.itemName}</small>
                                    </td>
                                    <td>${item.unit}</td>
                                    <td class="text-end">${this.formatNumber(item.quantity || 0)}</td>
                                    <td>${this.formatDateTime(item.lastUpdated)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>`
            : '<div class="alert alert-light mb-0">No inventory stored in this location.</div>';

        container.innerHTML = `
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-info-circle me-2"></i>Location Information</h6>
                            <dl class="row mb-0">
                                <dt class="col-sm-4">Code</dt>
                                <dd class="col-sm-8"><span class="badge bg-primary">${location.code}</span></dd>
                                <dt class="col-sm-4">Name</dt>
                                <dd class="col-sm-8">${location.name}</dd>
                                <dt class="col-sm-4">Description</dt>
                                <dd class="col-sm-8">${location.description || 'No description provided'}</dd>
                                <dt class="col-sm-4">Category</dt>
                                <dd class="col-sm-8">${location.category || '-'}</dd>
                                <dt class="col-sm-4">Status</dt>
                                <dd class="col-sm-8">
                                    <span class="badge ${this.getStatusBadgeClass(location)}">${statusText}</span>
                                    ${location.isActive ? '' : '<span class="badge bg-warning ms-2">Inactive</span>'}
                                </dd>
                                <dt class="col-sm-4">Created</dt>
                                <dd class="col-sm-8">${this.formatDateTime(audit.createdDate)}</dd>
                                ${audit.modifiedDate ? `
                                    <dt class="col-sm-4">Modified</dt>
                                    <dd class="col-sm-8">${this.formatDateTime(audit.modifiedDate)}</dd>
                                ` : ''}
                            </dl>
                        </div>
                    </div>
                </div>
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-chart-bar me-2"></i>Capacity Overview</h6>
                            <div class="mb-4">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <span class="fw-bold">Utilization</span>
                                    <span class="fw-bold">${capacityPercentage.toFixed(1)}%</span>
                                </div>
                                <div class="progress" style="height: 24px;">
                                    <div class="progress-bar ${this.getCapacityBarClass(location)}"
                                         style="width: ${capacityPercentage}%">
                                        ${this.formatNumber(capacity.current || 0)} / ${this.formatNumber(capacity.max || 0)}
                                    </div>
                                </div>
                            </div>
                            <div class="row text-center">
                                <div class="col-4">
                                    <div class="border rounded p-3">
                                        <h5 class="text-primary mb-1">${this.formatNumber(capacity.current || 0)}</h5>
                                        <small class="text-muted">Used</small>
                                    </div>
                                </div>
                                <div class="col-4">
                                    <div class="border rounded p-3">
                                        <h5 class="text-success mb-1">${this.formatNumber(capacity.available || 0)}</h5>
                                        <small class="text-muted">Available</small>
                                    </div>
                                </div>
                                <div class="col-4">
                                    <div class="border rounded p-3">
                                        <h5 class="text-info mb-1">${this.formatNumber(capacity.max || 0)}</h5>
                                        <small class="text-muted">Max</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4 mt-1">
                <div class="col-12">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-boxes me-2"></i>Inventory Items</h6>
                            ${inventoryTable}
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4 mt-1">
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-user me-2"></i>Audit Trail</h6>
                            <p class="mb-1"><strong>Created By:</strong> ${audit.createdBy || 'System'}</p>
                            <p class="mb-0"><strong>Modified By:</strong> ${audit.modifiedBy || 'N/A'}</p>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderDeleteConfirmation(location) {
        const infoDiv = document.getElementById('deleteLocationInfo');
        if (!infoDiv) return;
        
        const hasInventory = location.currentCapacity > 0;
        
        infoDiv.innerHTML = `
            <div class="card">
                <div class="card-body">
                    <h6 class="card-title">${location.code} - ${location.name}</h6>
                    <p class="card-text small text-muted">
                        Capacity: ${location.currentCapacity}/${location.maxCapacity} units
                    </p>
                    ${hasInventory ? `
                        <div class="alert alert-warning">
                            <i class="fas fa-exclamation-triangle me-2"></i>
                            <strong>Cannot Delete:</strong> This location contains ${location.currentCapacity} units of inventory.
                            <br><small>Please move all inventory to another location first before deleting.</small>
                        </div>
                    ` : `
                        <div class="alert alert-info">
                            <i class="fas fa-info-circle me-2"></i>
                            This location is empty and can be safely deleted.
                        </div>
                    `}
                </div>
            </div>
        `;
        
        // Disable delete button if has inventory
        const deleteBtn = document.getElementById('confirmDeleteBtn');
        if (deleteBtn) {
            if (hasInventory) {
                deleteBtn.disabled = true;
                deleteBtn.innerHTML = '<i class="fas fa-ban me-1"></i> Cannot Delete (Has Inventory)';
                deleteBtn.removeAttribute('onclick'); // Remove onclick since disabled
            } else {
                deleteBtn.disabled = false;
                deleteBtn.innerHTML = '<span class="spinner-border spinner-border-sm d-none" role="status"></span><i class="fas fa-trash me-1"></i> Delete Location';
                deleteBtn.setAttribute('onclick', 'confirmDeleteLocation()'); // Restore onclick handler
            }
        }
    }

    showFormErrors(errors) {
        const errorsDiv = document.getElementById('formErrors');
        errorsDiv.innerHTML = '<ul class="mb-0">' + 
            Object.values(errors).map(error => `<li>${error}</li>`).join('') + 
            '</ul>';
        errorsDiv.classList.remove('d-none');
    }

    showSuccess(message) {
        const successDiv = document.getElementById('successMessage');
        if (successDiv) {
            successDiv.textContent = message;
            successDiv.classList.remove('d-none');
        }
        console.log('Success:', message);
    }

    showError(message) {
        const errorDiv = document.getElementById('errorMessage');
        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.classList.remove('d-none');
        }
        console.error('Error:', message);
    }

    clearError() {
        const errorDiv = document.getElementById('errorMessage');
        if (errorDiv) {
            errorDiv.classList.add('d-none');
        }
    }

    clearSuccess() {
        const successDiv = document.getElementById('successMessage');
        if (successDiv) {
            successDiv.classList.add('d-none');
        }
    }

    getStatusBadgeClass(location) {
        const isActive = typeof location.isActive === 'boolean' ? location.isActive : true;
        const isFull = location.isFull ?? location.capacity?.isFull ?? false;
        const percentage = location.capacityPercentage ?? location.capacity?.percentage ?? 0;

        if (!isActive) return 'bg-secondary';
        if (isFull) return 'bg-danger';
        if (percentage >= 80) return 'bg-warning';
        return 'bg-success';
    }

    getCapacityBarClass(location) {
        const isFull = location.isFull ?? location.capacity?.isFull ?? false;
        const percentage = location.capacityPercentage ?? location.capacity?.percentage ?? 0;

        if (isFull) return 'bg-danger';
        if (percentage >= 80) return 'bg-warning';
        return 'bg-success';
    }

    formatNumber(value) {
        return new Intl.NumberFormat('id-ID').format(value ?? 0);
    }

    formatDate(dateString) {
        return new Date(dateString).toLocaleDateString('en-US', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
        });
    }

    formatTime(dateString) {
        return new Date(dateString).toLocaleTimeString('en-US', {
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    formatDateTime(dateString) {
        return new Date(dateString).toLocaleString('en-US', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }


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

    setupRealTimeUpdates() {
        // Auto-refresh every 30 seconds
        setInterval(() => {
            this.loadDashboard();
            this.loadLocations();
        }, 30000);
    }
}

// Global functions for onclick handlers
function openCreateModal() {
    locationManager.resetForm();
    const modal = new bootstrap.Modal(document.getElementById('locationModal'));
    modal.show();
}

function saveLocation() {
    locationManager.saveLocation();
}

function editLocationFromDetails() {
    bootstrap.Modal.getInstance(document.getElementById('viewLocationModal')).hide();
    locationManager.editLocation(locationManager.currentLocationId);
}

function refreshCapacities() {
    if (confirm('Refresh capacities for all locations? This may take a moment.')) {
        locationManager.refreshCapacities();
    }
}

function exportLocations() {
    window.open('/api/location/export', '_blank');
}



function confirmDeleteLocation() {
    locationManager.confirmDeleteLocation();
}

// Initialize when DOM is loaded
let locationManager;
document.addEventListener('DOMContentLoaded', function() {
    locationManager = new LocationManager();
});