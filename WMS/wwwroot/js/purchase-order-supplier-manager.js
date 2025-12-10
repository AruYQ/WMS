/**
 * Purchase Order Supplier Manager - JavaScript untuk handle supplier selection
 * Handles advanced search untuk Purchase Order supplier selection
 */

class PurchaseOrderSupplierManager {
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
        // Enter key untuk advanced search
        document.getElementById('poSupplierAdvancedSearchForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.searchSuppliersAdvanced();
        });
        
        // Cleanup backdrop saat modal ditutup
        document.getElementById('poSupplierAdvancedSearchModal')?.addEventListener('hidden.bs.modal', () => {
            this.cleanupBackdrop();
        });
    }

    /**
     * Cleanup saat modal ditutup (tanpa backdrop)
     */
    cleanupBackdrop() {
        try {
            // Karena menggunakan backdrop: false, tidak perlu cleanup backdrop
            // Tapi tetap cleanup untuk memastikan tidak ada state yang tersisa
            
            // Hanya cleanup jika ada modal-backdrop yang tidak seharusnya ada
            const backdrops = document.querySelectorAll('.modal-backdrop');
            if (backdrops.length > 0) {
                // Hanya remove backdrop yang terkait dengan advanced search modal
                backdrops.forEach(backdrop => {
                    if (backdrop.classList.contains('advanced-search-backdrop')) {
                        backdrop.remove();
                    }
                });
            }
            
            console.log('Purchase Order Supplier: Advanced search modal closed (no backdrop cleanup needed)');
        } catch (error) {
            console.error('Purchase Order Supplier: Error during modal cleanup:', error);
        }
    }

    // ===== ADVANCED SEARCH FUNCTIONS =====

    async openSupplierAdvancedSearch() {
        try {
            const modal = new bootstrap.Modal(document.getElementById('poSupplierAdvancedSearchModal'), {
                backdrop: false,     // Tidak ada backdrop untuk advanced search
                keyboard: true,      // Bisa ditutup dengan ESC
                focus: true          // Focus ke modal saat dibuka
            });
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
                name: document.getElementById('poSearchName').value.trim() || null,
                email: document.getElementById('poSearchEmail').value.trim() || null,
                phone: document.getElementById('poSearchPhone').value.trim() || null,
                city: document.getElementById('poSearchCity').value.trim() || null,
                contactPerson: document.getElementById('poSearchContactPerson').value.trim() || null,
                createdDateFrom: document.getElementById('poSearchCreatedDateFrom').value || null,
                createdDateTo: document.getElementById('poSearchCreatedDateTo').value || null,
                page: 1,
                pageSize: 10
            };

            console.log('Purchase Order Supplier search request before processing:', searchRequest);

            // Convert date strings to ISO string format (not Date objects)
            if (searchRequest.createdDateFrom) {
                const date = new Date(searchRequest.createdDateFrom);
                searchRequest.createdDateFrom = date.toISOString();
            }
            if (searchRequest.createdDateTo) {
                const date = new Date(searchRequest.createdDateTo);
                searchRequest.createdDateTo = date.toISOString();
            }

            console.log('Purchase Order Supplier search request after date conversion:', searchRequest);

            this.currentSearchRequest = searchRequest;
            this.currentPage = 1;

            console.log('Making request to /api/purchaseorder/suppliers/advanced-search');
            
            const response = await fetch('/api/purchaseorder/suppliers/advanced-search', {
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

            const response = await fetch('/api/purchaseorder/suppliers/advanced-search', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(this.currentSearchRequest)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            
            if (data.success) {
                this.renderAdvancedSearchResults(data);
            } else {
                this.showError(data.message || 'Search failed');
            }
        } catch (error) {
            console.error('Error loading page:', error);
            this.showError(`Error loading page: ${error.message}`);
        }
    }

    renderAdvancedSearchResults(data) {
        const resultsDiv = document.getElementById('poSupplierSearchResults');
        const tbody = document.getElementById('poSupplierSearchResultsBody');
        const pagination = document.getElementById('poSupplierSearchPagination');

        if (!resultsDiv || !tbody || !pagination) {
            console.error('Required elements not found for rendering results');
            return;
        }

        // Show results section
        resultsDiv.style.display = 'block';

        // Update pagination info
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
                                onclick="poSupplierManager.selectSupplierFromAdvanced(${supplier.id}, '${supplier.name}')">
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
                <button class="page-link" onclick="poSupplierManager.loadAdvancedSearchPage(${this.currentPage - 1})">Previous</button>
            </li>`;
        }

        // Page numbers
        const startPage = Math.max(1, this.currentPage - 2);
        const endPage = Math.min(this.totalPages, this.currentPage + 2);

        for (let i = startPage; i <= endPage; i++) {
            paginationHTML += `<li class="page-item ${i === this.currentPage ? 'active' : ''}">
                <button class="page-link" onclick="poSupplierManager.loadAdvancedSearchPage(${i})">${i}</button>
            </li>`;
        }

        // Next button
        if (this.currentPage < this.totalPages) {
            paginationHTML += `<li class="page-item">
                <button class="page-link" onclick="poSupplierManager.loadAdvancedSearchPage(${this.currentPage + 1})">Next</button>
            </li>`;
        }

        paginationHTML += '</ul>';
        container.innerHTML = paginationHTML;
    }

    selectSupplierFromAdvanced(id, name) {
        // PENTING: Cari supplier input berdasarkan modal yang sedang terbuka
        // Prioritaskan edit form jika edit modal sedang terbuka
        let supplierInput = null;
        let supplierContainer = null;
        
        // Cek apakah edit modal sedang terbuka
        const editModal = document.getElementById('editPurchaseOrderModal');
        const isEditModalOpen = editModal && editModal.classList.contains('show');
        
        if (isEditModalOpen) {
            // Jika edit modal terbuka, cari di edit form dulu
            supplierInput = document.querySelector('#editSupplierSelect');
            if (supplierInput) {
                supplierContainer = supplierInput.closest('.searchable-dropdown-container');
            }
        }
        
        // Jika tidak ditemukan atau create modal yang terbuka, cari di create form
        if (!supplierContainer) {
            supplierInput = document.querySelector('#supplierSelect');
            if (supplierInput) {
                supplierContainer = supplierInput.closest('.searchable-dropdown-container');
            }
        }
        
        if (supplierContainer) {
            const searchInput = supplierContainer.querySelector('.searchable-dropdown-input');
            const selectedIdInput = supplierContainer.querySelector('.selected-id-input');
            
            if (searchInput && selectedIdInput) {
                // PENTING: Set values dengan konversi ke string untuk konsistensi
                const supplierId = String(id);
                const supplierName = String(name);
                
                // PENTING: Force set values langsung tanpa delay
                searchInput.value = supplierName;
                selectedIdInput.value = supplierId;
                searchInput.setAttribute('data-selected-id', supplierId);
                
                // PENTING: Update clear button visibility
                const clearBtn = supplierContainer.querySelector('.clear-btn');
                if (clearBtn) {
                    clearBtn.style.display = supplierName ? 'inline-block' : 'none';
                }
                
                // PENTING: Gunakan requestAnimationFrame untuk memastikan DOM ter-update
                requestAnimationFrame(() => {
                    // Force set lagi untuk memastikan
                    searchInput.value = supplierName;
                    selectedIdInput.value = supplierId;
                    searchInput.setAttribute('data-selected-id', supplierId);
                    
                    // Trigger change event untuk KEDUA input
                    const changeEvent = new Event('change', { bubbles: true });
                    searchInput.dispatchEvent(changeEvent);
                    selectedIdInput.dispatchEvent(changeEvent);
                    
                    // Trigger supplier change event
                    if (window.purchaseOrderManager && typeof window.purchaseOrderManager.onSupplierChange === 'function') {
                        window.purchaseOrderManager.onSupplierChange(supplierId);
                    }
                    
                    // PENTING: Set value lagi beberapa kali untuk memastikan tidak ter-override
                    const forceUpdate = () => {
                        if (searchInput.value !== supplierName) {
                            console.warn('PurchaseOrderSupplierManager: Display value changed! Force restoring...');
                            searchInput.value = supplierName;
                            searchInput.setAttribute('data-selected-id', supplierId);
                        }
                        if (selectedIdInput.value !== supplierId) {
                            console.warn('PurchaseOrderSupplierManager: Form value changed! Force restoring...');
                            selectedIdInput.value = supplierId;
                        }
                    };
                    
                    // Force update beberapa kali dengan interval
                    setTimeout(forceUpdate, 100);
                    setTimeout(forceUpdate, 300);
                    setTimeout(forceUpdate, 500);
                    setTimeout(forceUpdate, 1000);
                });
            }
        }
        
        // Close modal dengan delay untuk memastikan UI ter-update
        setTimeout(() => {
            const modal = bootstrap.Modal.getInstance(document.getElementById('poSupplierAdvancedSearchModal'));
            if (modal) {
                modal.hide();
            }
            
            // Show success message
            this.showSuccess(`Supplier "${name}" selected`);
        }, 100);
    }

    clearAdvancedSearch() {
        // Clear form
        const form = document.getElementById('poSupplierAdvancedSearchForm');
        if (form) {
            form.reset();
        }
        
        // Hide results
        const resultsDiv = document.getElementById('poSupplierSearchResults');
        if (resultsDiv) {
            resultsDiv.style.display = 'none';
        }
        
        // Reset pagination
        this.currentPage = 1;
        this.totalPages = 0;
        this.totalCount = 0;
        this.currentSearchRequest = null;
    }

    formatDate(dateString) {
        if (!dateString) return '-';
        
        try {
            const date = new Date(dateString);
            return date.toLocaleDateString('en-US', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit'
            });
        } catch (error) {
            console.error('Error formatting date:', error);
            return '-';
        }
    }

    showError(message) {
        console.error('Purchase Order Supplier Manager Error:', message);
        
        // Try to show error in modal
        const errorDiv = document.getElementById('poSupplierErrorMessage');
        if (errorDiv) {
            errorDiv.innerHTML = `<i class="fas fa-exclamation-circle me-2"></i>${message}`;
            errorDiv.style.display = 'block';
        } else {
            // Fallback to alert
            alert(`Error: ${message}`);
        }
    }

    showSuccess(message) {
        console.log('Purchase Order Supplier Manager Success:', message);
        
        // Try to show success in modal
        const successDiv = document.getElementById('poSupplierSuccessMessage');
        if (successDiv) {
            successDiv.innerHTML = `<i class="fas fa-check-circle me-2"></i>${message}`;
            successDiv.style.display = 'block';
            
            // Hide after 3 seconds
            setTimeout(() => {
                successDiv.style.display = 'none';
            }, 3000);
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.poSupplierManager = new PurchaseOrderSupplierManager();
});

// Global function untuk dipanggil dari SearchableDropdown
window.openPOSupplierAdvancedSearch = function() {
    if (window.poSupplierManager) {
        window.poSupplierManager.openSupplierAdvancedSearch();
    } else {
        console.error('Purchase Order Supplier Manager not initialized');
    }
};
