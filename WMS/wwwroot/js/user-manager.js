/**
 * User Manager - AJAX-based User Management
 * Unified JavaScript module for all User operations
 */

class UserManager {
    constructor() {
        this.currentUserId = null;
        this.users = [];
        this.statistics = {};
        this.roles = [];
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalCount = 0;
        this.totalPages = 0;
        this.filters = {
            status: '',
            role: '',
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
        // Removed loadRoles() - auto-assign WarehouseStaff
        this.loadDashboard();
        this.loadUsers();
        this.setupRealTimeUpdates();
    }

    getCurrentUserId() {
        // Try to get current user ID from various sources
        // This could be from a hidden input, data attribute, or API call
        const userIdInput = document.querySelector('input[name="CurrentUserId"]');
        if (userIdInput) {
            return userIdInput.value;
        }
        
        // Try to get from data attribute on body
        const body = document.body;
        if (body.dataset.currentUserId) {
            return body.dataset.currentUserId;
        }
        
        // Fallback: try to get from user info in the page
        const userInfo = document.querySelector('[data-user-id]');
        if (userInfo) {
            return userInfo.dataset.userId;
        }
        
        return null;
    }

    showLockedMessage() {
        this.showError('You cannot modify your own account. Please contact another admin to make changes to your profile.');
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
            this.loadUsers();
        });

        document.getElementById('roleFilter')?.addEventListener('change', (e) => {
            this.filters.role = e.target.value;
            this.currentPage = 1; // Reset to first page
            this.loadUsers();
        });

        document.getElementById('searchInput')?.addEventListener('input', 
            this.debounce((e) => {
                this.filters.search = e.target.value;
                this.currentPage = 1; // Reset to first page
                this.loadUsers();
            }, 300)
        );

        // Username validation - HANYA element yang ada di HTML
        document.getElementById('username')?.addEventListener('blur', (e) => {
            this.validateUsername(e.target.value);
        });

        // Email validation - HANYA element yang ada di HTML
        document.getElementById('email')?.addEventListener('blur', (e) => {
            this.validateEmail(e.target.value);
        });



