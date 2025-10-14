/**
 * Item Supplier Manager - JavaScript untuk handle supplier selection
 * Handles both dropdown list dan advanced search untuk Item form
 */

class ItemSupplierManager {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalPages = 0;
        this.totalCount = 0;
        this.currentSearchRequest = null;
        
        this.init();
    }

    init() {
        this.bindEvents();
    }

    bindEvents() {
        // Supplier dropdown search
        document.getElementById('supplierDropdownSearch')?.addEventListener('input', (e) => {
            this.searchSuppliersDropdown(e.target.value);
        });

        // Enter key untuk advanced search
        document.getElementById('supplierAdvancedSearchForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.searchSuppliersAdvanced();
        });
    }

    // ===== DROPDOWN FUNCTIONS =====

    async openSupplierDropdown() {
        try {
            const modal = new bootstrap.Modal(document.getElementById('supplierDropdownModal'));
            modal.show();
            
            // Load all suppliers
            await this.loadAllSuppliers();
        } catch (error) {
            console.error('Error opening supplier dropdown:', error);
            this.showError('Failed to open supplier list');
        }
    }

    async loadAllSuppliers() {
        try {
            const response = await fetch('/Item/GetSuppliers');
            const data = await response.json();
            
            if (data.success) {
                this.renderSupplierDropdownList(data.data);
            } else {
                this.showError(data.message || 'Failed to load suppliers');
            }
        } catch (error) {
            console.error('Error loading suppliers:', error);
            this.showError('Failed to load suppliers');
        }
    }

    async searchSuppliersDropdown(searchTerm) {
        try {
            const response = await fetch(`/Item/GetSuppliers?search=${encodeURIComponent(searchTerm)}&limit=50`);
            const data = await response.json();
            
            if (data.success) {
                this.renderSupplierDropdownList(data.data);
            } else {
                this.showError(data.message || 'Failed to search suppliers');
            }
        } catch (error) {
            console.error('Error searching suppliers:', error);
            this.showError('Failed to search suppliers');
        }
    }

    renderSupplierDropdownList(suppliers) {
        const container = document.getElementById('supplierDropdownList');
        if (!container) return;

        if (suppliers.length === 0) {
            container.innerHTML = '<div class="text-center text-muted py-3">No suppliers found</div>';
            return;
        }

        container.innerHTML = suppliers.map(supplier => `
            <div class="list-group-item list-group-item-action" onclick="itemSupplierManager.selectSupplier(${supplier.id}, '${supplier.name}')">
                <div class="d-flex w-100 justify-content-between">
                    <h6 class="mb-1">${supplier.name}</h6>
                    <small class="text-muted">${supplier.email}</small>
                </div>
                <p class="mb-1">${supplier.phone || 'No phone'} | ${supplier.address || 'No address'}</p>
            </div>
        `).join('');
    }

    selectSupplier(id, name) {
        // Set values
        document.getElementById('supplierId').value = id;
        document.getElementById('supplierName').value = name;
        
        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('supplierDropdownModal'));
        modal.hide();
        
        // Show success message
        this.showSuccess(`Supplier "${name}" selected`);
    }

    // ===== ADVANCED SEARCH FUNCTIONS =====

    async openSupplierAdvancedSearch() {
        try {
            const modal = new bootstrap.Modal(document.getElementById('supplierAdvancedSearchModal'));
            modal.show();
            
            // Clear previous search
            this.clearAdvancedSearch();
        } catch (error) {
            console.error('Error opening advanced search:', error);
            this.showError('Failed to open advanced search');
        }
    }

    async searchSuppliersAdvanced() {
        try {
            // Get search criteria
            const searchRequest = {
                name: document.getElementById('searchName').value.trim() || null,
                email: document.getElementById('searchEmail').value.trim() || null,
                phone: document.getElementById('searchPhone').value.trim() || null,
                city: document.getElementById('searchCity').value.trim() || null,
                contactPerson: document.getElementById('searchContactPerson').value.trim() || null,
                createdDateFrom: document.getElementById('searchCreatedDateFrom').value || null,
                createdDateTo: document.getElementById('searchCreatedDateTo').value || null,
                page: 1,
                pageSize: 10
            };

            console.log('Search request before processing:', searchRequest);

            // Convert date strings to ISO string format (not Date objects)
            if (searchRequest.createdDateFrom) {
                const date = new Date(searchRequest.createdDateFrom);
                searchRequest.createdDateFrom = date.toISOString();
            }
            if (searchRequest.createdDateTo) {
                const date = new Date(searchRequest.createdDateTo);
                searchRequest.createdDateTo = date.toISOString();
            }

            console.log('Search request after date conversion:', searchRequest);

            this.currentSearchRequest = searchRequest;
            this.currentPage = 1;

            console.log('Making request to /api/item/suppliers/advanced-search');
            
            const response = await fetch('/api/item/suppliers/advanced-search', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(searchRequest)
            });

            console.log('Response status:', response.status);
            console.log('Response ok:', response.ok);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('HTTP Error Response:', errorText);
                throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
            }

            const data = await response.json();
            console.log('Response data:', data);
            
            if (data.success) {
                this.renderAdvancedSearchResults(data);
            } else {
                this.showError(data.message || 'Search failed');
            }
        } catch (error) {
            console.error('Error in advanced search:', error);
            this.showError(`Error performing advanced search: ${error.message}`);
        }
    }

    async loadAdvancedSearchPage(page) {
        if (!this.currentSearchRequest) return;

        try {
            this.currentSearchRequest.page = page;
            this.currentPage = page;

            console.log('Loading advanced search page:', page, 'Request:', this.currentSearchRequest);

            const response = await fetch('/api/item/suppliers/advanced-search', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(this.currentSearchRequest)
            });

            console.log('Pagination response status:', response.status);
            console.log('Pagination response ok:', response.ok);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Pagination HTTP Error Response:', errorText);
                throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
            }

            const data = await response.json();
            console.log('Pagination response data:', data);
            
            if (data.success) {
                this.renderAdvancedSearchResults(data);
            } else {
                this.showError(data.message || 'Failed to load page');
            }
        } catch (error) {
            console.error('Error loading page:', error);
            this.showError(`Failed to load page: ${error.message}`);
        }
    }

    renderAdvancedSearchResults(data) {
        const resultsDiv = document.getElementById('supplierSearchResults');
        const tbody = document.getElementById('supplierSearchResultsBody');
        const pagination = document.getElementById('supplierSearchPagination');

        if (!resultsDiv || !tbody || !pagination) return;

        // Show results
        resultsDiv.style.display = 'block';

        // Update data
        this.totalPages = data.totalPages;
        this.totalCount = data.totalCount;
        this.currentPage = data.currentPage;

        // Render table
        if (data.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-3">No suppliers found matching your criteria</td></tr>';
        } else {
            tbody.innerHTML = data.data.map(supplier => `
                <tr>
                    <td>${supplier.name}</td>
                    <td>${supplier.email}</td>
                    <td>${supplier.phone || '-'}</td>
                    <td>${supplier.city || '-'}</td>
                    <td>${supplier.contactPerson || '-'}</td>
                    <td>${this.formatDate(supplier.createdDate)}</td>
                    <td>
                        <button type="button" class="btn btn-sm btn-primary" 
                                onclick="itemSupplierManager.selectSupplierFromAdvanced(${supplier.id}, '${supplier.name}')">
                            Select
                        </button>
                    </td>
                </tr>
            `).join('');
        }

        // Render pagination
        this.renderPagination(pagination);
    }

    renderPagination(container) {
        if (this.totalPages <= 1) {
            container.innerHTML = '';
            return;
        }

        let paginationHTML = '<ul class="pagination pagination-sm justify-content-center">';
        
        // Previous button
        if (this.currentPage > 1) {
            paginationHTML += `<li class="page-item">
                <button class="page-link" onclick="itemSupplierManager.loadAdvancedSearchPage(${this.currentPage - 1})">Previous</button>
            </li>`;
        }

        // Page numbers
        const startPage = Math.max(1, this.currentPage - 2);
        const endPage = Math.min(this.totalPages, this.currentPage + 2);

        for (let i = startPage; i <= endPage; i++) {
            paginationHTML += `<li class="page-item ${i === this.currentPage ? 'active' : ''}">
                <button class="page-link" onclick="itemSupplierManager.loadAdvancedSearchPage(${i})">${i}</button>
            </li>`;
        }

        // Next button
        if (this.currentPage < this.totalPages) {
            paginationHTML += `<li class="page-item">
                <button class="page-link" onclick="itemSupplierManager.loadAdvancedSearchPage(${this.currentPage + 1})">Next</button>
            </li>`;
        }

        paginationHTML += '</ul>';
        container.innerHTML = paginationHTML;
    }

    selectSupplierFromAdvanced(id, name) {
        // Set values
        document.getElementById('supplierId').value = id;
        document.getElementById('supplierName').value = name;
        
        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('supplierAdvancedSearchModal'));
        modal.hide();
        
        // Show success message
        this.showSuccess(`Supplier "${name}" selected`);
    }

    clearAdvancedSearch() {
        // Clear form
        document.getElementById('supplierAdvancedSearchForm').reset();
        
        // Hide results
        const resultsDiv = document.getElementById('supplierSearchResults');
        if (resultsDiv) {
            resultsDiv.style.display = 'none';
        }
        
        // Reset data
        this.currentSearchRequest = null;
        this.currentPage = 1;
        this.totalPages = 0;
        this.totalCount = 0;
    }

    // ===== UTILITY FUNCTIONS =====

    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
        });
    }

    showSuccess(message) {
        // You can implement toast notification here
        console.log('Success:', message);
    }

    showError(message) {
        // You can implement toast notification here
        console.error('Error:', message);
        alert(message); // Fallback to alert
    }
}

// Global functions untuk onclick handlers
function openSupplierAdvancedSearch() {
    if (window.itemSupplierManager) {
        window.itemSupplierManager.openSupplierAdvancedSearch();
    }
}

function openSupplierDropdown() {
    if (window.itemSupplierManager) {
        window.itemSupplierManager.openSupplierDropdown();
    }
}

function searchSuppliersAdvanced() {
    if (window.itemSupplierManager) {
        window.itemSupplierManager.searchSuppliersAdvanced();
    }
}

function clearAdvancedSearch() {
    if (window.itemSupplierManager) {
        window.itemSupplierManager.clearAdvancedSearch();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.itemSupplierManager = new ItemSupplierManager();
});
