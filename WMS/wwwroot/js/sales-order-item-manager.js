/**
 * Sales Order Item Manager - Advanced Search Item Selection
 * Handles advanced search untuk Sales Order item selection (Inventory-based)
 */

class SalesOrderItemManager {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalPages = 0;
        this.totalCount = 0;
        this.currentSearchRequest = null;
        this.currentItemIndex = null;
        
        this.init();
    }

    init() {
        this.bindEvents();
    }

    bindEvents() {
        // Enter key untuk advanced search
        document.getElementById('soItemAdvancedSearchForm')?.addEventListener('submit', (e) => {
            e.preventDefault();
            this.searchItemsAdvanced();
        });
        
        // Cleanup saat modal ditutup
        document.getElementById('soItemAdvancedSearchModal')?.addEventListener('hidden.bs.modal', () => {
            this.cleanupBackdrop();
        });
    }

    /**
     * Cleanup saat modal ditutup
     */
    cleanupBackdrop() {
        try {
            // Hanya cleanup jika ada modal-backdrop yang tidak seharusnya ada
            const backdrops = document.querySelectorAll('.modal-backdrop');
            if (backdrops.length > 0) {
                backdrops.forEach(backdrop => {
                    if (backdrop.classList.contains('advanced-search-backdrop')) {
                        backdrop.remove();
                    }
                });
            }
            
            console.log('Sales Order Item: Advanced search modal closed');
        } catch (error) {
            console.error('Sales Order Item: Error during modal cleanup:', error);
        }
    }

    // ===== ADVANCED SEARCH FUNCTIONS =====

    async openItemAdvancedSearch(itemIndex) {
        try {
            // Store context for search
            this.currentItemIndex = itemIndex;
            
            const modal = new bootstrap.Modal(document.getElementById('soItemAdvancedSearchModal'), {
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
                itemCode: document.getElementById('soSearchItemCode').value.trim() || null,
                name: document.getElementById('soSearchItemName').value.trim() || null,
                unit: document.getElementById('soSearchItemUnit').value.trim() || null,
                createdDateFrom: document.getElementById('soSearchItemCreatedDateFrom').value || null,
                createdDateTo: document.getElementById('soSearchItemCreatedDateTo').value || null,
                page: 1,
                pageSize: 10
            };

            console.log('Sales Order Item search request:', searchRequest);

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
            
            const response = await fetch('/api/salesorder/items/advanced-search', {
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
        const resultsDiv = document.getElementById('soItemSearchResults');
        const tbody = document.getElementById('soItemSearchResultsBody');
        const pagination = document.getElementById('soItemSearchPagination');

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
            tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-3">No items found matching your criteria</td></tr>';
        } else {
            tbody.innerHTML = data.data.map(item => `
                <tr>
                    <td>${item.itemCode}</td>
                    <td>${item.name}</td>
                    <td>${item.unit}</td>
                    <td>Rp ${parseFloat(item.standardPrice).toLocaleString('id-ID')}</td>
                    <td><span class="badge bg-success">${item.totalStock}</span></td>
                    <td>${this.formatDate(item.createdDate)}</td>
                    <td>
                        <button type="button" class="btn btn-sm btn-primary" 
                                onclick="salesOrderItemManager.selectItemFromAdvanced(${item.id}, '${this.escapeHtml(item.name)}', '${this.escapeHtml(item.itemCode)}', ${item.standardPrice}, '${this.escapeHtml(item.unit)}', ${item.totalStock})">
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
            '<a class="page-link" href="#" onclick="salesOrderItemManager.loadAdvancedSearchPage(' + (this.currentPage - 1) + '); return false;">Previous</a></li>';

        for (let i = 1; i <= this.totalPages; i++) {
            if (i === this.currentPage) {
                paginationHtml += '<li class="page-item active"><span class="page-link">' + i + '</span></li>';
            } else {
                paginationHtml += '<li class="page-item"><a class="page-link" href="#" onclick="salesOrderItemManager.loadAdvancedSearchPage(' + i + '); return false;">' + i + '</a></li>';
            }
        }

        paginationHtml += '<li class="page-item' + (this.currentPage === this.totalPages ? ' disabled' : '') + '">' +
            '<a class="page-link" href="#" onclick="salesOrderItemManager.loadAdvancedSearchPage(' + (this.currentPage + 1) + '); return false;">Next</a></li>';

        container.querySelector('ul').innerHTML = paginationHtml;
    }

    selectItemFromAdvanced(itemId, itemName, itemCode, standardPrice, unit, totalStock) {
        try {
            if (!window.salesOrderManager) {
                this.showError('Sales Order Manager not initialized');
                return;
            }

            // Check if item already added
            if (window.salesOrderManager.selectedItemIds.includes(itemId)) {
                alert('This item is already added. Duplicate items are not allowed.');
                return;
            }

            // Add item to Sales Order
            const newItem = {
                id: Date.now(),
                itemId: itemId,
                itemCode: itemCode,
                itemName: itemName,
                unit: unit,
                quantity: 1,
                unitPrice: parseFloat(standardPrice),
                totalPrice: parseFloat(standardPrice),
                availableStock: parseInt(totalStock)
            };

            window.salesOrderManager.currentSOItems.push(newItem);
            window.salesOrderManager.selectedItemIds.push(itemId);

            // Determine which form is active
            const editModal = document.getElementById('editSOModal');
            const formType = editModal && editModal.classList.contains('show') ? 'edit' : 'create';

            // Update UI
            window.salesOrderManager.updateSOItemsContainer(formType);
            window.salesOrderManager.updateSOSummary(formType);

            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('soItemAdvancedSearchModal'));
            if (modal) {
                modal.hide();
            }

            this.clearAdvancedSearch();
            
            // Show success message
            window.salesOrderManager.showSuccess('Item added successfully');
            
        } catch (error) {
            console.error('Error selecting item from advanced search:', error);
            this.showError('Error selecting item');
        }
    }

    clearAdvancedSearch() {
        // Clear form fields
        const codeInput = document.getElementById('soSearchItemCode');
        const nameInput = document.getElementById('soSearchItemName');
        const unitInput = document.getElementById('soSearchItemUnit');
        const dateFromInput = document.getElementById('soSearchItemCreatedDateFrom');
        const dateToInput = document.getElementById('soSearchItemCreatedDateTo');

        if (codeInput) codeInput.value = '';
        if (nameInput) nameInput.value = '';
        if (unitInput) unitInput.value = '';
        if (dateFromInput) dateFromInput.value = '';
        if (dateToInput) dateToInput.value = '';

        // Hide results
        const resultsDiv = document.getElementById('soItemSearchResults');
        if (resultsDiv) {
            resultsDiv.style.display = 'none';
        }

        // Clear results body
        const tbody = document.getElementById('soItemSearchResultsBody');
        if (tbody) {
            tbody.innerHTML = '';
        }

        // Clear pagination
        const pagination = document.getElementById('soItemSearchPagination');
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
        const errorDiv = document.getElementById('soItemErrorMessage');
        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.style.display = 'block';
        }
        this.clearSuccess();
    }

    showSuccess(message) {
        const successDiv = document.getElementById('soItemSuccessMessage');
        if (successDiv) {
            successDiv.textContent = message;
            successDiv.style.display = 'block';
        }
        this.clearError();
    }

    clearError() {
        const errorDiv = document.getElementById('soItemErrorMessage');
        if (errorDiv) {
            errorDiv.style.display = 'none';
            errorDiv.textContent = '';
        }
    }

    clearSuccess() {
        const successDiv = document.getElementById('soItemSuccessMessage');
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

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.salesOrderItemManager = new SalesOrderItemManager();
});

// Global function to be called from HTML
window.openSOItemAdvancedSearch = (itemIndex) => {
    if (window.salesOrderItemManager) {
        window.salesOrderItemManager.openItemAdvancedSearch(itemIndex);
    } else {
        console.error('Sales Order Item Manager not initialized');
    }
};