        // Change Password form submission
        document.getElementById('changePasswordForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.changePassword();
        });

        // Modal events - HANYA element yang ada di HTML
        document.getElementById('userModal')?.addEventListener('hidden.bs.modal', () => {
            this.resetForm();
        });

        // Form submission prevention - HANYA element yang ada di HTML
        document.getElementById('userForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveUser();
        });

        // HAPUS semua binding untuk element yang tidak ada:
        // - pageSizeSelect (tidak ada di HTML)
        // - refreshBtn (tidak ada di HTML)
        // - paginationNav (tidak ada di HTML)
        // - usersTableBody (tidak ada di HTML)
    }

    // Removed loadRoles() - auto-assign WarehouseStaff role

    // Removed populateRolesDropdown() - auto-assign WarehouseStaff role

    async loadDashboard() {
        try {
            const response = await fetch('/api/user/dashboard');
            
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

    async loadUsers() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                ...this.filters
            });

            const response = await fetch(`/api/user?${params}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.users = data.data.items;
                this.totalCount = data.data.totalCount;
                this.totalPages = data.data.totalPages;
                
                this.renderUsersTable();
                this.updatePagination(this.totalCount, this.totalPages);
                this.clearError(); // Clear any previous errors
            } else {
                // Handle API response with success = false
                const errorMessage = data.message || 'Failed to load users';
                console.error('Users API Error:', errorMessage, data);
                this.showError(errorMessage);
            }
        } catch (error) {
            console.error('Error loading users:', error);
            this.showError('Failed to load users');
            // Reset pagination on error
            this.users = [];
            this.totalCount = 0;
            this.totalPages = 0;
            this.updatePagination(this.totalCount, this.totalPages);
        }
    }

    updateStatisticsCards() {
        document.getElementById('totalUsersCount').textContent = this.statistics.totalUsers || 0;
        document.getElementById('activeUsersCount').textContent = this.statistics.activeUsers || 0;
        document.getElementById('adminUsersCount').textContent = this.statistics.adminUsers || 0;
        document.getElementById('warehouseStaffCount').textContent = this.statistics.warehouseStaffUsers || 0;
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

    goToPage(page) {
        if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
            this.currentPage = page;
            this.loadUsers();
        }
    }

    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadUsers();
        }
    }

    nextPage() {
        if (this.currentPage < this.totalPages) {
            this.currentPage++;
            this.loadUsers();
        }
    }

    changePageSize(newPageSize) {
        this.pageSize = parseInt(newPageSize);
        this.currentPage = 1; // Reset to first page
        this.loadUsers();
    }

    renderUsersTable() {
        const container = document.getElementById('usersTableContainer');
        if (!container) return;

        if (this.users.length === 0) {
            container.innerHTML = '<div class="text-center py-5"><p>No users found</p></div>';
            return;
        }

        let html = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>USERNAME</th>
                            <th>FULL NAME</th>
                            <th>EMAIL</th>
                            <th>ROLES</th>
                            <th>STATUS</th>
                            <th>LAST LOGIN</th>
                            <th>ACTIONS</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        this.users.forEach(user => {
            html += `
                <tr>
                    <td>
                        <div class="fw-medium text-primary">${user.username}</div>
                        <div class="small text-muted">
                            <i class="fas fa-user me-1"></i>
                            ID: ${user.id}
                        </div>
                    </td>
                    <td>
                        <div class="fw-medium">${user.fullName}</div>
                        ${user.phone ? `<div class="small text-muted"><i class="fas fa-phone me-1"></i>${user.phone}</div>` : ''}
                    </td>
                    <td>
                        <div class="fw-medium">${user.email}</div>
                        <div class="small text-muted">
                            <i class="fas fa-envelope me-1"></i>
                            Email verified
                        </div>
                    </td>
                    <td>
                        <div class="d-flex flex-wrap gap-1">
                            ${user.roleNames.map(role => `
                                <span class="badge ${this.getRoleBadgeClass(role)}">${role}</span>
                            `).join('')}
                        </div>
                    </td>
                    <td>
                        <span class="badge ${user.isActive ? 'bg-success' : 'bg-secondary'}">
                            ${user.isActive ? 'Active' : 'Inactive'}
                        </span>
                    </td>
                    <td>
                        ${user.lastLoginDate ? `
                            <span class="fw-medium">${this.formatDate(user.lastLoginDate)}</span>
                            <div class="small text-muted">${this.formatTime(user.lastLoginDate)}</div>
                        ` : '<span class="text-muted">Never</span>'}
                    </td>
                    <td>
                        <div class="btn-group" role="group">
                            <button type="button" class="btn btn-sm ${user.isSelfEdit ? 'btn-secondary disabled btn-locked' : 'btn-info'}" 
                                    onclick="${user.isSelfEdit ? 'userManager.showLockedMessage()' : 'userManager.viewUser(' + user.id + ')'}" 
                                    title="${user.isSelfEdit ? 'You cannot view your own account' : 'View Details'}"
                                    ${user.isSelfEdit ? 'disabled' : ''}>
                                <i class="fas ${user.isSelfEdit ? 'fa-lock' : 'fa-eye'}"></i>
                            </button>
                            <button type="button" class="btn btn-sm ${user.isSelfEdit ? 'btn-secondary disabled btn-locked' : 'btn-primary'}" 
                                    onclick="${user.isSelfEdit ? 'userManager.showLockedMessage()' : 'userManager.editUser(' + user.id + ')'}" 
                                    title="${user.isSelfEdit ? 'You cannot edit your own account' : 'Edit'}"
                                    ${user.isSelfEdit ? 'disabled' : ''}>
                                <i class="fas ${user.isSelfEdit ? 'fa-lock' : 'fa-edit'}"></i>
                            </button>
                            <button type="button" class="btn btn-sm ${user.isSelfEdit ? 'btn-secondary disabled btn-locked' : 'btn-warning'}" 
                                    onclick="${user.isSelfEdit ? 'userManager.showLockedMessage()' : 'userManager.showChangePasswordModal(' + user.id + ')'}" 
                                    title="${user.isSelfEdit ? 'You cannot change your own password' : 'Change Password'}"
                                    ${user.isSelfEdit ? 'disabled' : ''}>
                                <i class="fas ${user.isSelfEdit ? 'fa-lock' : 'fa-key'}"></i>
                            </button>
                            <button type="button" class="btn btn-sm ${user.isSelfEdit ? 'btn-secondary disabled btn-locked' : (user.isActive ? 'btn-warning' : 'btn-success')}" 
                                    onclick="${user.isSelfEdit ? 'userManager.showLockedMessage()' : 'userManager.toggleStatus(' + user.id + ')'}" 
                                    title="${user.isSelfEdit ? 'You cannot modify your own account' : (user.isActive ? 'Deactivate' : 'Activate')}"
                                    ${user.isSelfEdit ? 'disabled' : ''}>
                                <i class="fas ${user.isSelfEdit ? 'fa-lock' : (user.isActive ? 'fa-pause' : 'fa-play')}"></i>
                            </button>
                            <button type="button" class="btn btn-sm ${user.isSelfEdit ? 'btn-secondary disabled btn-locked' : 'btn-danger'}" 
                                    onclick="${user.isSelfEdit ? 'userManager.showLockedMessage()' : 'userManager.deleteUser(' + user.id + ')'}" 
                                    title="${user.isSelfEdit ? 'You cannot delete your own account' : 'Delete'}"
                                    ${user.isSelfEdit ? 'disabled' : ''}>
                                <i class="fas ${user.isSelfEdit ? 'fa-lock' : 'fa-trash'}"></i>
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

    async viewUser(id) {
        try {
            const response = await fetch(`/api/user/${id}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.renderUserDetails(data.data);
                const modal = new bootstrap.Modal(document.getElementById('viewUserModal'));
                modal.show();
            } else {
                this.showError(data.message || 'Failed to load user details');
            }
        } catch (error) {
            console.error('Error loading user details:', error);
            this.showError('Failed to load user details');
        }
    }

    async editUser(id) {
        try {
            const response = await fetch(`/api/user/${id}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                console.log('Edit user data loaded:', data.data); // Debug log
                this.populateForm(data.data);
                
                // Update modal title and hide password fields for EDIT mode
                document.getElementById('userModalTitle').textContent = 'Edit User';
                document.getElementById('passwordFields').style.display = 'none';
                
                // Remove required attribute from password fields for EDIT mode
                document.getElementById('password').required = false;
                document.getElementById('confirmPassword').required = false;
                
                this.currentUserId = id;
                
                // Check if user is editing themselves
                const currentUserId = this.getCurrentUserId();
                if (currentUserId && parseInt(id) === parseInt(currentUserId)) {
                    // Disable role selection for self-edit
                    const rolesSelect = document.getElementById('roles');
                    if (rolesSelect) {
                        rolesSelect.disabled = true;
                        rolesSelect.title = "You cannot modify your own roles";
                        
                        // Add warning message
                        const warningDiv = document.createElement('div');
                        warningDiv.className = 'alert alert-warning mt-2';
                        warningDiv.innerHTML = '<i class="fas fa-exclamation-triangle"></i> You cannot modify your own roles. Please contact another admin.';
                        
                        const rolesContainer = rolesSelect.closest('.mb-3');
                        if (rolesContainer && !rolesContainer.querySelector('.alert-warning')) {
                            rolesContainer.appendChild(warningDiv);
                        }
                    }
                } else {
                    // Enable role selection for other users
                    const rolesSelect = document.getElementById('roles');
                    if (rolesSelect) {
                        rolesSelect.disabled = false;
                        rolesSelect.title = "";
                        
                        // Remove warning message if exists
                        const rolesContainer = rolesSelect.closest('.mb-3');
                        const warningDiv = rolesContainer?.querySelector('.alert-warning');
                        if (warningDiv) {
                            warningDiv.remove();
                        }
                    }
                }
                
                const modal = new bootstrap.Modal(document.getElementById('userModal'));
                modal.show();
            } else {
                console.error('API Error:', data); // Debug log
                this.showError(data.message || 'Failed to load user data');
            }
        } catch (error) {
            console.error('Error loading user for edit:', error);
            this.showError('Failed to load user data');
        }
    }

    async saveUser() {
        const form = document.getElementById('userForm');
        const formData = new FormData(form);
        
        // Auto-assign WarehouseStaff role
        const selectedRoles = ["WarehouseStaff"];
        
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;
        const userId = formData.get('Id');
        
        // Validate password for create mode (when password fields are visible)
        const passwordFieldsVisible = document.getElementById('passwordFields').style.display !== 'none';
        if (passwordFieldsVisible && !password) {
            this.showError('Password is required for new user');
            return;
        }
        
        // Validate password match if password is provided
        if (password && password !== confirmPassword) {
            this.showError('Passwords do not match');
            return;
        }
        
        // Validate password strength for create mode
        if (passwordFieldsVisible && password && password.length < 8) {
            this.showError('Password must be at least 8 characters long');
            return;
        }
        
        const userData = {
            id: userId || null,
            username: formData.get('Username'),
            email: formData.get('Email'),
            fullName: formData.get('FullName'),
            phone: formData.get('Phone'),
            isActive: formData.get('IsActive') === 'on',
            roles: selectedRoles,
            password: !userId ? password : undefined, // For create
            newPassword: userId && password ? password : undefined // For update (optional)
        };

        try {
            const saveBtn = document.getElementById('saveUserBtn');
            if (!saveBtn) {
                console.error('Save button not found');
                return;
            }
            
            const spinner = saveBtn.querySelector('.spinner-border');
            const icon = saveBtn.querySelector('i');
            const btnText = saveBtn.querySelector('.btn-text');
            
            if (spinner && icon && btnText) {
                saveBtn.disabled = true;
                spinner.classList.remove('d-none');
                icon.classList.add('d-none');
                btnText.textContent = 'Loading...';
            }

            const url = userData.id ? `/api/user/${userData.id}` : '/api/user';
            const method = userData.id ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(userData)
            });

            if (!response.ok) {
                let errorMessage = `HTTP error! status: ${response.status}`;
                try {
                    const errorData = await response.json();
                    console.log('Server error response:', errorData);
                    
                    if (errorData.message) {
                        errorMessage = errorData.message;
                        console.log('Using server message:', errorMessage);
                    }
                    if (errorData.errors) {
                        // Handle ModelState validation errors
                        const validationErrors = [];
                        for (const [key, value] of Object.entries(errorData.errors)) {
                            if (value.errors && Array.isArray(value.errors)) {
                                validationErrors.push(...value.errors.map(e => e.errorMessage || e));
                            }
                        }
                        if (validationErrors.length > 0) {
                            errorMessage = 'Validation errors: ' + validationErrors.join(', ');
                            console.log('Using validation errors:', errorMessage);
                        }
                    }
                } catch (jsonError) {
                    console.warn('Could not parse error response as JSON:', jsonError);
                }
                console.log('Final error message to display:', errorMessage);
                throw new Error(errorMessage);
            }

            const result = await response.json();

            if (result.success) {
                this.showSuccess(result.message);
                this.resetForm();
                this.clearError();
                const modal = bootstrap.Modal.getInstance(document.getElementById('userModal'));
                if (modal) {
                    modal.hide();
                }
                this.loadDashboard();
                this.loadUsers();
            } else {
                this.showFormErrors(result.errors);
            }
        } catch (error) {
            console.error('Error saving user:', error);
            // Tampilkan error yang detail ke user
            const errorMessage = error.message || 'Failed to save user';
            this.showError(errorMessage);
            
            // Jangan tutup modal saat ada error, biarkan user memperbaiki input
            console.log('Error displayed to user:', errorMessage);
        } finally {
            const saveBtn = document.getElementById('saveUserBtn');
            if (saveBtn) {
                const spinner = saveBtn.querySelector('.spinner-border');
                const icon = saveBtn.querySelector('i');
                const btnText = saveBtn.querySelector('.btn-text');
                
                if (spinner && icon && btnText) {
                    saveBtn.disabled = false;
                    spinner.classList.add('d-none');
                    icon.classList.remove('d-none');
                    btnText.textContent = 'Save User';
                }
            }
        }
    }

    async deleteUser(id) {
        try {
            const response = await fetch(`/api/user/${id}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.renderDeleteConfirmation(data.data);
                this.currentUserId = id;
                
                const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
                modal.show();
            } else {
                this.showError(data.message || 'Failed to load user data');
            }
        } catch (error) {
            console.error('Error loading user for delete:', error);
            this.showError('Failed to load user data');
        }
    }

    async confirmDeleteUser() {
        try {
            const deleteBtn = document.getElementById('confirmDeleteBtn');
            const spinner = deleteBtn.querySelector('.spinner-border');
            const icon = deleteBtn.querySelector('i');
            
            deleteBtn.disabled = true;
            if (spinner) spinner.classList.remove('d-none');
            if (icon) icon.classList.add('d-none');

            const response = await fetch(`/api/user/${this.currentUserId}`, {
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
                this.loadUsers();
            } else {
                this.showError(result.message || 'Failed to delete user');
            }
        } catch (error) {
            console.error('Error deleting user:', error);
            this.showError('Failed to delete user');
        } finally {
            const deleteBtn = document.getElementById('confirmDeleteBtn');
            const spinner = deleteBtn.querySelector('.spinner-border');
            const icon = deleteBtn.querySelector('i');
            
            deleteBtn.disabled = false;
            if (spinner) spinner.classList.add('d-none');
            if (icon) icon.classList.remove('d-none');
        }
    }

    async toggleStatus(id) {
        try {
            const response = await fetch(`/api/user/${id}/toggle-status`, {
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
                this.loadUsers();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            console.error('Error toggling status:', error);
            this.showError('Failed to update user status');
        }
    }

    async validateUsername(username) {
        if (!username) return;

        try {
            const excludeId = this.currentUserId || '';
            const response = await fetch(`/api/user/check-username?username=${encodeURIComponent(username)}&excludeId=${excludeId}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            const feedback = document.getElementById('usernameValidation');
            feedback.innerHTML = data.isUnique 
                ? `<i class="fas fa-check text-success"></i> ${data.message}`
                : `<i class="fas fa-times text-danger"></i> ${data.message}`;
        } catch (error) {
            console.error('Error validating username:', error);
        }
    }

    async validateEmail(email) {
        if (!email) return;

        try {
            const excludeId = this.currentUserId || '';
            const response = await fetch(`/api/user/check-email?email=${encodeURIComponent(email)}&excludeId=${excludeId}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            const feedback = document.getElementById('emailValidation');
            feedback.innerHTML = data.isUnique 
                ? `<i class="fas fa-check text-success"></i> ${data.message}`
                : `<i class="fas fa-times text-danger"></i> ${data.message}`;
        } catch (error) {
            console.error('Error validating email:', error);
        }
    }

    populateForm(user) {
        try {
            console.log('Populating form with user data:', user); // Debug log
            
            // Fill basic form fields
            const userIdEl = document.getElementById('userId');
            const usernameEl = document.getElementById('username');
            const emailEl = document.getElementById('email');
            const fullNameEl = document.getElementById('fullName');
            const phoneEl = document.getElementById('phone');
            const isActiveEl = document.getElementById('isActive');
            // Removed rolesEl - auto-assign WarehouseStaff
            const passwordEl = document.getElementById('password');
            const confirmPasswordEl = document.getElementById('confirmPassword');
            const passwordMatchEl = document.getElementById('passwordMatch');
            const passwordRequiredEl = document.getElementById('passwordRequired');
            const confirmPasswordRequiredEl = document.getElementById('confirmPasswordRequired');
            const passwordHintEl = document.getElementById('passwordHint');
            
            if (userIdEl) userIdEl.value = user.id || '';
            if (usernameEl) usernameEl.value = user.username || '';
            if (emailEl) emailEl.value = user.email || '';
            if (fullNameEl) fullNameEl.value = user.fullName || '';
            if (phoneEl) phoneEl.value = user.phone || '';
            if (isActiveEl) isActiveEl.checked = user.isActive || false;
            
            // Auto-assign WarehouseStaff role (no UI needed)
            // Role is automatically assigned in backend
            
            // For edit mode: password is optional
            if (passwordEl) passwordEl.value = '';
            if (confirmPasswordEl) confirmPasswordEl.value = '';
            if (passwordMatchEl) passwordMatchEl.textContent = '';
            if (passwordRequiredEl) passwordRequiredEl.classList.add('d-none');
            if (confirmPasswordRequiredEl) confirmPasswordRequiredEl.classList.add('d-none');
            if (passwordHintEl) passwordHintEl.innerHTML = '<small>Leave blank to keep current password. Minimum 8 characters, at least one uppercase, one lowercase, one number</small>';
            
            console.log('Form populated successfully'); // Debug log
        } catch (error) {
            console.error('Error populating form:', error);
            this.showError('Error loading user data for editing');
        }
    }

    showCreateModal() {
        this.resetForm();
        // Set modal title and show password fields for CREATE mode
        document.getElementById('userModalTitle').textContent = 'Add New User';
        document.getElementById('passwordFields').style.display = 'block';
        
        // Set password fields as required for CREATE mode
        document.getElementById('password').required = true;
        document.getElementById('confirmPassword').required = true;
        
        const modal = new bootstrap.Modal(document.getElementById('userModal'));
        modal.show();
    }

    // Change Password functionality
    showChangePasswordModal(userId) {
        document.getElementById('changePasswordUserId').value = userId;
        document.getElementById('changePasswordForm').reset();
        this.clearPasswordError();
        
        const modal = new bootstrap.Modal(document.getElementById('changePasswordModal'));
        modal.show();
    }

    async changePassword() {
        const userId = document.getElementById('changePasswordUserId').value;
        const newPassword = document.getElementById('newPassword').value;
        const confirmPassword = document.getElementById('confirmNewPassword').value;

        if (!newPassword || !confirmPassword) {
            this.showPasswordError('Please fill in all password fields');
            return;
        }

        if (newPassword !== confirmPassword) {
            this.showPasswordError('Passwords do not match');
            return;
        }

        try {
            const response = await fetch(`/api/user/${userId}/change-password`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({
                    newPassword: newPassword
                })
            });

            if (!response.ok) {
                let errorMessage = `HTTP error! status: ${response.status}`;
                try {
                    const errorData = await response.json();
                    if (errorData.message) {
                        errorMessage = errorData.message;
                    }
                } catch (jsonError) {
                    console.warn('Could not parse error response as JSON:', jsonError);
                }
                throw new Error(errorMessage);
            }

            const result = await response.json();

            if (result.success) {
                this.showPasswordSuccess(result.message);
                setTimeout(() => {
                    const modal = bootstrap.Modal.getInstance(document.getElementById('changePasswordModal'));
                    if (modal) {
                        modal.hide();
                    }
                }, 1500);
            } else {
                this.showPasswordError(result.message || 'Failed to change password');
            }
        } catch (error) {
            console.error('Error changing password:', error);
            this.showPasswordError(error.message || 'Failed to change password');
        }
    }

    showPasswordError(message) {
        const errorElement = document.getElementById('passwordErrorMessage');
        if (errorElement) {
            errorElement.innerHTML = `<i class="fas fa-exclamation-triangle me-2"></i>${message}`;
            errorElement.classList.remove('d-none');
            errorElement.style.display = 'block';
        }
        console.error('Password Error:', message);
    }

    showPasswordSuccess(message) {
        const successElement = document.getElementById('passwordErrorMessage');
        if (successElement) {
            successElement.innerHTML = `<i class="fas fa-check-circle me-2"></i>${message}`;
            successElement.classList.remove('d-none');
            successElement.classList.remove('alert-danger');
            successElement.classList.add('alert-success');
            successElement.style.display = 'block';
        }
        console.log('Password Success:', message);
    }

    clearPasswordError() {
        const errorElement = document.getElementById('passwordErrorMessage');
        if (errorElement) {
            errorElement.classList.add('d-none');
            errorElement.classList.remove('alert-success');
            errorElement.classList.add('alert-danger');
            errorElement.innerHTML = '';
            errorElement.style.display = 'none';
        }
    }

    resetForm() {
        const userFormEl = document.getElementById('userForm');
        const userIdEl = document.getElementById('userId');
        const usernameValidationEl = document.getElementById('usernameValidation');
        const emailValidationEl = document.getElementById('emailValidation');
        const modalTitleEl = document.getElementById('userModalTitle');
        const passwordEl = document.getElementById('password');
        const confirmPasswordEl = document.getElementById('confirmPassword');
        const passwordMatchEl = document.getElementById('passwordMatch');
        const passwordRequiredEl = document.getElementById('passwordRequired');
        const confirmPasswordRequiredEl = document.getElementById('confirmPasswordRequired');
        const passwordHintEl = document.getElementById('passwordHint');
        
        if (userFormEl) userFormEl.reset();
        if (userIdEl) userIdEl.value = '';
        if (usernameValidationEl) usernameValidationEl.innerHTML = '';
        if (emailValidationEl) emailValidationEl.innerHTML = '';
        if (modalTitleEl) modalTitleEl.textContent = 'Add New User';
        
        // Clear error and success messages
        this.clearError();
        this.clearSuccess();
        
        // Reset password fields
        if (passwordEl) passwordEl.value = '';
        if (confirmPasswordEl) confirmPasswordEl.value = '';
        if (passwordMatchEl) passwordMatchEl.textContent = '';
        
        // For create mode: password is required
        if (passwordRequiredEl) passwordRequiredEl.classList.remove('d-none');
        if (confirmPasswordRequiredEl) confirmPasswordRequiredEl.classList.remove('d-none');
        if (passwordHintEl) passwordHintEl.innerHTML = '<small>Minimum 8 characters, at least one uppercase, one lowercase, one number</small>';
        
        // Setup password match validation
        if (passwordEl && confirmPasswordEl && passwordMatchEl) {
            const validatePasswordMatch = () => {
                if (confirmPasswordEl.value && passwordEl.value !== confirmPasswordEl.value) {
                    passwordMatchEl.innerHTML = '<span class="text-danger"><i class="fas fa-times"></i> Passwords do not match</span>';
                } else if (confirmPasswordEl.value && passwordEl.value === confirmPasswordEl.value) {
                    passwordMatchEl.innerHTML = '<span class="text-success"><i class="fas fa-check"></i> Passwords match</span>';
                } else {
                    passwordMatchEl.textContent = '';
                }
            };
            
            passwordEl.addEventListener('input', validatePasswordMatch);
            confirmPasswordEl.addEventListener('input', validatePasswordMatch);
        }
        
        this.currentUserId = null;
    }

    renderUserDetails(user) {
        const detailsDiv = document.getElementById('userDetailsContainer');
        detailsDiv.innerHTML = `
            <div class="row">
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header">
                            <h6 class="mb-0"><i class="fas fa-user me-2"></i>User Information</h6>
                        </div>
                        <div class="card-body">
                            <dl class="row">
                                <dt class="col-sm-4">Username:</dt>
                                <dd class="col-sm-8"><span class="badge bg-primary">${user.username}</span></dd>
                                
                                <dt class="col-sm-4">Full Name:</dt>
                                <dd class="col-sm-8">${user.fullName}</dd>
                                
                                <dt class="col-sm-4">Email:</dt>
                                <dd class="col-sm-8">${user.email}</dd>
                                
                                <dt class="col-sm-4">Phone:</dt>
                                <dd class="col-sm-8">${user.phone || 'Not provided'}</dd>
                                
                                <dt class="col-sm-4">Status:</dt>
                                <dd class="col-sm-8">
                                    <span class="badge ${user.isActive ? 'bg-success' : 'bg-secondary'}">${user.isActive ? 'Active' : 'Inactive'}</span>
                                </dd>
                                
                                <dt class="col-sm-4">Roles:</dt>
                                <dd class="col-sm-8">
                                    <div class="d-flex flex-wrap gap-1">
                                        ${user.roleNames.map(role => `
                                            <span class="badge ${this.getRoleBadgeClass(role)}">${role}</span>
                                        `).join('')}
                                    </div>
                                </dd>
                                
                                <dt class="col-sm-4">Created:</dt>
                                <dd class="col-sm-8">${this.formatDateTime(user.createdDate)}</dd>
                                
                                ${user.modifiedDate ? `
                                <dt class="col-sm-4">Modified:</dt>
                                <dd class="col-sm-8">${this.formatDateTime(user.modifiedDate)}</dd>
                                ` : ''}
                            </dl>
                        </div>
                    </div>
                </div>
                
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header">
                            <h6 class="mb-0"><i class="fas fa-chart-line me-2"></i>Activity Overview</h6>
                        </div>
                        <div class="card-body">
                            <div class="mb-4">
                                <div class="d-flex justify-content-between mb-2">
                                    <span class="fw-bold">Last Login</span>
                                    <span class="fw-bold">${user.lastLoginDate ? this.formatDateTime(user.lastLoginDate) : 'Never'}</span>
                                </div>
                                <div class="progress" style="height: 8px;">
                                    <div class="progress-bar ${user.lastLoginDate ? 'bg-success' : 'bg-secondary'}" 
                                         style="width: ${user.lastLoginDate ? '100' : '0'}%">
                                    </div>
                                </div>
                            </div>

                            <div class="row text-center">
                                <div class="col-6">
                                    <div class="border rounded p-3">
                                        <h5 class="text-primary mb-1">${user.isActive ? 'Active' : 'Inactive'}</h5>
                                        <small class="text-muted">Status</small>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <div class="border rounded p-3">
                                        <h5 class="text-info mb-1">${user.roleNames.length}</h5>
                                        <small class="text-muted">Roles</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderDeleteConfirmation(user) {
        const infoDiv = document.getElementById('deleteUserInfo');
        
        infoDiv.innerHTML = `
            <div class="card">
                <div class="card-body">
                    <h6 class="card-title">${user.username} - ${user.fullName}</h6>
                    <p class="card-text small text-muted">
                        Email: ${user.email}
                    </p>
                    <div class="alert alert-warning">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        <strong>Warning:</strong> This action cannot be undone. The user will be permanently removed from the system.
                    </div>
                </div>
            </div>
        `;
    }

    showFormErrors(errors) {
        const errorElement = document.getElementById('errorMessage');
        if (errorElement) {
            let errorMessage = '';
            if (typeof errors === 'object' && errors !== null) {
                if (Array.isArray(errors)) {
                    errorMessage = errors.join(', ');
                } else {
                    errorMessage = Object.values(errors).join(', ');
                }
            } else {
                errorMessage = errors || 'Validation error occurred';
            }
            
            errorElement.innerHTML = `<i class="fas fa-exclamation-triangle me-2"></i>${errorMessage}`;
            errorElement.classList.remove('d-none');
        }
        
        console.error('Form validation errors:', errors);
    }

    showSuccess(message) {
        const successElement = document.getElementById('successMessage');
        if (successElement) {
            successElement.innerHTML = `<i class="fas fa-check-circle me-2"></i>${message}`;
            successElement.classList.remove('d-none');
        }
        
        // Show success toast
        if (this.successToast) {
            this.successToast.show();
        }
        
        console.log('Success:', message);
    }

    showError(message) {
        // Retry mechanism untuk menemukan element
        const findErrorElement = () => {
            let errorElement = document.getElementById('errorMessage');
            if (!errorElement) {
                // Coba cari di dalam modal
                const modal = document.getElementById('userModal');
                if (modal) {
                    errorElement = modal.querySelector('#errorMessage');
                }
            }
            return errorElement;
        };
        
        let errorElement = findErrorElement();
        
        if (errorElement) {
            errorElement.innerHTML = `<i class="fas fa-exclamation-triangle me-2"></i>${message}`;
            errorElement.classList.remove('d-none');
            errorElement.style.display = 'block';
            console.log('Error element found and shown with message:', message);
        } else {
            console.error('Error element not found! Trying alternative approach...');
            
            // Fallback: coba cari dengan querySelector
            errorElement = document.querySelector('#errorMessage');
            if (errorElement) {
                errorElement.innerHTML = `<i class="fas fa-exclamation-triangle me-2"></i>${message}`;
                errorElement.classList.remove('d-none');
                errorElement.style.display = 'block';
                console.log('Error element found with querySelector:', message);
            } else {
                console.error('Error element still not found! Showing alert as fallback.');
                // Last resort: show alert
                alert('Error: ' + message);
            }
        }
        
        console.error('Error:', message);
        
        if (this.errorToast) {
            this.errorToast.show();
        }
    }

    clearError() {
        let errorElement = document.getElementById('errorMessage');
        if (!errorElement) {
            errorElement = document.querySelector('#errorMessage');
        }
        
        if (errorElement) {
            errorElement.classList.add('d-none');
            errorElement.innerHTML = '';
            errorElement.style.display = 'none';
        }
        
        if (this.errorToast) {
            this.errorToast.hide();
        }
    }

    clearSuccess() {
        const successElement = document.getElementById('successMessage');
        if (successElement) {
            successElement.classList.add('d-none');
            successElement.innerHTML = '';
        }
        
        if (this.successToast) {
            this.successToast.hide();
        }
    }

    getRoleBadgeClass(role) {
        switch (role) {
            case 'Admin':
                return 'bg-danger';
            case 'WarehouseStaff':
                return 'bg-info';
            default:
                return 'bg-secondary';
        }
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


    async changeOwnPassword() {
        const currentPassword = document.getElementById('currentPassword').value;
        const newPasswordOwn = document.getElementById('newPasswordOwn').value;
        const confirmNewPassword = document.getElementById('confirmNewPassword').value;
        
        // Validate inputs
        if (!currentPassword || !newPasswordOwn || !confirmNewPassword) {
            this.showError('All fields are required');
            return;
        }
        
        if (newPasswordOwn !== confirmNewPassword) {
            this.showError('New passwords do not match');
            return;
        }
        
        try {
            const btn = document.getElementById('changePasswordBtn');
            const spinner = btn.querySelector('.spinner-border');
            const icon = btn.querySelector('i');
            
            btn.disabled = true;
            spinner.classList.remove('d-none');
            icon.classList.add('d-none');
            
            const response = await fetch('/api/user/change-password', {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    currentPassword: currentPassword,
                    newPassword: newPasswordOwn
                })
            });
            
            const data = await response.json();
            
            if (data.success) {
                this.showSuccess(data.message || 'Password changed successfully');
                
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('changePasswordModal'));
                modal.hide();
                
                // Redirect to logout after 2 seconds
                if (data.requireLogout) {
                    setTimeout(() => {
                        window.location.href = '/Account/Logout';
                    }, 2000);
                }
            } else {
                this.showError(data.message || 'Failed to change password');
            }
            
            btn.disabled = false;
            spinner.classList.add('d-none');
            icon.classList.remove('d-none');
            
        } catch (error) {
            console.error('Error changing password:', error);
            this.showError('Failed to change password');
            
            const btn = document.getElementById('changePasswordBtn');
            const spinner = btn.querySelector('.spinner-border');
            const icon = btn.querySelector('i');
            btn.disabled = false;
            spinner.classList.add('d-none');
            icon.classList.remove('d-none');
        }
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
            this.loadUsers();
        }, 30000);
    }
}

