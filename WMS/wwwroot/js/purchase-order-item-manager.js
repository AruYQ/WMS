/**
 * Purchase Order Item Manager - JavaScript untuk handle item selection
 * Handles advanced search untuk Purchase Order item selection
 */

class PurchaseOrderItemManager {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalPages = 0;
        this.totalCount = 0;
        this.currentSearchRequest = null;
        this.currentSupplierId = null;
        this.currentPurchaseOrderId = null;
        this.currentItemIndex = null;
        
        this.init();
    }

    init() {
        this.bindEvents();
    }

    bindEvents() {
        // Enter key untuk advanced search
        document.getElementById('poItemAdvancedSearchForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.searchItemsAdvanced();
        });
        
        // Cleanup backdrop saat modal ditutup
        document.getElementById('poItemAdvancedSearchModal')?.addEventListener('hidden.bs.modal', () => {
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
            
            console.log('Purchase Order Item: Advanced search modal closed (no backdrop cleanup needed)');
        } catch (error) {
            console.error('Purchase Order Item: Error during modal cleanup:', error);
        }
    }

    // ===== ADVANCED SEARCH FUNCTIONS =====

    async openItemAdvancedSearch(supplierId, purchaseOrderId, itemIndex) {
        try {
            // Store context for search
            this.currentSupplierId = supplierId;
            this.currentPurchaseOrderId = purchaseOrderId;
            this.currentItemIndex = itemIndex;
            
            if (!supplierId) {
                this.showError('Please select a supplier first before searching for items');
                return;
            }

            const modal = new bootstrap.Modal(document.getElementById('poItemAdvancedSearchModal'), {
                backdrop: false,     // Tidak ada backdrop untuk advanced search
                keyboard: true,      // Bisa ditutup dengan ESC
                focus: true          // Focus ke modal saat dibuka
            });
            modal.show();
            
            // Clear previous search
            this.clearAdvancedSearch();
        } catch (error) {
            console.error('Error opening item advanced search:', error);
            this.showError('Failed to open advanced search');
        }
    }

    async searchItemsAdvanced() {
        try {
            // Get search criteria
            const searchRequest = {
                itemCode: document.getElementById('poSearchItemCode').value.trim() || null,
                name: document.getElementById('poSearchItemName').value.trim() || null,
                unit: document.getElementById('poSearchItemUnit').value.trim() || null,
                createdDateFrom: document.getElementById('poSearchItemCreatedDateFrom').value || null,
                createdDateTo: document.getElementById('poSearchItemCreatedDateTo').value || null,
                supplierId: this.currentSupplierId,
                purchaseOrderId: this.currentPurchaseOrderId,
                page: 1,
                pageSize: 10
            };

            console.log('Purchase Order Item search request before processing:', searchRequest);
            
            // Check if supplier is selected
            if (!searchRequest.supplierId) {
                this.showError('Supplier must be selected before searching items');
                return;
            }

            this.currentSearchRequest = searchRequest;
            this.currentPage = 1;

            // Perform search
            await this.loadAdvancedSearchPage(1);
            
        } catch (error) {
            console.error('Error in item advanced search:', error);
            this.showError('Error performing search');
        }
    }

    async loadAdvancedSearchPage(page) {
        try {
            if (!this.currentSearchRequest) return;

            const request = { ...this.currentSearchRequest, page: page };
            
            const response = await fetch('/api/purchaseorder/items/advanced-search', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(request)
            });

            const data = await response.json();
            
            if (data.success) {
                this.renderAdvancedSearchResults(data);
                this.currentPage = page;
            } else {
                this.showError(data.message || 'Error performing search');
            }
        } catch (error) {
            console.error('Error loading advanced search page:', error);
            this.showError('Error loading search results');
        }
    }

    renderAdvancedSearchResults(data) {
        const resultsDiv = document.getElementById('poItemSearchResults');
        const tbody = document.getElementById('poItemSearchResultsBody');
        const pagination = document.getElementById('poItemSearchPagination');

        if (!resultsDiv || !tbody || !pagination) {
            console.error('Required DOM elements not found');
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
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3">No items found matching your criteria</td></tr>';
        } else {
            tbody.innerHTML = data.data.map(item => `
                <tr>
                    <td>${item.itemCode}</td>
                    <td>${item.name}</td>
                    <td>${item.unit}</td>
                    <td>${new Intl.NumberFormat('id-ID', { style: 'currency', currency: 'IDR' }).format(item.purchasePrice)}</td>
                    <td>${this.formatDate(item.createdDate)}</td>
                    <td>
                        <button type="button" class="btn btn-sm btn-primary" 
                                onclick="poItemManager.selectItemFromAdvanced(${item.id}, '${item.name}', '${item.itemCode}', ${item.purchasePrice}, '${item.unit}')">
                            <i class="fas fa-check"></i> Select
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

        let paginationHtml = '<li class="page-item' + (this.currentPage === 1 ? ' disabled' : '') + '">' +
            '<a class="page-link" href="#" onclick="poItemManager.loadAdvancedSearchPage(' + (this.currentPage - 1) + ')">Previous</a></li>';

        for (let i = 1; i <= this.totalPages; i++) {
            if (i === this.currentPage) {
                paginationHtml += '<li class="page-item active"><span class="page-link">' + i + '</span></li>';
            } else {
                paginationHtml += '<li class="page-item"><a class="page-link" href="#" onclick="poItemManager.loadAdvancedSearchPage(' + i + ')">' + i + '</a></li>';
            }
        }

        paginationHtml += '<li class="page-item' + (this.currentPage === this.totalPages ? ' disabled' : '') + '">' +
            '<a class="page-link" href="#" onclick="poItemManager.loadAdvancedSearchPage(' + (this.currentPage + 1) + ')">Next</a></li>';

        container.querySelector('ul').innerHTML = paginationHtml;
    }

    selectItemFromAdvanced(itemId, itemName, itemCode, purchasePrice, unit) {
        try {
            if (this.currentItemIndex !== null && this.currentItemIndex !== undefined) {
                // Find the specific item row and update it
                const itemRows = document.querySelectorAll('.item-row');
                const targetRow = itemRows[this.currentItemIndex];
                
                if (targetRow) {
                    const itemSelect = targetRow.querySelector('.item-select');
                    const unitPriceInput = targetRow.querySelector('.unit-price-input');
                    
                    if (itemSelect) {
                        // Check if option already exists
                        let selectedOption = itemSelect.querySelector(`option[value="${itemId}"]`);
                        
                        if (!selectedOption) {
                            // Create new option
                            selectedOption = document.createElement('option');
                            selectedOption.value = itemId;
                            selectedOption.textContent = `${itemName} (${itemCode})`;
                            selectedOption.setAttribute('data-unit', unit);
                            selectedOption.setAttribute('data-price', purchasePrice);
                            itemSelect.appendChild(selectedOption);
                        }
                        
                        // Set the selected value
                        itemSelect.value = itemId;
                        
                        // Trigger change event to update dependent fields
                        itemSelect.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                    
                    if (unitPriceInput) {
                        unitPriceInput.value = purchasePrice;
                        // Trigger change event to calculate total
                        unitPriceInput.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                }
            }

            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('poItemAdvancedSearchModal'));
            if (modal) {
                modal.hide();
            }

            this.clearAdvancedSearch();
            
        } catch (error) {
            console.error('Error selecting item from advanced search:', error);
            this.showError('Error selecting item');
        }
    }

    clearAdvancedSearch() {
        // Clear form fields
        document.getElementById('poSearchItemCode').value = '';
        document.getElementById('poSearchItemName').value = '';
        document.getElementById('poSearchItemUnit').value = '';
        document.getElementById('poSearchItemCreatedDateFrom').value = '';
        document.getElementById('poSearchItemCreatedDateTo').value = '';

        // Hide results
        const resultsDiv = document.getElementById('poItemSearchResults');
        if (resultsDiv) {
            resultsDiv.style.display = 'none';
        }

        // Clear results body
        const tbody = document.getElementById('poItemSearchResultsBody');
        if (tbody) {
            tbody.innerHTML = '';
        }

        // Clear pagination
        const pagination = document.getElementById('poItemSearchPagination');
        if (pagination && pagination.querySelector('ul')) {
            pagination.querySelector('ul').innerHTML = '';
        }

        // Clear messages
        this.clearError();
        this.clearSuccess();

        // Reset pagination state
        this.currentPage = 1;
        this.totalPages = 0;
        this.totalCount = 0;
        this.currentSearchRequest = null;
    }

    // ===== UTILITY FUNCTIONS =====

    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    showError(message) {
        const errorDiv = document.getElementById('poItemErrorMessage');
        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.style.display = 'block';
        }
        this.clearSuccess();
    }

    showSuccess(message) {
        const successDiv = document.getElementById('poItemSuccessMessage');
        if (successDiv) {
            successDiv.textContent = message;
            successDiv.style.display = 'block';
        }
        this.clearError();
    }

    clearError() {
        const errorDiv = document.getElementById('poItemErrorMessage');
        if (errorDiv) {
            errorDiv.style.display = 'none';
            errorDiv.textContent = '';
        }
    }

    clearSuccess() {
        const successDiv = document.getElementById('poItemSuccessMessage');
        if (successDiv) {
            successDiv.style.display = 'none';
            successDiv.textContent = '';
        }
    }

    formatDate(dateString) {
        try {
            const date = new Date(dateString);
            return date.toLocaleDateString('id-ID');
        } catch (error) {
            return dateString;
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.poItemManager = new PurchaseOrderItemManager();
});

// Global function to be called from HTML
window.openPOItemAdvancedSearch = (supplierId, purchaseOrderId, itemIndex) => {
    if (window.poItemManager) {
        window.poItemManager.openItemAdvancedSearch(supplierId, purchaseOrderId, itemIndex);
    } else {
        console.error('Purchase Order Item Manager not initialized');
    }
};
