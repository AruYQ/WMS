/**
 * Company Manager - AJAX-based Company Management (SuperAdmin)
 * Manages company creation with auto-admin user creation
 */
class CompanyManager {
    constructor() {
        this.companies = [];
        this.init();
    }

    async init() {
        console.log('CompanyManager: Initializing...');
        await this.loadCompanies();
    }

    async loadCompanies() {
        try {
            const response = await fetch('/api/company');
            const result = await response.json();

            if (result.success) {
                this.companies = result.data;
                this.renderCompaniesTable();
            } else {
                this.showToast('Error loading companies', 'error');
            }
        } catch (error) {
            console.error('Error loading companies:', error);
            this.showToast('Error loading companies', 'error');
        }
    }

    renderCompaniesTable() {
        const container = document.getElementById('companiesTableContainer');
        if (!container) return;

        if (this.companies.length === 0) {
            container.innerHTML = '<div class="text-center py-5"><p>No companies found</p></div>';
            return;
        }

        let html = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>Code</th>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Users</th>
                            <th>Status</th>
                            <th>Created</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        this.companies.forEach(company => {
            const statusBadge = company.isActive 
                ? '<span class="badge bg-success">Active</span>'
                : '<span class="badge bg-secondary">Inactive</span>';

            html += `
                <tr>
                    <td><strong>${company.code}</strong></td>
                    <td>${company.name}</td>
                    <td>${company.email}</td>
                    <td>${company.totalUsers}</td>
                    <td>${statusBadge}</td>
                    <td>${new Date(company.createdDate).toLocaleDateString()}</td>
                    <td>
                        <button class="btn btn-sm btn-info me-1" onclick="companyManager.viewCompany(${company.id})">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button class="btn btn-sm btn-warning me-1" onclick="companyManager.editCompany(${company.id})">
                            <i class="fas fa-edit"></i>
                        </button>
                        ${company.isActive 
                            ? `<button class="btn btn-sm btn-danger" onclick="companyManager.deactivateCompany(${company.id})">
                                <i class="fas fa-ban"></i>
                               </button>`
                            : `<button class="btn btn-sm btn-success" onclick="companyManager.activateCompany(${company.id})">
                                <i class="fas fa-check"></i>
                               </button>`
                        }
                    </td>
                </tr>
            `;
        });

        html += '</tbody></table></div>';
        container.innerHTML = html;
    }

    async createCompany() {
        const form = document.getElementById('createCompanyForm');
        const formData = new FormData(form);

        const data = {
            code: formData.get('code'),
            name: formData.get('name'),
            email: formData.get('email'),
            phone: formData.get('phone'),
            address: formData.get('address'),
            adminFullName: formData.get('adminFullName'),
            adminEmail: formData.get('adminEmail')
        };

        try {
            const response = await fetch('/api/company', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            const result = await response.json();

            if (result.success) {
                this.showToast(result.message, 'success');
                bootstrap.Modal.getInstance(document.getElementById('createCompanyModal')).hide();
                form.reset();
                await this.loadCompanies();
            } else {
                this.showToast(result.message, 'error');
            }
        } catch (error) {
            console.error('Error creating company:', error);
            this.showToast('Error creating company', 'error');
        }
    }

    async viewCompany(id) {
        try {
            const response = await fetch(`/api/company/${id}`);
            const result = await response.json();

            if (result.success) {
                const company = result.data;
                const container = document.getElementById('companyDetailsContainer');
                container.innerHTML = `
                    <div class="row">
                        <div class="col-md-6">
                            <p><strong>Code:</strong> ${company.code}</p>
                            <p><strong>Name:</strong> ${company.name}</p>
                            <p><strong>Email:</strong> ${company.email}</p>
                            <p><strong>Phone:</strong> ${company.phone || '-'}</p>
                        </div>
                        <div class="col-md-6">
                            <p><strong>Users:</strong> ${company.currentUsers} / ${company.maxUsers}</p>
                            <p><strong>Status:</strong> ${company.isActive ? 'Active' : 'Inactive'}</p>
                            <p><strong>Created:</strong> ${new Date(company.createdDate).toLocaleString()}</p>
                        </div>
                    </div>
                    <hr>
                    <h6>Statistics</h6>
                    <div class="row">
                        <div class="col-md-3"><p>Items: ${company.totalItems}</p></div>
                        <div class="col-md-3"><p>Locations: ${company.totalLocations}</p></div>
                        <div class="col-md-3"><p>Purchase Orders: ${company.totalPurchaseOrders}</p></div>
                        <div class="col-md-3"><p>Sales Orders: ${company.totalSalesOrders}</p></div>
                    </div>
                `;
                new bootstrap.Modal(document.getElementById('viewCompanyModal')).show();
            }
        } catch (error) {
            console.error('Error viewing company:', error);
            this.showToast('Error loading company details', 'error');
        }
    }

    async editCompany(id) {
        try {
            const response = await fetch(`/api/company/${id}`);
            const result = await response.json();

            if (result.success) {
                const company = result.data;
                document.getElementById('editCompanyId').value = company.id;
                document.getElementById('editCompanyCode').value = company.code;
                document.getElementById('editCompanyName').value = company.name;
                document.getElementById('editCompanyEmail').value = company.email;
                document.getElementById('editCompanyPhone').value = company.phone || '';
                document.getElementById('editCompanyAddress').value = company.address || '';
                
                new bootstrap.Modal(document.getElementById('editCompanyModal')).show();
            }
        } catch (error) {
            console.error('Error loading company for edit:', error);
            this.showToast('Error loading company details', 'error');
        }
    }

    async updateCompany() {
        const form = document.getElementById('editCompanyForm');
        const formData = new FormData(form);

        const data = {
            id: parseInt(formData.get('id')),
            code: formData.get('code'),
            name: formData.get('name'),
            email: formData.get('email'),
            phone: formData.get('phone'),
            address: formData.get('address')
        };

        try {
            const response = await fetch(`/api/company/${data.id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            const result = await response.json();

            if (result.success) {
                this.showToast(result.message, 'success');
                bootstrap.Modal.getInstance(document.getElementById('editCompanyModal')).hide();
                await this.loadCompanies();
            } else {
                this.showToast(result.message, 'error');
            }
        } catch (error) {
            console.error('Error updating company:', error);
            this.showToast('Error updating company', 'error');
        }
    }

    async deactivateCompany(id) {
        if (!confirm('Are you sure you want to deactivate this company?')) return;

        try {
            const response = await fetch(`/api/company/${id}`, { method: 'DELETE' });
            const result = await response.json();

            if (result.success) {
                this.showToast('Company deactivated', 'success');
                await this.loadCompanies();
            } else {
                this.showToast(result.message, 'error');
            }
        } catch (error) {
            console.error('Error deactivating company:', error);
            this.showToast('Error deactivating company', 'error');
        }
    }

    async activateCompany(id) {
        try {
            const response = await fetch(`/api/company/${id}/activate`, { method: 'POST' });
            const result = await response.json();

            if (result.success) {
                this.showToast('Company activated', 'success');
                await this.loadCompanies();
            } else {
                this.showToast(result.message, 'error');
            }
        } catch (error) {
            console.error('Error activating company:', error);
            this.showToast('Error activating company', 'error');
        }
    }

    showToast(message, type = 'info') {
        // Simple toast implementation
        alert(message);
    }
}