// Global functions for onclick handlers
function openCreateModal() {
    userManager.resetForm();
    const modal = new bootstrap.Modal(document.getElementById('userModal'));
    modal.show();
}

function saveUser() {
    userManager.saveUser();
}

function editUserFromDetails() {
    bootstrap.Modal.getInstance(document.getElementById('viewUserModal')).hide();
    userManager.editUser(userManager.currentUserId);
}

function confirmDeleteUser() {
    userManager.confirmDeleteUser();
}

// Initialize when DOM is loaded
let userManager;
document.addEventListener('DOMContentLoaded', function() {
    try {
        // Check if Bootstrap is available
        if (typeof bootstrap === 'undefined') {
            console.error('Bootstrap is not loaded. Please ensure Bootstrap is included before this script.');
            return;
        }
        
        // Check if required elements exist
        const requiredElements = [
            'statusFilter', 'roleFilter', 'searchInput', 'pageSizeSelect', 
            'usersTableContainer', 'showingStart', 'showingEnd', 'totalRecords',
            'currentPageNum', 'totalPagesNum', 'prevPageBtn', 'nextPageBtn'
        ];
        
        const missingElements = requiredElements.filter(id => !document.getElementById(id));
        if (missingElements.length > 0) {
            console.error('Missing required elements:', missingElements);
            return;
        }
        
        userManager = new UserManager();
        console.log('UserManager initialized successfully');
    } catch (error) {
        console.error('Error initializing UserManager:', error);
    }
});
