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
        // Filter events
        document.getElementById('statusFilter').addEventListener('change', (e) => {
            this.filters.status = e.target.value;
            this.currentPage = 1; // Reset to first page
            this.loadLocations();
        });

        document.getElementById('capacityFilter').addEventListener('change', (e) => {
            this.filters.capacity = e.target.value;
            this.currentPage = 1; // Reset to first page
            this.loadLocations();
        });

        document.getElementById('searchInput').addEventListener('input', 
            this.debounce((e) => {
                this.filters.search = e.target.value;
                this.currentPage = 1; // Reset to first page
                this.loadLocations();
            }, 300)
        );

        // Page size selector
        document.getElementById('pageSizeSelect').addEventListener('change', (e) => {
            this.pageSize = parseInt(e.target.value);
            this.currentPage = 1; // Reset to first page
            this.loadLocations();
        });

        // Refresh button
        document.getElementById('refreshBtn').addEventListener('click', () => {
            this.loadDashboard();
            this.loadLocations();
        });

        // Code validation
        document.getElementById('code').addEventListener('blur', (e) => {
            this.validateCode(e.target.value);
        });

        document.getElementById('code').addEventListener('input', (e) => {
            this.formatCode(e.target);
        });

        // Modal events
        document.getElementById('locationModal').addEventListener('hidden.bs.modal', () => {
            this.resetForm();
        });
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
        document.getElementById('totalLocations').textContent = this.statistics.totalLocations || 0;
        document.getElementById('activeLocations').textContent = this.statistics.activeLocations || 0;
        document.getElementById('nearFullLocations').textContent = this.statistics.nearFullLocations || 0;
        document.getElementById('fullLocations').textContent = this.statistics.fullLocations || 0;
    }

    updatePagination(totalCount, totalPages) {
        // Update pagination info
        const startRecord = totalCount > 0 ? ((this.currentPage - 1) * this.pageSize) + 1 : 0;
        const endRecord = Math.min(this.currentPage * this.pageSize, totalCount);
        
        document.getElementById('showingStart').textContent = startRecord;
        document.getElementById('showingEnd').textContent = endRecord;
        document.getElementById('totalRecords').textContent = totalCount;
        
        // Generate pagination buttons
        const paginationNav = document.getElementById('paginationNav');
        paginationNav.innerHTML = '';
        
        if (totalPages <= 1) {
            // No pagination needed if only one page
            return;
        }
        
        // Previous button
        const prevLi = document.createElement('li');
        prevLi.className = `page-item ${this.currentPage === 1 ? 'disabled' : ''}`;
        prevLi.innerHTML = `
            <a class="page-link" href="#" onclick="locationManager.goToPage(${this.currentPage - 1}); return false;">
                <i class="fas fa-chevron-left"></i>
            </a>
        `;
        paginationNav.appendChild(prevLi);
        
        // Page numbers
        const maxVisiblePages = 5;
        let startPage = Math.max(1, this.currentPage - Math.floor(maxVisiblePages / 2));
        let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
        
        // Adjust start page if we're near the end
        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1);
        }
        
        // First page and ellipsis
        if (startPage > 1) {
            const firstLi = document.createElement('li');
            firstLi.className = 'page-item';
            firstLi.innerHTML = `
                <a class="page-link" href="#" onclick="locationManager.goToPage(1); return false;">1</a>
            `;
            paginationNav.appendChild(firstLi);
            
            if (startPage > 2) {
                const ellipsisLi = document.createElement('li');
                ellipsisLi.className = 'page-item disabled';
                ellipsisLi.innerHTML = '<span class="page-link">...</span>';
                paginationNav.appendChild(ellipsisLi);
            }
        }
        
        // Page numbers
        for (let i = startPage; i <= endPage; i++) {
            const pageLi = document.createElement('li');
            pageLi.className = `page-item ${i === this.currentPage ? 'active' : ''}`;
            pageLi.innerHTML = `
                <a class="page-link" href="#" onclick="locationManager.goToPage(${i}); return false;">${i}</a>
            `;
            paginationNav.appendChild(pageLi);
        }
        
        // Last page and ellipsis
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                const ellipsisLi = document.createElement('li');
                ellipsisLi.className = 'page-item disabled';
                ellipsisLi.innerHTML = '<span class="page-link">...</span>';
                paginationNav.appendChild(ellipsisLi);
            }
            
            const lastLi = document.createElement('li');
            lastLi.className = 'page-item';
            lastLi.innerHTML = `
                <a class="page-link" href="#" onclick="locationManager.goToPage(${totalPages}); return false;">${totalPages}</a>
            `;
            paginationNav.appendChild(lastLi);
        }
        
        // Next button
        const nextLi = document.createElement('li');
        nextLi.className = `page-item ${this.currentPage === totalPages ? 'disabled' : ''}`;
        nextLi.innerHTML = `
            <a class="page-link" href="#" onclick="locationManager.goToPage(${this.currentPage + 1}); return false;">
                <i class="fas fa-chevron-right"></i>
            </a>
        `;
        paginationNav.appendChild(nextLi);
    }

    goToPage(page) {
        if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
            this.currentPage = page;
            this.loadLocations();
        }
    }

    renderLocationsTable() {
        const tbody = document.getElementById('locationsTableBody');
        const noDataMessage = document.getElementById('noLocationsMessage');

        if (this.locations.length === 0) {
            tbody.innerHTML = '';
            noDataMessage.style.display = 'block';
            return;
        }

        noDataMessage.style.display = 'none';
        tbody.innerHTML = this.locations.map(location => `
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
                        <button type="button" class="btn btn-sm btn-outline-primary" 
                                onclick="locationManager.viewLocation(${location.id})" title="View Details">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-secondary" 
                                onclick="locationManager.editLocation(${location.id})" title="Edit">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button type="button" class="btn btn-sm ${location.isActive ? 'btn-outline-warning' : 'btn-outline-success'}" 
                                onclick="locationManager.toggleStatus(${location.id})" 
                                title="${location.isActive ? 'Deactivate' : 'Activate'}">
                            <i class="fas ${location.isActive ? 'fa-pause' : 'fa-play'}"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-info" 
                                onclick="locationManager.refreshCapacity(${location.id})" title="Refresh Capacity">
                            <i class="fas fa-sync-alt"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger" 
                                onclick="locationManager.deleteLocation(${location.id})" title="Delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
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
                const modal = new bootstrap.Modal(document.getElementById('detailsModal'));
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
                const modalTitleEl = document.getElementById('modalTitle');
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
            maxCapacity: parseInt(formData.get('MaxCapacity')),
            isActive: formData.get('IsActive') === 'on'
        };

        try {
            const saveBtn = document.getElementById('saveLocationBtn');
            const spinner = saveBtn.querySelector('.spinner-border');
            const icon = saveBtn.querySelector('i');
            
            saveBtn.disabled = true;
            spinner.classList.remove('d-none');
            icon.classList.add('d-none');

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
            const spinner = saveBtn.querySelector('.spinner-border');
            const icon = saveBtn.querySelector('i');
            
            saveBtn.disabled = false;
            spinner.classList.add('d-none');
            icon.classList.remove('d-none');
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
            
            // Fill basic form fields
            const locationIdEl = document.getElementById('locationId');
            const codeEl = document.getElementById('code');
            const nameEl = document.getElementById('name');
            const descriptionEl = document.getElementById('description');
            const maxCapacityEl = document.getElementById('maxCapacity');
            const isActiveEl = document.getElementById('isActive');
            
            if (locationIdEl) locationIdEl.value = location.id || '';
            if (codeEl) codeEl.value = location.code || '';
            if (nameEl) nameEl.value = location.name || '';
            if (descriptionEl) descriptionEl.value = location.description || '';
            if (maxCapacityEl) maxCapacityEl.value = location.maxCapacity || '';
            if (isActiveEl) isActiveEl.checked = location.isActive || false;
            
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

    resetForm() {
        const locationFormEl = document.getElementById('locationForm');
        const locationIdEl = document.getElementById('locationId');
        const codeValidationEl = document.getElementById('codeValidation');
        const formErrorsEl = document.getElementById('formErrors');
        const currentStatusEl = document.getElementById('currentStatus');
        const modalTitleEl = document.getElementById('modalTitle');
        
        if (locationFormEl) locationFormEl.reset();
        if (locationIdEl) locationIdEl.value = '';
        if (codeValidationEl) codeValidationEl.innerHTML = '';
        if (formErrorsEl) formErrorsEl.classList.add('d-none');
        if (currentStatusEl) currentStatusEl.classList.add('d-none');
        if (modalTitleEl) modalTitleEl.textContent = 'Create Location';
        
        this.currentLocationId = null;
    }

    renderLocationDetails(location) {
        const detailsDiv = document.getElementById('locationDetails');
        detailsDiv.innerHTML = `
            <div class="row">
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header">
                            <h6 class="mb-0"><i class="fas fa-info-circle me-2"></i>Location Information</h6>
                        </div>
                        <div class="card-body">
                            <dl class="row">
                                <dt class="col-sm-4">Code:</dt>
                                <dd class="col-sm-8"><span class="badge bg-primary">${location.code}</span></dd>
                                
                                <dt class="col-sm-4">Name:</dt>
                                <dd class="col-sm-8">${location.name}</dd>
                                
                                <dt class="col-sm-4">Description:</dt>
                                <dd class="col-sm-8">${location.description || 'No description'}</dd>
                                
                                <dt class="col-sm-4">Status:</dt>
                                <dd class="col-sm-8">
                                    <span class="badge ${this.getStatusBadgeClass(location)}">${location.capacityStatus}</span>
                                    ${!location.isActive ? '<span class="badge bg-warning ms-2">Inactive</span>' : ''}
                                </dd>
                                
                                <dt class="col-sm-4">Max Capacity:</dt>
                                <dd class="col-sm-8">${location.maxCapacity} units</dd>
                                
                                <dt class="col-sm-4">Current Usage:</dt>
                                <dd class="col-sm-8">${location.currentCapacity} units</dd>
                                
                                <dt class="col-sm-4">Available:</dt>
                                <dd class="col-sm-8">${location.availableCapacity} units</dd>
                                
                                <dt class="col-sm-4">Created:</dt>
                                <dd class="col-sm-8">${this.formatDateTime(location.createdDate)}</dd>
                                
                                ${location.modifiedDate ? `
                                <dt class="col-sm-4">Modified:</dt>
                                <dd class="col-sm-8">${this.formatDateTime(location.modifiedDate)}</dd>
                                ` : ''}
                            </dl>
                        </div>
                    </div>
                </div>
                
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header">
                            <h6 class="mb-0"><i class="fas fa-chart-bar me-2"></i>Capacity Overview</h6>
                        </div>
                        <div class="card-body">
                            <div class="mb-4">
                                <div class="d-flex justify-content-between mb-2">
                                    <span class="fw-bold">Capacity Utilization</span>
                                    <span class="fw-bold">${location.capacityPercentage.toFixed(1)}%</span>
                                </div>
                                <div class="progress" style="height: 25px;">
                                    <div class="progress-bar ${this.getCapacityBarClass(location)}" 
                                         style="width: ${location.capacityPercentage}%">
                                        ${location.currentCapacity} / ${location.maxCapacity}
                                    </div>
                                </div>
                            </div>

                            <div class="row text-center">
                                <div class="col-4">
                                    <div class="border rounded p-3">
                                        <h5 class="text-primary mb-1">${location.currentCapacity}</h5>
                                        <small class="text-muted">Used</small>
                                    </div>
                                </div>
                                <div class="col-4">
                                    <div class="border rounded p-3">
                                        <h5 class="text-success mb-1">${location.availableCapacity}</h5>
                                        <small class="text-muted">Available</small>
                                    </div>
                                </div>
                                <div class="col-4">
                                    <div class="border rounded p-3">
                                        <h5 class="text-info mb-1">${location.maxCapacity}</h5>
                                        <small class="text-muted">Max</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderDeleteConfirmation(location) {
        const infoDiv = document.getElementById('deleteLocationInfo');
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

    showFormErrors(errors) {
        const errorsDiv = document.getElementById('formErrors');
        errorsDiv.innerHTML = '<ul class="mb-0">' + 
            Object.values(errors).map(error => `<li>${error}</li>`).join('') + 
            '</ul>';
        errorsDiv.classList.remove('d-none');
    }

    showSuccess(message) {
        document.getElementById('successMessage').textContent = message;
        if (this.successToast) {
            this.successToast.show();
        }
    }

    showError(message) {
        document.getElementById('errorMessage').textContent = message;
        if (this.errorToast) {
            this.errorToast.show();
        }
    }

    clearError() {
        if (this.errorToast) {
            this.errorToast.hide();
        }
    }

    clearSuccess() {
        if (this.successToast) {
            this.successToast.hide();
        }
    }

    getStatusBadgeClass(location) {
        if (!location.isActive) return 'bg-secondary';
        if (location.isFull) return 'bg-danger';
        if (location.capacityPercentage >= 80) return 'bg-warning';
        return 'bg-success';
    }

    getCapacityBarClass(location) {
        if (location.isFull) return 'bg-danger';
        if (location.capacityPercentage >= 80) return 'bg-warning';
        return 'bg-success';
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

    async exportToExcel() {
        try {
            const exportBtn = document.getElementById('exportExcelBtn');
            const originalText = exportBtn.innerHTML;
            
            // Show loading state
            exportBtn.disabled = true;
            exportBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Exporting...';

            // Get form data
            const formData = {
                dateFrom: document.getElementById('exportDateFrom').value || null,
                dateTo: document.getElementById('exportDateTo').value || null,
                statusFilter: document.getElementById('exportStatusFilter').value || null,
                locationTypeFilter: document.getElementById('exportLocationTypeFilter').value || null,
                capacityFrom: document.getElementById('exportCapacityFrom').value ? parseInt(document.getElementById('exportCapacityFrom').value) : null,
                capacityTo: document.getElementById('exportCapacityTo').value ? parseInt(document.getElementById('exportCapacityTo').value) : null,
                searchText: document.getElementById('exportSearchText').value || null
            };

            // Convert date strings to proper format
            if (formData.dateFrom) {
                formData.dateFrom = new Date(formData.dateFrom + 'T00:00:00').toISOString();
            }
            if (formData.dateTo) {
                formData.dateTo = new Date(formData.dateTo + 'T23:59:59').toISOString();
            }

            const response = await fetch('/api/location/export-excel', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(formData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Export failed');
            }

            // Get filename from response headers or create default
            const contentDisposition = response.headers.get('Content-Disposition');
            let filename = 'Locations_Export.xlsx';
            if (contentDisposition) {
                const filenameMatch = contentDisposition.match(/filename="(.+)"/);
                if (filenameMatch) {
                    filename = filenameMatch[1];
                }
            }

            // Download file
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            // Close modal and show success message
            bootstrap.Modal.getInstance(document.getElementById('exportModal')).hide();
            this.showSuccess('Excel file exported successfully!');

        } catch (error) {
            console.error('Error exporting to Excel:', error);
            this.showError('Failed to export to Excel: ' + error.message);
        } finally {
            // Restore button state
            const exportBtn = document.getElementById('exportExcelBtn');
            exportBtn.disabled = false;
            exportBtn.innerHTML = '<i class="fas fa-file-excel me-1"></i>Export to Excel';
        }
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
    bootstrap.Modal.getInstance(document.getElementById('detailsModal')).hide();
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

function openExportModal() {
    const modal = new bootstrap.Modal(document.getElementById('exportModal'));
    modal.show();
}

function exportToExcel() {
    locationManager.exportToExcel();
}

function confirmDeleteLocation() {
    locationManager.confirmDeleteLocation();
}

// Initialize when DOM is loaded
let locationManager;
document.addEventListener('DOMContentLoaded', function() {
    locationManager = new LocationManager();
});