/**
 * Customer Manager - AJAX-based Customer Management
 * Unified JavaScript module for all Customer operations
 */

class CustomerManager {
    constructor() {
        this.currentCustomerId = null;
        this.customers = [];
        this.statistics = {};
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalCount = 0;
        this.totalPages = 0;
        this.filters = {
            search: '',
            status: '',
            type: ''
        };

        // Toast instances for better management
        this.errorToast = null;
        this.successToast = null;

        this.init();
    }

    init() {
        this.initializeToasts();
        this.bindEvents();

        // Only load dashboard and customers if we're on the index page
        if (this.isIndexPage()) {
            this.loadDashboard();
            this.loadCustomers();
            this.setupRealTimeUpdates();
        }
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
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('input', this.debounce(() => {
                this.filters.search = searchInput.value;
                this.currentPage = 1;
                this.loadCustomers();
            }, 500));
        }

        const statusFilter = document.getElementById('statusFilter');
        if (statusFilter) {
            statusFilter.addEventListener('change', () => {
                this.filters.status = statusFilter.value;
                this.currentPage = 1;
                this.loadCustomers();
            });
        }

        const typeFilter = document.getElementById('typeFilter');
        if (typeFilter) {
            typeFilter.addEventListener('change', () => {
                this.filters.type = typeFilter.value;
                this.currentPage = 1;
                this.loadCustomers();
            });
        }

