/**
 * Putaway Manager - AJAX-based operations
 * Manages all Putaway operations following the established pattern
 */
class PutawayManager {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.currentStatusFilter = null;
        this.showTodayOnly = false;
        this.currentSearch = '';
        
        this.init();
    }

    /**
     * Initialize the manager
     */
    async init() {
        try {
            console.log('PutawayManager: Initializing...');
            
            // Wait a bit for DOM to be fully ready
            await new Promise(resolve => setTimeout(resolve, 100));
            
            // Bind events first
            this.bindEvents();
            
            // Load initial data
            await this.loadDashboard();
            await this.loadPutawayASNs();
            
            console.log('PutawayManager: Initialized successfully');
        } catch (error) {
            console.error('PutawayManager: Initialization failed:', error);
            // Retry initialization after a short delay
            setTimeout(() => {
                if (document.readyState === 'complete') {
                    console.log('PutawayManager: Retrying initialization...');
                    this.init();
                }
            }, 1000);
        }
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        // Filter events
        document.getElementById('statusFilter')?.addEventListener('change', (e) => {
            this.currentStatusFilter = e.target.value;
            this.currentPage = 1;
            this.loadPutawayASNs();
        });

        document.getElementById('showTodayOnly')?.addEventListener('change', (e) => {
            this.showTodayOnly = e.target.checked;
            this.currentPage = 1;
            this.loadPutawayASNs();
        });

        document.getElementById('pageSizeSelect')?.addEventListener('change', (e) => {
            this.pageSize = parseInt(e.target.value);
            this.currentPage = 1;
            this.loadPutawayASNs();
        });

        // Refresh button
        document.getElementById('refreshBtn')?.addEventListener('click', () => {
            this.loadDashboard();
            this.loadPutawayASNs();
        });
    }

    /**
     * Load dashboard statistics
     */
    async loadDashboard() {
        try {
            const response = await fetch('/api/Putaway/Dashboard');
            const result = await response.json();

            if (result.success && result.data) {
                const data = result.data;
                const totalASNsEl = document.getElementById('totalProcessedASNs');
                const pendingItemsEl = document.getElementById('totalPendingItems');
                const pendingQuantityEl = document.getElementById('totalPendingQuantity');
                const todayCountEl = document.getElementById('todayPutawayCount');
                
                if (totalASNsEl) totalASNsEl.textContent = data.totalProcessedASNs || 0;
                if (pendingItemsEl) pendingItemsEl.textContent = data.totalPendingItems || 0;
                if (pendingQuantityEl) pendingQuantityEl.textContent = (data.totalPendingQuantity || 0).toLocaleString();
                if (todayCountEl) todayCountEl.textContent = data.todayPutawayCount || 0;
            } else {
                console.error('Failed to load dashboard:', result.message);
            }
        } catch (error) {
            console.error('Error loading dashboard:', error);
        }
    }

    /**
     * Load Putaway ASNs list with pagination
     */
    async loadPutawayASNs() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                ...(this.currentStatusFilter && { statusFilter: this.currentStatusFilter }),
                showTodayOnly: this.showTodayOnly
            });

            const response = await fetch(`/api/Putaway?${params}`);
            const result = await response.json();

            if (result.success) {
                this.renderPutawayTable(result.data || []);
                this.updatePagination(result.pagination);
            } else {
                this.showError(result.message || 'Failed to load ASNs');
            }
        } catch (error) {
            console.error('Error loading Putaway ASNs:', error);
            this.showError('Error loading ASNs');
        }
    }

    /**
     * Render Putaway ASN table
     */
    renderPutawayTable(asns) {
        const container = document.getElementById('putawayTableContainer');
        if (!container) return;

        let tableHtml = `
            <div class="table-responsive">
                <table class="table table-hover mb-0">
                    <thead>
                        <tr>
                            <th>ASN Number</th>
                            <th>PO Number</th>
                            <th>Supplier</th>
                            <th>Arrival Date</th>
                            <th>Items</th>
                            <th>Quantity</th>
                            <th>Progress</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        if (asns && asns.length > 0) {
            asns.forEach(asn => {
                const statusBadge = this.getStatusBadge(asn.status);
                const progressBar = this.getProgressBar(asn.completionPercentage, asn.isCompleted);
                const arrivalDate = asn.actualArrivalDate ? 
                    new Date(asn.actualArrivalDate).toLocaleDateString() : 'Not set';
                
                tableHtml += `
                    <tr>
                        <td>
                            <div class="fw-medium text-primary">${asn.asnNumber}</div>
                            ${asn.isCompleted ? '<div class="small text-success"><i class="fas fa-check-circle me-1"></i>Completed</div>' : ''}
                        </td>
                        <td>
                            <div class="fw-medium">${asn.poNumber}</div>
                        </td>
                        <td>
                            <div class="fw-medium">${asn.supplierName}</div>
                        </td>
                        <td>
                            <span class="fw-medium">${arrivalDate}</span>
                        </td>
                        <td>
                            <div class="fw-medium">${asn.totalItemTypes} types</div>
                            <div class="small text-muted">${asn.pendingPutawayCount} pending</div>
                        </td>
                        <td>
                            <div class="fw-medium">${asn.totalQuantity.toLocaleString()}</div>
                        </td>
                        <td>
                            ${progressBar}
                        </td>
                        <td>
                            ${statusBadge}
                        </td>
                        <td>
                            <div class="btn-group" role="group">
                                <a href="/Inventory/ProcessPutaway?asnId=${asn.asnId}" class="btn btn-sm btn-outline-primary" title="Process Putaway">
                                    <i class="fas fa-truck-loading"></i>
                                </a>
                                <button type="button" class="btn btn-sm btn-outline-success" onclick="putawayManager.bulkPutaway(${asn.asnId})" title="Bulk Putaway">
                                    <i class="fas fa-forward"></i>
                                </button>
                            </div>
                        </td>
                    </tr>
                `;
            });
        } else {
            tableHtml += `
                <tr>
                    <td colspan="9" class="text-center text-muted py-4">
                        <i class="fas fa-truck-loading fa-2x mb-2"></i><br>
                        No ASNs ready for putaway
                    </td>
                </tr>
            `;
        }

        tableHtml += `
                    </tbody>
                </table>
            </div>
        `;

        container.innerHTML = tableHtml;
    }

    /**
     * Update pagination controls
     */
    updatePagination(pagination) {
        // Add null check before destructuring to prevent "Invalid left-hand side assignment" error
        if (!pagination || typeof pagination !== 'object') {
            console.error('Pagination data is missing or invalid:', pagination);
            return;
        }
        
        const { currentPage, totalPages, totalCount, pageSize } = pagination;
        
        // Additional null checks for destructured values
        const safeCurrentPage = currentPage || 1;
        const safeTotalPages = totalPages || 1;
        const safeTotalCount = totalCount || 0;
        const safePageSize = pageSize || 10;
        
        const start = (safeCurrentPage - 1) * safePageSize + 1;
        const end = Math.min(safeCurrentPage * safePageSize, safeTotalCount);
        
        // Update pagination info with null safety
        const showingStartEl = document.getElementById('showingStart');
        const showingEndEl = document.getElementById('showingEnd');
        const totalRecordsEl = document.getElementById('totalRecords');
        const currentPageNumEl = document.getElementById('currentPageNum');
        const totalPagesNumEl = document.getElementById('totalPagesNum');
        
        if (showingStartEl) showingStartEl.textContent = start;
        if (showingEndEl) showingEndEl.textContent = end;
        if (totalRecordsEl) totalRecordsEl.textContent = safeTotalCount;
        if (currentPageNumEl) currentPageNumEl.textContent = safeCurrentPage;
        if (totalPagesNumEl) totalPagesNumEl.textContent = safeTotalPages;
        
        // Update pagination buttons
        const prevBtn = document.getElementById('prevPageBtn');
        const nextBtn = document.getElementById('nextPageBtn');
        
        if (prevBtn) {
            prevBtn.disabled = safeCurrentPage <= 1;
            prevBtn.onclick = safeCurrentPage <= 1 ? null : () => this.previousPage();
        }
        
        if (nextBtn) {
            nextBtn.disabled = safeCurrentPage >= safeTotalPages;
            nextBtn.onclick = safeCurrentPage >= safeTotalPages ? null : () => this.nextPage();
        }
    }

    /**
     * Bulk putaway function
     */
    async bulkPutaway(asnId) {
        if (confirm('Are you sure you want to process all ready items for this ASN in bulk?')) {
            try {
                // This would make an AJAX call to process bulk putaway
                // For now, redirect to the process putaway page
                window.location.href = `/Inventory/ProcessPutaway?asnId=${asnId}&bulk=true`;
            } catch (error) {
                console.error('Error processing bulk putaway:', error);
                this.showError('Error processing bulk putaway');
            }
        }
    }

    /**
     * Pagination methods
     */
    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadPutawayASNs();
        }
    }

    nextPage() {
        this.currentPage++;
        this.loadPutawayASNs();
    }

    /**
     * Utility methods
     */
    getStatusBadge(status) {
        const statusMap = {
            'Processed': 'badge bg-success',
            'Pending': 'badge bg-warning',
            'Completed': 'badge bg-primary'
        };
        return `<span class="${statusMap[status] || 'badge bg-secondary'}">${status}</span>`;
    }

    getProgressBar(percentage, isCompleted) {
        const progressClass = isCompleted ? 'bg-success' : percentage > 70 ? 'bg-info' : percentage > 30 ? 'bg-warning' : 'bg-danger';
        
        return `
            <div class="progress" style="height: 20px;">
                <div class="progress-bar ${progressClass}" 
                     role="progressbar" 
                     style="width: ${percentage}%"
                     aria-valuenow="${percentage}" 
                     aria-valuemin="0" 
                     aria-valuemax="100">
                    ${percentage.toFixed(0)}%
                </div>
            </div>
        `;
    }

    showError(message) {
        const errorElement = document.getElementById('errorMessage');
        if (errorElement) {
            errorElement.innerHTML = `<i class="fas fa-exclamation-triangle me-2"></i>${message}`;
            errorElement.classList.remove('d-none');
        }
        console.error('Error:', message);
    }

    showSuccess(message) {
        const successElement = document.getElementById('successMessage');
        if (successElement) {
            successElement.innerHTML = `<i class="fas fa-check-circle me-2"></i>${message}`;
            successElement.classList.remove('d-none');
            setTimeout(() => this.clearSuccess(), 3000);
        }
    }

    clearError() {
        const errorElement = document.getElementById('errorMessage');
        if (errorElement) {
            errorElement.classList.add('d-none');
            errorElement.innerHTML = '';
        }
    }

    clearSuccess() {
        const successElement = document.getElementById('successMessage');
        if (successElement) {
            successElement.classList.add('d-none');
            successElement.innerHTML = '';
        }
    }
}

// Global functions for onclick handlers
function previousPage() {
    if (window.putawayManager) {
        window.putawayManager.previousPage();
    }
}

function nextPage() {
    if (window.putawayManager) {
        window.putawayManager.nextPage();
    }
}

function changePageSize() {
    if (window.putawayManager) {
        const pageSize = document.getElementById('pageSizeSelect')?.value;
        if (pageSize) {
            window.putawayManager.pageSize = parseInt(pageSize);
            window.putawayManager.currentPage = 1;
            window.putawayManager.loadPutawayASNs();
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    try {
        window.putawayManager = new PutawayManager();
    } catch (error) {
        console.error('Error creating PutawayManager:', error);
        // Retry after DOM is fully ready
        setTimeout(() => {
            try {
                window.putawayManager = new PutawayManager();
            } catch (retryError) {
                console.error('Failed to initialize PutawayManager after retry:', retryError);
            }
        }, 500);
    }
});
