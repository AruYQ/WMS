/**
 * ASN Purchase Order Advanced Search Manager
 * Handles advanced search functionality for Purchase Orders in ASN context
 */
class ASNPurchaseOrderManager {
    constructor() {
        this.currentPage = 1;
        this.totalPages = 0;
        this.totalCount = 0;
        this.currentSearchRequest = null;
    }

    /**
     * Open Purchase Order advanced search modal
     */
    async openPOAdvancedSearch() {
        try {
            console.log('ASNPurchaseOrderManager: Opening Purchase Order advanced search');
            
            // Reset search form
            this.resetSearchForm();
            
            // Show modal
            const modalElement = document.getElementById('poAdvancedSearchModal');
            if (modalElement) {
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
                
                // Load initial data
                await this.loadAdvancedSearchPage(1);
            } else {
                console.error('ASNPurchaseOrderManager: Modal element not found');
                this.showError('Failed to open advanced search');
            }
        } catch (error) {
            console.error('ASNPurchaseOrderManager: Error opening advanced search:', error);
            this.showError('Failed to open advanced search');
        }
    }

    /**
     * Search Purchase Orders with advanced criteria
     */
    async searchPOAdvanced() {
        try {
            // Get search criteria
            const searchRequest = {
                poNumber: document.getElementById('asnSearchPONumber').value.trim() || null,
                supplierName: document.getElementById('asnSearchSupplierName').value.trim() || null,
                orderDateFrom: document.getElementById('asnSearchOrderDateFrom').value || null,
                orderDateTo: document.getElementById('asnSearchOrderDateTo').value || null,
                page: 1,
                pageSize: 10
            };

            console.log('ASN Purchase Order search request:', searchRequest);
            
            this.currentSearchRequest = searchRequest;
            this.currentPage = 1;

            // Perform search
            await this.loadAdvancedSearchPage(1);
            
        } catch (error) {
            console.error('Error in Purchase Order advanced search:', error);
            this.showError('Error performing search');
        }
    }