        // Pagination events
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('page-link')) {
                e.preventDefault();
                const page = parseInt(e.target.dataset.page);
                if (page && page !== this.currentPage) {
                    this.currentPage = page;
                    this.loadCustomers();
                }
            }
        });

        // Modal events
        // Event listener untuk customerModal (digunakan untuk create dan edit)
        const customerModal = document.getElementById('customerModal');
        if (customerModal) {
            customerModal.addEventListener('hidden.bs.modal', () => {
                // Reset form dan title saat modal ditutup
                this.resetForm();
                this.resetEditForm();
            });
        }
    }

    setupRealTimeUpdates() {
        // Refresh data every 30 seconds
        setInterval(() => {
            this.loadDashboard();
            this.loadCustomers();
        }, 30000);
    }

    // Dashboard Methods
    async loadDashboard() {
        try {
            const response = await fetch('/api/customer/dashboard', {
                method: 'GET',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                if (response.status === 404) {
                    console.warn('Customer dashboard API endpoint not found. Using default statistics.');
                    this.statistics = {
                        totalCustomers: 0,
                        activeCustomers: 0,
                        inactiveCustomers: 0,
                        customersWithOrders: 0,
                        newCustomersThisMonth: 0,
                        topCustomerType: 'Unknown'
                    };
                    this.updateDashboardUI();
                    return;
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            if (result.success) {
                this.statistics = result.data;
                this.updateDashboardUI();
            }
        } catch (error) {
            console.error('Error loading dashboard:', error);
            // Set default statistics on error
            this.statistics = {
                totalCustomers: 0,
                activeCustomers: 0,
                inactiveCustomers: 0,
                customersWithOrders: 0,
                newCustomersThisMonth: 0,
                topCustomerType: 'Unknown'
            };
            this.updateDashboardUI();
        }
    }

    updateDashboardUI() {
        // Update statistics cards
        this.updateElement('totalCustomersCount', this.statistics.totalCustomers || 0);
        this.updateElement('activeCustomersCount', this.statistics.activeCustomers || 0);
        this.updateElement('inactiveCustomersCount', this.statistics.inactiveCustomers || 0);
        this.updateElement('customersWithOrders', this.statistics.customersWithOrders || 0);
        this.updateElement('newCustomersThisMonth', this.statistics.newCustomersThisMonth || 0);
        this.updateElement('topCustomerType', this.statistics.topCustomerType || 'Unknown');
    }

    // Customer CRUD Methods
    
    showCreateModal() {
        this.resetForm();
        document.getElementById('customerModalTitle').textContent = 'Add New Customer';
        const modal = new bootstrap.Modal(document.getElementById('customerModal'));
        modal.show();
    }

    resetForm() {
        const form = document.getElementById('customerForm');
        if (form) {
            form.reset();
        }
        document.getElementById('customerId').value = '';
        this.clearValidation();
    }

    clearValidation() {
        const form = document.getElementById('customerForm');
        if (form) {
            const inputs = form.querySelectorAll('.is-invalid');
            inputs.forEach(input => input.classList.remove('is-invalid'));
        }
    }

    async loadCustomers() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                search: this.filters.search || '',
                status: this.filters.status || '',
                type: this.filters.type || ''
            });

            const response = await fetch(`/api/customer?${params}`, {
                method: 'GET',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                if (response.status === 404) {
                    console.warn('Customer API endpoint not found. Using empty data.');
                    this.customers = [];
                    this.totalCount = 0;
                    this.totalPages = 0;
                    this.updateCustomersTable();
                    this.updatePagination(this.totalCount, this.totalPages);
                    return;
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            console.log('=== LOAD CUSTOMERS API RESPONSE ===');
            console.log('API Response:', result);
            console.log('API Response success:', result.success);
            console.log('API Response data:', result.data);
            console.log('API Response pagination:', result.pagination);

            if (result.success) {
                this.customers = result.data;
                this.totalCount = result.pagination.totalCount;
                this.totalPages = result.pagination.totalPages;

                console.log('✅ Customers loaded successfully');
                console.log('✅ Total customers:', this.customers.length);
                console.log('✅ First customer sample:', this.customers[0]);
                console.log('=== END LOAD CUSTOMERS API RESPONSE ===');

                this.updateCustomersTable();
                this.updatePagination(this.totalCount, this.totalPages);
            } else {
                console.error('❌ Failed to load customers:', result.message);
            }
        } catch (error) {
            console.error('Error loading customers:', error);
            // Show empty state instead of error for better UX
            this.customers = [];
            this.totalCount = 0;
            this.totalPages = 0;
            this.updateCustomersTable();
            this.updatePagination(this.totalCount, this.totalPages);
        }
    }

    updateCustomersTable() {
        const container = document.getElementById('customersTableContainer');
        if (!container) return;

        if (this.customers.length === 0) {
            container.innerHTML = '<div class="text-center py-5"><p>No customers found</p></div>';
            return;
        }

        let html = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>CODE</th>
                            <th>CUSTOMER</th>
                            <th>PHONE</th>
                            <th>CITY</th>
                            <th>TYPE</th>
                            <th>STATUS</th>
                            <th>ORDERS</th>
                            <th>ACTIONS</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        this.customers.forEach((customer, index) => {
            if (!customer.id) return; // Skip customer tanpa ID

            html += `
                <tr>
                    <td>${customer.code}</td>
                    <td>
                        <div class="d-flex align-items-center">
                            <div class="avatar-sm bg-primary text-white rounded-circle d-flex align-items-center justify-content-center me-2">
                                ${customer.name.charAt(0).toUpperCase()}
                            </div>
                            <div>
                                <div class="fw-bold">${customer.name}</div>
                                <small class="text-muted">${customer.email}</small>
                            </div>
                        </div>
                    </td>
                    <td>${customer.phone || '-'}</td>
                    <td>${customer.city || '-'}</td>
                    <td>
                        <span class="badge bg-${customer.customerType === 'Individual' ? 'info' : 'success'}">
                            ${customer.customerType}
                        </span>
                    </td>
                    <td>
                        <span class="badge bg-${customer.isActive ? 'success' : 'secondary'}">
                            ${customer.isActive ? 'Active' : 'Inactive'}
                        </span>
                    </td>
                    <td>
                        <div class="text-end">
                            <div class="fw-bold">${customer.totalOrders}</div>
                            <small class="text-muted">orders</small>
                        </div>
                    </td>
                    <td>
                        <div class="btn-group" role="group">
                            <button class="btn btn-sm btn-info" onclick="customerManager.viewCustomer(${customer.id})" title="View">
                                <i class="fas fa-eye"></i>
                            </button>
                            <button class="btn btn-sm btn-primary" onclick="customerManager.editCustomer(${customer.id})" title="Edit">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="customerManager.deleteCustomer(${customer.id})" title="Delete">
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

    updatePagination(totalCount, totalPages) {
        if (totalCount !== undefined) this.totalCount = totalCount;
        if (totalPages !== undefined) this.totalPages = totalPages;
        
        // Update pagination info
        const startRecord = this.totalCount > 0 ? ((this.currentPage - 1) * this.pageSize) + 1 : 0;
        const endRecord = Math.min(this.currentPage * this.pageSize, this.totalCount);
        
        const showingStartEl = document.getElementById('showingStart');
        const showingEndEl = document.getElementById('showingEnd');
        const totalRecordsEl = document.getElementById('totalRecords');
        const currentPageNumEl = document.getElementById('currentPageNum');
        const totalPagesNumEl = document.getElementById('totalPagesNum');
        const prevPageBtnEl = document.getElementById('prevPageBtn');
        const nextPageBtnEl = document.getElementById('nextPageBtn');
        
        if (showingStartEl) showingStartEl.textContent = startRecord;
        if (showingEndEl) showingEndEl.textContent = endRecord;
        if (totalRecordsEl) totalRecordsEl.textContent = this.totalCount;
        if (currentPageNumEl) currentPageNumEl.textContent = this.currentPage;
        if (totalPagesNumEl) totalPagesNumEl.textContent = this.totalPages;
        
        // Update button states
        if (prevPageBtnEl) prevPageBtnEl.disabled = this.currentPage === 1;
        if (nextPageBtnEl) nextPageBtnEl.disabled = this.currentPage >= this.totalPages;
    }

    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadCustomers();
        }
    }

    nextPage() {
        if (this.currentPage < this.totalPages) {
            this.currentPage++;
            this.loadCustomers();
        }
    }

    changePageSize(newPageSize) {
        this.pageSize = parseInt(newPageSize);
        this.currentPage = 1; // Reset to first page
        this.loadCustomers();
    }

    // Customer Actions
    async viewCustomer(id) {
        try {
            const response = await fetch(`/api/customer/${id}`, {
                method: 'GET',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            if (result.success) {
                this.showCustomerDetails(result.data);
            }
        } catch (error) {
            console.error('Error loading customer:', error);
            this.showError('Failed to load customer details');
        }
    }

    showCustomerDetails(customer) {
        const statistics = customer.statistics || {};
        const audit = customer.audit || {};
        const recentOrders = Array.isArray(customer.recentOrders) ? customer.recentOrders : [];

        const ordersTable = recentOrders.length > 0
            ? `
                <div class="table-responsive">
                    <table class="table table-sm table-striped">
                        <thead class="table-light">
                            <tr>
                                <th>SO Number</th>
                                <th>Date</th>
                                <th>Status</th>
                                <th class="text-end">Items</th>
                                <th class="text-end">Total</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${recentOrders.map(order => `
                                <tr>
                                    <td><span class="badge bg-primary">${order.soNumber || '-'}</span></td>
                                    <td>${order.orderDate ? new Date(order.orderDate).toLocaleString('id-ID') : 'N/A'}</td>
                                    <td><span class="badge bg-info">${order.status || '-'}</span></td>
                                    <td class="text-end">${order.itemsCount || 0}</td>
                                    <td class="text-end">Rp ${order.totalAmount ? order.totalAmount.toLocaleString('id-ID') : '0'}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>`
            : '<div class="alert alert-light mb-0">No recent sales orders for this customer.</div>';

        const content = `
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-info-circle me-2"></i>Basic Information</h6>
                            <dl class="row mb-0">
                                <dt class="col-sm-4">Customer Code</dt>
                                <dd class="col-sm-8"><span class="badge bg-primary">${customer.code || 'N/A'}</span></dd>
                                <dt class="col-sm-4">Name</dt>
                                <dd class="col-sm-8">${customer.name || '-'}</dd>
                                <dt class="col-sm-4">Email</dt>
                                <dd class="col-sm-8">${customer.email || '-'}</dd>
                                <dt class="col-sm-4">Phone</dt>
                                <dd class="col-sm-8">${customer.phone || 'N/A'}</dd>
                                <dt class="col-sm-4">Type</dt>
                                <dd class="col-sm-8">${customer.customerType || '-'}</dd>
                                <dt class="col-sm-4">Status</dt>
                                <dd class="col-sm-8">
                                    <span class="badge ${customer.isActive ? 'bg-success' : 'bg-secondary'}">
                                        ${customer.isActive ? 'Active' : 'Inactive'}
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
                            <p class="mb-1"><strong>Address:</strong> ${customer.address || 'N/A'}</p>
                            <p class="mb-1"><strong>City:</strong> ${customer.city || 'N/A'}</p>
                            <p class="mb-1"><strong>Created:</strong> ${audit.createdDate ? new Date(audit.createdDate).toLocaleString('id-ID') : 'N/A'}</p>
                            ${audit.modifiedDate ? `<p class="mb-1"><strong>Modified:</strong> ${new Date(audit.modifiedDate).toLocaleString('id-ID')}</p>` : ''}
                            <p class="mb-1"><strong>Created By:</strong> ${audit.createdBy || 'System'}</p>
                            <p class="mb-0"><strong>Modified By:</strong> ${audit.modifiedBy || 'N/A'}</p>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row g-4 mt-1">
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-chart-line me-2"></i>Order Statistics</h6>
                            <div class="d-flex justify-content-between mb-2">
                                <span>Total Orders</span>
                                <strong>${statistics.totalOrders || 0}</strong>
                            </div>
                            <div class="d-flex justify-content-between mb-2">
                                <span>Total Order Value</span>
                                <strong>Rp ${statistics.totalValue ? statistics.totalValue.toLocaleString('id-ID') : '0'}</strong>
                            </div>
                            <div class="d-flex justify-content-between">
                                <span>Open Orders</span>
                                <strong>${statistics.openOrders || 0}</strong>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-lg-6">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="text-primary"><i class="fas fa-history me-2"></i>Recent Sales Orders</h6>
                            ${ordersTable}
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.getElementById('customerDetailsContainer').innerHTML = content;
        const modal = new bootstrap.Modal(document.getElementById('viewCustomerModal'));
        modal.show();
    }

    async editCustomer(id) {
        try {
            console.log('=== EDIT CUSTOMER CALLED ===');
            console.log('Parameter id:', id, 'Type:', typeof id);
            console.log('Parameter id value:', JSON.stringify(id));
            console.log('Is parameter null?', id === null);
            console.log('Is parameter undefined?', id === undefined);
            console.log('Is parameter empty string?', id === '');
            console.log('Is parameter "null" string?', id === 'null');
            console.log('this.currentCustomerId before:', this.currentCustomerId);

            // Validasi parameter customer ID
            if (!id || id === 'null' || id === '' || id === 'undefined') {
                console.error('❌ Invalid customer ID parameter:', id);
                console.error('Parameter type:', typeof id);
                this.showError('Invalid customer ID: ' + id);
                return;
            }

            // Pastikan ID adalah number atau string yang valid
            const customerId = parseInt(id) || id.toString();
            console.log('✅ Processed customer ID:', customerId);

            // Validasi customer ID setelah processing
            if (!customerId || isNaN(customerId) || customerId <= 0) {
                console.error('❌ Invalid processed customer ID:', customerId);
                this.showError('Invalid customer ID format: ' + customerId);
                return;
            }

            console.log('🌐 Making API request to:', `/api/customer/${customerId}`);
            const response = await fetch(`/api/customer/${customerId}`, {
                method: 'GET',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            console.log('📡 API Response status:', response.status);
            console.log('📡 API Response ok:', response.ok);

            if (!response.ok) {
                if (response.status === 404) {
                    console.error('❌ Customer not found for ID:', customerId);
                    this.showError('Customer not found');
                    return;
                }
                console.error('❌ HTTP error:', response.status, response.statusText);
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            console.log('📦 API Response:', result);
            console.log('📦 API Response success:', result.success);
            console.log('📦 API Response data:', result.data);

            if (result.success && result.data) {
                console.log('=== EDIT CUSTOMER DEBUG ===');
                console.log('Customer ID from parameter:', customerId);
                console.log('Customer data from API:', result.data);
                console.log('Customer ID from data:', result.data?.id);
                console.log('Customer ID from data type:', typeof result.data?.id);

                // Validasi data customer dari API
                if (!result.data.id) {
                    console.error('❌ Customer data from API is missing ID');
                    console.error('Full customer data:', JSON.stringify(result.data, null, 2));
                    this.showError('Invalid customer data received from server');
                    return;
                }

                // Set currentCustomerId dulu
                this.currentCustomerId = customerId;
                console.log('✅ Current customerId set to:', this.currentCustomerId);

                // Populate form dengan data yang valid
                this.populateEditForm(result.data);

                // Update modal title
                const modalTitle = document.getElementById('customerModalTitle');
                if (modalTitle) {
                    modalTitle.textContent = 'Edit Customer';
                }

                // Verifikasi bahwa form berhasil di-populate
                const hiddenInput = document.getElementById('customerId');
                console.log('🔍 Hidden input element:', hiddenInput);
                console.log('🔍 Hidden input value after populate:', hiddenInput?.value);
                console.log('🔍 Hidden input value type:', typeof hiddenInput?.value);

                console.log('=== END EDIT CUSTOMER DEBUG ===');

                // Show modal (gunakan customerModal, bukan editCustomerModal)
                const modalElement = document.getElementById('customerModal');
                if (!modalElement) {
                    console.error('❌ customerModal element not found');
                    this.showError('Modal not found');
                    return;
                }
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
            } else {
                console.error('❌ Invalid response from API:', result);
                this.showError(result.message || 'Failed to load customer data');
            }
        } catch (error) {
            console.error('❌ Error loading customer for edit:', error);
            console.error('❌ Error stack:', error.stack);
            this.showError('Failed to load customer for editing: ' + error.message);
        }
    }

    populateEditForm(customer) {
        console.log('=== POPULATE EDIT FORM DEBUG ===');
        console.log('Customer object received:', customer);
        console.log('Customer ID from object:', customer?.id);
        console.log('Customer ID type:', typeof customer?.id);

        // Validasi customer object
        if (!customer) {
            console.error('Customer object is null/undefined');
            this.showError('Invalid customer data received');
            return;
        }

        if (!customer.id) {
            console.error('Customer ID is null/undefined');
            this.showError('Customer ID is missing');
            return;
        }

        // Set customer ID to hidden input (gunakan customerId, bukan editCustomerId)
        const customerIdEl = document.getElementById('customerId');
        if (!customerIdEl) {
            console.error('customerId element not found!');
            this.showError('Edit form not properly initialized');
            return;
        }

        // Pastikan customer ID adalah string
        const customerIdStr = customer.id.toString();
        customerIdEl.value = customerIdStr;
        console.log('Customer ID set to hidden input:', customerIdStr);

        // Set juga currentCustomerId untuk backward compatibility
        this.currentCustomerId = customer.id;
        console.log('Current Customer ID set to:', this.currentCustomerId);

        // Populate form fields (gunakan IDs yang sama dengan create form)
        const codeEl = document.getElementById('customerCode');
        const nameEl = document.getElementById('customerName');
        const emailEl = document.getElementById('customerEmail');
        const phoneEl = document.getElementById('customerPhone');
        const addressEl = document.getElementById('customerAddress');
        const cityEl = document.getElementById('customerCity');
        const typeEl = document.getElementById('customerType');
        const notesEl = document.getElementById('customerNotes');
        const activeEl = document.getElementById('isActive');

        if (codeEl) codeEl.value = customer.code || '';
        if (nameEl) nameEl.value = customer.name || '';
        if (emailEl) emailEl.value = customer.email || '';
        if (phoneEl) phoneEl.value = customer.phone || '';
        if (addressEl) addressEl.value = customer.address || '';
        if (cityEl) cityEl.value = customer.city || '';
        if (typeEl) typeEl.value = customer.customerType || 'Individual';
        if (notesEl) notesEl.value = customer.notes || '';
        if (activeEl) activeEl.checked = customer.isActive !== false;

        console.log('Form populated successfully for customer ID:', customer.id);
        console.log('Hidden input value after population:', customerIdEl.value);
        console.log('=== END POPULATE EDIT FORM DEBUG ===');
    }

    async updateCustomer() {
        try {
            console.log('=== UPDATE CUSTOMER DEBUG ===');
            console.log('Current customerId from instance:', this.currentCustomerId);
            console.log('Current customerId type:', typeof this.currentCustomerId);

            // Prioritas 1: Ambil dari hidden input (gunakan customerId, bukan editCustomerId)
            const customerIdEl = document.getElementById('customerId');
            console.log('🔍 Hidden input element:', customerIdEl);
            let customerId = customerIdEl ? customerIdEl.value : null;
            console.log('🔍 Customer ID from hidden input:', customerId);
            console.log('🔍 Customer ID from hidden input type:', typeof customerId);
            console.log('🔍 Hidden input value length:', customerId?.length);

            // Prioritas 2: Fallback ke currentCustomerId jika hidden input kosong
            if (!customerId || customerId === 'null' || customerId === '' || customerId === 'undefined') {
                console.log('⚠️ Hidden input is empty, trying fallback...');
                if (this.currentCustomerId) {
                    customerId = this.currentCustomerId.toString();
                    console.log('✅ Using fallback currentCustomerId:', customerId);

                    // Update hidden input juga
                    if (customerIdEl) {
                        customerIdEl.value = customerId;
                        console.log('✅ Updated hidden input with fallback value');
                    }
                } else {
                    console.error('❌ No fallback currentCustomerId available');
                }
            } else {
                console.log('✅ Hidden input has valid value:', customerId);
            }

            // Validasi final customer ID
            if (!customerId || customerId === 'null' || customerId === '' || customerId === 'undefined') {
                console.error('❌ No valid customer ID found');
                console.error('❌ Hidden input value:', customerIdEl?.value);
                console.error('❌ Current customer ID:', this.currentCustomerId);
                console.error('❌ All DOM elements check:');
                console.error('  - customerId element exists:', !!customerIdEl);
                console.error('  - customerId value:', customerIdEl?.value);
                console.error('  - currentCustomerId:', this.currentCustomerId);
                console.error('  - window.customerManager exists:', !!window.customerManager);
                console.error('  - window.customerManager.currentCustomerId:', window.customerManager?.currentCustomerId);
                this.showError('No customer selected for editing. Please close the modal and try again.');
                return;
            }

            console.log('✅ Using customer ID for update:', customerId);
            console.log('✅ Final customer ID type:', typeof customerId);

            // Collect form data (gunakan IDs yang sama dengan create form)
            const formData = {
                customerCode: document.getElementById('customerCode')?.value || '',
                name: document.getElementById('customerName')?.value || '',
                email: document.getElementById('customerEmail')?.value || '',
                phone: document.getElementById('customerPhone')?.value || '',
                address: document.getElementById('customerAddress')?.value || '',
                city: document.getElementById('customerCity')?.value || '',
                customerType: document.getElementById('customerType')?.value || 'Individual',
                notes: document.getElementById('customerNotes')?.value || '',
                isActive: document.getElementById('isActive')?.checked || false
            };

            console.log('📝 Form data to be sent:', formData);
            console.log('📝 Form elements check:');
            console.log('  - customerName element:', !!document.getElementById('customerName'));
            console.log('  - customerEmail element:', !!document.getElementById('customerEmail'));
            console.log('  - customerId element:', !!document.getElementById('customerId'));

            // Validate required fields
            if (!formData.name.trim() || !formData.email.trim()) {
                console.error('❌ Required fields validation failed');
                console.error('❌ Name:', formData.name);
                console.error('❌ Email:', formData.email);
                this.showError('Name and Email are required fields');
                return;
            }

            console.log('🌐 Making API call to:', `/api/customer/${customerId}`);
            console.log('🌐 Request URL will be:', `${window.location.origin}/api/customer/${customerId}`);

            const response = await fetch(`/api/customer/${customerId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(formData)
            });

            console.log('📡 Response status:', response.status);
            console.log('📡 Response ok:', response.ok);
            console.log('📡 Response statusText:', response.statusText);

            if (!response.ok) {
                let errorMessage = 'Update failed';
                try {
                    const errorData = await response.json();
                    console.log('❌ Error response data:', errorData);

                    // Handle validation errors
                    if (errorData.errors) {
                        errorMessage = 'Validation failed:\n';
                        for (const [field, errors] of Object.entries(errorData.errors)) {
                            errorMessage += `• ${field}: ${errors.join(', ')}\n`;
                        }
                    } else if (errorData.message) {
                        errorMessage = errorData.message;
                    }
                } catch (jsonError) {
                    console.warn('⚠️ Could not parse error response:', jsonError);
                    errorMessage = `Server error: ${response.status} ${response.statusText}`;
                }

                this.showError(errorMessage);
                return;
            }

            const result = await response.json();
            console.log('✅ Success response:', result);

            if (result.success) {
                this.showSuccess(result.message);
                const modal = bootstrap.Modal.getInstance(document.getElementById('customerModal'));
                if (modal) {
                    modal.hide();
                }
                this.loadCustomers();

                // Reset current customer ID
                this.currentCustomerId = null;
                console.log('✅ Update successful, reset currentCustomerId');
            } else {
                console.error('❌ Update failed:', result.message);
                this.showError(result.message || 'Update failed');
            }
        } catch (error) {
            console.error('❌ Error updating customer:', error);
            console.error('❌ Error stack:', error.stack);
            this.showError(error.message || 'Failed to update customer');
        }
        console.log('=== END UPDATE CUSTOMER DEBUG ===');
    }

    async deleteCustomer(id) {
        if (!confirm('Are you sure you want to delete this customer?')) {
            return;
        }

        try {
            const response = await fetch(`/api/customer/${id}`, {
                method: 'DELETE',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Delete failed');
            }

            const result = await response.json();
            if (result.success) {
                this.showSuccess(result.message);
                this.loadCustomers();
                this.loadDashboard();
            }
        } catch (error) {
            console.error('Error deleting customer:', error);
            this.showError(error.message || 'Failed to delete customer');
        }
    }

    // Create Customer
    async createCustomer() {
        try {
            console.log('=== CREATE CUSTOMER START ===');
            
            const formData = {
                customerCode: document.getElementById('customerCode').value,
                name: document.getElementById('customerName').value,
                email: document.getElementById('customerEmail').value,
                phone: document.getElementById('customerPhone').value,
                address: document.getElementById('customerAddress').value,
                city: document.getElementById('customerCity').value,
                customerType: document.getElementById('customerType').value,
                isActive: document.getElementById('isActive').checked
            };

            console.log('Form data:', formData);

            const response = await fetch('/api/customer', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(formData)
            });

            console.log('API Response status:', response.status);

            if (!response.ok) {
                if (response.status === 404) {
                    this.showError('Customer API endpoint not available. Please contact administrator.');
                    return;
                }

                let errorMessage = 'Create failed';
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.message || errorMessage;

                    // Handle ModelState validation errors
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
            console.log('API Response data:', result);
            
            if (result.success) {
                this.showSuccess(result.message);
                this.loadCustomers();
                this.loadDashboard();
                this.resetForm();
                this.closeModal();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error creating customer:', error);
            this.showError(error.message || 'Failed to create customer');
        }
    }

    // Form handling methods
    handleFormSubmit(event) {
        event.preventDefault();
        console.log('=== FORM SUBMIT HANDLED ===');
        
        if (window.customerManager) {
            const customerId = document.getElementById('customerId').value;
            if (customerId) {
                console.log('Updating existing customer:', customerId);
                window.customerManager.updateCustomer();
            } else {
                console.log('Creating new customer');
                window.customerManager.createCustomer();
            }
        } else {
            console.error('customerManager not available');
        }
        
        return false; // Pastikan return false untuk mencegah form submission
    }

    resetForm() {
        document.getElementById('customerForm').reset();
        document.getElementById('customerId').value = '';
        document.getElementById('customerModalTitle').textContent = 'Add New Customer';
    }

    closeModal() {
        const modal = bootstrap.Modal.getInstance(document.getElementById('customerModal'));
        if (modal) {
            modal.hide();
        }
    }

    // Form Management
    resetCreateForm() {
        document.getElementById('createCustomerForm').reset();
        document.getElementById('createIsActive').checked = true;
    }

    resetEditForm() {
        console.log('=== RESET EDIT FORM ===');
        console.log('Resetting currentCustomerId from:', this.currentCustomerId);

        this.currentCustomerId = null;

        // Reset hidden input (gunakan customerId, bukan editCustomerId)
        const customerIdEl = document.getElementById('customerId');
        if (customerIdEl) {
            customerIdEl.value = '';
            console.log('Reset hidden input customerId');
        }

        // Reset modal title
        const modalTitle = document.getElementById('customerModalTitle');
        if (modalTitle) {
            modalTitle.textContent = 'Add New Customer';
        }

        // Reset form (gunakan customerForm, bukan editCustomerForm)
        const form = document.getElementById('customerForm');
        if (form) {
            form.reset();
            console.log('Reset customer form');
        }

        console.log('=== END RESET EDIT FORM ===');
    }

    // Export
    exportCustomers() {
        window.open('/api/customer/export', '_blank');
    }

    // Utility Methods
    updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
    }

    showSuccess(message) {
        const toastElement = document.getElementById('successToast');
        if (toastElement) {
            const toastBody = toastElement.querySelector('.toast-body');
            if (toastBody) {
                toastBody.textContent = message;
            }
            this.successToast.show();
        }
    }

    showError(message) {
        const toastElement = document.getElementById('errorToast');
        if (toastElement) {
            const toastBody = toastElement.querySelector('.toast-body');
            if (toastBody) {
                toastBody.textContent = message;
            }
            this.errorToast.show();
        }
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

    // Helper method to check if we're on the index page
    isIndexPage() {
        return window.location.pathname.endsWith('/Customer') ||
            window.location.pathname.endsWith('/Customer/') ||
            window.location.pathname.endsWith('/Customer/Index');
    }

    // Clear filters
    clearFilters() {
        this.filters = {
            search: '',
            status: '',
            type: ''
        };
        
        // Reset filter inputs
        const searchInput = document.getElementById('searchInput');
        const statusFilter = document.getElementById('statusFilter');
        const typeFilter = document.getElementById('typeFilter');
        
        if (searchInput) searchInput.value = '';
        if (statusFilter) statusFilter.value = '';
        if (typeFilter) typeFilter.value = '';
        
        this.currentPage = 1;
        this.loadCustomers();
    }

    // Method to load customer details for Details page
    async loadCustomerDetails(customerId) {
        try {
            const response = await fetch(`/api/customer/${customerId}`, {
                method: 'GET',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            if (result.success) {
                this.displayCustomerDetails(result.data);
            }
        } catch (error) {
            console.error('Error loading customer details:', error);
            this.showError('Failed to load customer details');
        }
    }

    displayCustomerDetails(customer) {
        const detailsContainer = document.getElementById('customerDetails');
        if (!detailsContainer) return;

        detailsContainer.innerHTML = `
            <div class="row">
                <div class="col-md-6">
                    <h6>Informasi Dasar</h6>
                    <table class="table table-sm">
                        <tr><td><strong>Kode:</strong></td><td>${customer.code || '-'}</td></tr>
                        <tr><td><strong>Nama:</strong></td><td>${customer.name}</td></tr>
                        <tr><td><strong>Email:</strong></td><td>${customer.email}</td></tr>
                        <tr><td><strong>Telepon:</strong></td><td>${customer.phone || '-'}</td></tr>
                        <tr><td><strong>Tipe:</strong></td><td>${customer.customerType || '-'}</td></tr>
                        <tr><td><strong>Status:</strong></td><td>
                            <span class="badge bg-${customer.isActive ? 'success' : 'secondary'}">
                                ${customer.isActive ? 'Aktif' : 'Tidak Aktif'}
                            </span>
                        </td></tr>
                    </table>
                </div>
                <div class="col-md-6">
                    <h6>Alamat & Statistik</h6>
                    <table class="table table-sm">
                        <tr><td><strong>Alamat:</strong></td><td>${customer.address || '-'}</td></tr>
                        <tr><td><strong>Kota:</strong></td><td>${customer.city || '-'}</td></tr>
                        <tr><td><strong>Total Order:</strong></td><td>${customer.totalOrders || 0}</td></tr>
                        <tr><td><strong>Total Nilai:</strong></td><td>$${customer.totalValue || 0}</td></tr>
                        <tr><td><strong>Dibuat:</strong></td><td>${this.formatDate(customer.createdDate)}</td></tr>
                        <tr><td><strong>Dibuat Oleh:</strong></td><td>${customer.createdBy || '-'}</td></tr>
                    </table>
                </div>
            </div>
            <div class="mt-3">
                <a href="/Customer/Edit/${customer.id}" class="btn btn-warning me-2">
                    <i class="fas fa-edit me-1"></i>Edit Customer
                </a>
                <button type="button" class="btn btn-danger" onclick="customerManager.deleteCustomer(${customer.id})">
                    <i class="fas fa-trash me-1"></i>Hapus Customer
                </button>
            </div>
        `;
    }

    // Method to load customer for Edit page
    async loadCustomerForEdit(customerId) {
        try {
            const response = await fetch(`/api/customer/${customerId}`, {
                method: 'GET',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            if (result.success) {
                this.populateEditForm(result.data);
                this.currentCustomerId = customerId;

                // Hide loading and show form
                const loadingMessage = document.getElementById('loadingMessage');
                const editForm = document.getElementById('editCustomerForm');
                if (loadingMessage) loadingMessage.style.display = 'none';
                if (editForm) editForm.style.display = 'block';
            }
        } catch (error) {
            console.error('Error loading customer for edit:', error);
            this.showError('Failed to load customer for editing');
        }
    }

}

// Global functions for HTML onclick events
function createCustomer() {
    customerManager.createCustomer();
}

function updateCustomer() {
    console.log('=== GLOBAL UPDATE CUSTOMER DEBUG ===');
    console.log('🔄 Global updateCustomer() called');
    console.log('🔄 window.customerManager exists:', !!window.customerManager);
    console.log('🔄 customerManager type:', typeof window.customerManager);
    console.log('🔄 customerManager currentCustomerId:', window.customerManager?.currentCustomerId);
    console.log('🔄 window.customerManager object:', window.customerManager);

    if (window.customerManager) {
        console.log('✅ Calling customerManager.updateCustomer()');
        try {
            customerManager.updateCustomer();
        } catch (error) {
            console.error('❌ Error calling customerManager.updateCustomer():', error);
            console.error('❌ Error stack:', error.stack);
            alert('Error calling update customer: ' + error.message);
        }
    } else {
        console.error('❌ customerManager not found!');
        console.log('🔍 Available window properties:', Object.keys(window).filter(key => key.includes('Manager')));
        console.log('🔍 All window properties containing "customer":', Object.keys(window).filter(key => key.toLowerCase().includes('customer')));
        alert('Error: Customer manager not initialized. Please refresh the page.');
    }
    console.log('=== END GLOBAL UPDATE CUSTOMER DEBUG ===');
}

function exportCustomers() {
    customerManager.exportCustomers();
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    console.log('=== CUSTOMER MANAGER INITIALIZATION ===');
    console.log('DOM loaded, initializing customerManager...');

    let customerManager = new CustomerManager();
    window.customerManager = customerManager; // Make it globally available

    console.log('customerManager initialized:', !!customerManager);
    console.log('customerManager type:', typeof customerManager);
    console.log('customerManager currentCustomerId:', customerManager.currentCustomerId);
    console.log('=== END CUSTOMER MANAGER INITIALIZATION ===');
});
