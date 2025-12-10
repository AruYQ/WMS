/**
 * ASN Location Advanced Search Manager
 * Handles advanced search functionality for Locations in ASN context
 */
class ASNLocationManager {
    constructor() {
        this.currentPage = 1;
        this.totalPages = 0;
        this.totalCount = 0;
        this.currentSearchRequest = null;
    }

    /**
     * Open Location advanced search modal
     */
    async openLocationAdvancedSearch() {
        try {
            console.log('ASNLocationManager: Opening Location advanced search');
            
            // Reset search form
            this.resetSearchForm();
            
            // Show modal
            const modalElement = document.getElementById('locationAdvancedSearchModal');
            if (modalElement) {
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
                
                // Load initial data
                await this.loadAdvancedSearchPage(1);
            } else {
                console.error('ASNLocationManager: Modal element not found');
                this.showError('Failed to open advanced search');
            }
        } catch (error) {
            console.error('ASNLocationManager: Error opening advanced search:', error);
            this.showError('Failed to open advanced search');
        }
    }

    /**
     * Search Locations with advanced criteria
     */
    async searchLocationsAdvanced() {
        try {
            // Get search criteria
            const searchRequest = {
                name: document.getElementById('asnSearchLocationName').value.trim() || null,
                code: document.getElementById('asnSearchLocationCode').value.trim() || null,
                createdDateFrom: document.getElementById('asnSearchLocationDateFrom').value || null,
                createdDateTo: document.getElementById('asnSearchLocationDateTo').value || null,
                page: 1,
                pageSize: 10
            };

            console.log('ASN Location search request:', searchRequest);
            
            this.currentSearchRequest = searchRequest;
            this.currentPage = 1;

            // Perform search
            await this.loadAdvancedSearchPage(1);
            
        } catch (error) {
            console.error('Error in Location advanced search:', error);
            this.showError('Error performing search');
        }
    }

    /**
     * Load advanced search page
     */
    async loadAdvancedSearchPage(page) {
        try {
            if (!this.currentSearchRequest) {
                console.error('ASNLocationManager: No search request found');
                return;
            }

            const searchRequest = { ...this.currentSearchRequest, page };
            
            console.log('ASNLocationManager: Loading page', page, 'with request:', searchRequest);

            const response = await fetch('/api/asn/locations/advanced-search', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(searchRequest)
            });

            const result = await response.json();
            console.log('ASNLocationManager: Search response:', result);

            if (result.success) {
                this.renderAdvancedSearchResults(result.data);
                this.updatePagination(result.totalCount, result.totalPages);
            } else {
                console.error('Failed to search Locations:', result.message);
                this.showError(result.message || 'Failed to search Locations');
            }
        } catch (error) {
            console.error('ASNLocationManager: Error loading search page:', error);
            this.showError('Error loading search results');
        }
    }

    /**
     * Render advanced search results
     */
    renderAdvancedSearchResults(locations) {
        const tbody = document.getElementById('locationAdvancedSearchResultsBody');
        if (!tbody) {
            console.error('ASNLocationManager: Results body not found');
            return;
        }

        if (locations.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-4">
                        <i class="fas fa-search fa-2x text-muted mb-2"></i>
                        <p class="text-muted">No Locations found matching your criteria</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = locations.map(location => `
            <tr>
                <td><strong>${location.name}</strong></td>
                <td>${location.code}</td>
                <td>${location.description || '-'}</td>
                <td>${location.maxCapacity.toLocaleString('id-ID')}</td>
                <td>${location.currentCapacity.toLocaleString('id-ID')}</td>
                <td>${location.availableCapacity.toLocaleString('id-ID')}</td>
                <td><span class="badge ${location.isActive ? 'bg-success' : 'bg-secondary'}">${location.isActive ? 'Active' : 'Inactive'}</span></td>
                <td>
                    <button type="button" class="btn btn-sm btn-primary" 
                            onclick="asnLocationManager.selectLocationFromAdvanced(${location.id}, '${location.name}', '${location.code}')">
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

        const paginationContainer = document.getElementById('locationAdvancedSearchPagination');
        if (!paginationContainer) return;

        if (totalPages <= 1) {
            paginationContainer.innerHTML = '';
            return;
        }

        let paginationHtml = '<ul class="pagination justify-content-center">';
        
        // Previous button
        paginationHtml += `
            <li class="page-item ${this.currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="asnLocationManager.loadAdvancedSearchPage(${this.currentPage - 1}); return false;">
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
                    <a class="page-link" href="#" onclick="asnLocationManager.loadAdvancedSearchPage(${i}); return false;">${i}</a>
                </li>
            `;
        }

        // Next button
        paginationHtml += `
            <li class="page-item ${this.currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="asnLocationManager.loadAdvancedSearchPage(${this.currentPage + 1}); return false;">
                    <i class="fas fa-chevron-right"></i>
                </a>
            </li>
        `;

        paginationHtml += '</ul>';
        paginationContainer.innerHTML = paginationHtml;
    }

    /**
     * Select Location from advanced search
     */
    selectLocationFromAdvanced(locationId, locationName, locationCode) {
        try {
            console.log('ASNLocationManager: Selecting Location:', locationId, locationName, locationCode);
            
            // Set selected Location
            const locationSelect = document.getElementById('holdingLocationId');
            if (locationSelect) {
                locationSelect.value = locationId;
                
                // Trigger change event to load location details
                const changeEvent = new Event('change', { bubbles: true });
                locationSelect.dispatchEvent(changeEvent);
            }

            // Close modal
            const modalElement = document.getElementById('locationAdvancedSearchModal');
            if (modalElement) {
                const modal = bootstrap.Modal.getInstance(modalElement);
                if (modal) {
                    modal.hide();
                }
            }

            console.log('ASNLocationManager: Location selected successfully');
        } catch (error) {
            console.error('ASNLocationManager: Error selecting Location:', error);
            this.showError('Error selecting Location');
        }
    }

    /**
     * Reset search form
     */
    resetSearchForm() {
        document.getElementById('asnSearchLocationName').value = '';
        document.getElementById('asnSearchLocationCode').value = '';
        document.getElementById('asnSearchLocationDateFrom').value = '';
        document.getElementById('asnSearchLocationDateTo').value = '';
    }

    /**
     * Clear search results
     */
    clearAdvancedSearch() {
        this.resetSearchForm();
        this.currentSearchRequest = null;
        this.currentPage = 1;
        
        const tbody = document.getElementById('locationAdvancedSearchResultsBody');
        if (tbody) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-4">
                        <i class="fas fa-search fa-2x text-muted mb-2"></i>
                        <p class="text-muted">Enter search criteria and click Search to find Locations</p>
                    </td>
                </tr>
            `;
        }
        
        const paginationContainer = document.getElementById('locationAdvancedSearchPagination');
        if (paginationContainer) {
            paginationContainer.innerHTML = '';
        }
    }

    /**
     * Show error message
     */
    showError(message) {
        console.error('ASNLocationManager Error:', message);
        // You can implement toast notification here if needed
        alert(message);
    }
}