    /**
     * Load advanced search page
     */
    async loadAdvancedSearchPage(page) {
        try {
            if (!this.currentSearchRequest) {
                console.error('ASNPurchaseOrderManager: No search request found');
                return;
            }

            const searchRequest = { ...this.currentSearchRequest, page };
            
            console.log('ASNPurchaseOrderManager: Loading page', page, 'with request:', searchRequest);

            const response = await fetch('/api/asn/purchaseorders/advanced-search', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(searchRequest)
            });

            const result = await response.json();
            console.log('ASNPurchaseOrderManager: Search response:', result);

            if (result.success) {
                this.renderAdvancedSearchResults(result.data);
                this.updatePagination(result.totalCount, result.totalPages);
            } else {
                console.error('Failed to search Purchase Orders:', result.message);
                this.showError(result.message || 'Failed to search Purchase Orders');
            }
        } catch (error) {
            console.error('ASNPurchaseOrderManager: Error loading search page:', error);
            this.showError('Error loading search results');
        }
    }

    /**
     * Render advanced search results
     */
    renderAdvancedSearchResults(purchaseOrders) {
        const tbody = document.getElementById('poAdvancedSearchResultsBody');
        if (!tbody) {
            console.error('ASNPurchaseOrderManager: Results body not found');
            return;
        }

        if (purchaseOrders.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-4">
                        <i class="fas fa-search fa-2x text-muted mb-2"></i>
                        <p class="text-muted">No Purchase Orders found matching your criteria</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = purchaseOrders.map(po => `
            <tr>
                <td><strong>${po.poNumber}</strong></td>
                <td>${po.supplierName}</td>
                <td>${po.supplierEmail}</td>
                <td>${new Date(po.orderDate).toLocaleDateString('id-ID')}</td>
                <td>${new Date(po.expectedDeliveryDate).toLocaleDateString('id-ID')}</td>
                <td><span class="badge ${this.getStatusBadgeClass(po.status)}">${po.status}</span></td>
                <td>Rp ${po.totalAmount.toLocaleString('id-ID')}</td>
                <td>
                    <button type="button" class="btn btn-sm btn-primary" 
                            onclick="asnPOManager.selectPOFromAdvanced(${po.id}, '${po.poNumber}', '${po.supplierName}')">
                        <i class="fas fa-check"></i> Select
                    </button>
                </td>
            </tr>
        `).join('');
    }

    /**
     * Update pagination
     */
    updatePagination(totalCount, totalPages) {
        this.totalCount = totalCount;
        this.totalPages = totalPages;

        const paginationContainer = document.getElementById('poAdvancedSearchPagination');
        if (!paginationContainer) return;

        if (totalPages <= 1) {
            paginationContainer.innerHTML = '';
            return;
        }

        let paginationHtml = '<ul class="pagination justify-content-center">';
        
        // Previous button
        paginationHtml += `
            <li class="page-item ${this.currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="asnPOManager.loadAdvancedSearchPage(${this.currentPage - 1}); return false;">
                    <i class="fas fa-chevron-left"></i>
                </a>
            </li>
        `;

        // Page numbers
        const startPage = Math.max(1, this.currentPage - 2);
        const endPage = Math.min(totalPages, this.currentPage + 2);

        for (let i = startPage; i <= endPage; i++) {
            paginationHtml += `
                <li class="page-item ${i === this.currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" onclick="asnPOManager.loadAdvancedSearchPage(${i}); return false;">${i}</a>
                </li>
            `;
        }

        // Next button
        paginationHtml += `
            <li class="page-item ${this.currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="asnPOManager.loadAdvancedSearchPage(${this.currentPage + 1}); return false;">
                    <i class="fas fa-chevron-right"></i>
                </a>
            </li>
        `;

        paginationHtml += '</ul>';
        paginationContainer.innerHTML = paginationHtml;
    }

    /**
     * Select Purchase Order from advanced search
     */
    selectPOFromAdvanced(poId, poNumber, supplierName) {
        try {
            console.log('ASNPurchaseOrderManager: Selecting PO:', poId, poNumber, supplierName);
            
            // Set selected Purchase Order
            const poSelect = document.getElementById('purchaseOrderId');
            if (poSelect) {
                poSelect.value = poId;
                
                // Trigger change event to load PO details
                const changeEvent = new Event('change', { bubbles: true });
                poSelect.dispatchEvent(changeEvent);
            }

            // Set supplier name
            const supplierNameInput = document.getElementById('supplierName');
            if (supplierNameInput) {
                supplierNameInput.value = supplierName;
            }

            // Close modal
            const modalElement = document.getElementById('poAdvancedSearchModal');
            if (modalElement) {
                const modal = bootstrap.Modal.getInstance(modalElement);
                if (modal) {
                    modal.hide();
                }
            }

            console.log('ASNPurchaseOrderManager: PO selected successfully');
        } catch (error) {
            console.error('ASNPurchaseOrderManager: Error selecting PO:', error);
            this.showError('Error selecting Purchase Order');
        }
    }

    /**
     * Reset search form
     */
    resetSearchForm() {
        document.getElementById('asnSearchPONumber').value = '';
        document.getElementById('asnSearchSupplierName').value = '';
        document.getElementById('asnSearchOrderDateFrom').value = '';
        document.getElementById('asnSearchOrderDateTo').value = '';
    }

    /**
     * Clear search results
     */
    clearAdvancedSearch() {
        this.resetSearchForm();
        this.currentSearchRequest = null;
        this.currentPage = 1;
        
        const tbody = document.getElementById('poAdvancedSearchResultsBody');
        if (tbody) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-4">
                        <i class="fas fa-search fa-2x text-muted mb-2"></i>
                        <p class="text-muted">Enter search criteria and click Search to find Purchase Orders</p>
                    </td>
                </tr>
            `;
        }
        
        const paginationContainer = document.getElementById('poAdvancedSearchPagination');
        if (paginationContainer) {
            paginationContainer.innerHTML = '';
        }
    }

    /**
     * Get status badge class
     */
    getStatusBadgeClass(status) {
        switch (status.toLowerCase()) {
            case 'pending': return 'bg-warning';
            case 'approved': return 'bg-info';
            case 'received': return 'bg-success';
            case 'cancelled': return 'bg-danger';
            default: return 'bg-secondary';
        }
    }

    /**
     * Show error message
     */
    showError(message) {
        console.error('ASNPurchaseOrderManager Error:', message);
        // You can implement toast notification here if needed
        alert(message);
    }
}
