/**
 * ASN Item Autocomplete Manager
 * Handles autocomplete functionality for item search in ASN
 */
class ASNItemAutocomplete {
    constructor() {
        this.debounceTimer = null;
        this.minSearchLength = 2;
        this.searchDelay = 300; // ms
    }

    /**
     * Initialize autocomplete for a specific item search input
     */
    initialize(inputId, resultsId, itemIdInput, onItemSelect) {
        const searchInput = document.getElementById(inputId);
        const resultsContainer = document.getElementById(resultsId);
        const hiddenItemIdInput = document.getElementById(itemIdInput);

        if (!searchInput || !resultsContainer) {
            console.warn('ASNItemAutocomplete: Required elements not found');
            return;
        }

        // Clear any existing event listeners
        searchInput.removeEventListener('input', this.handleInput);
        searchInput.removeEventListener('blur', this.handleBlur);
        searchInput.removeEventListener('focus', this.handleFocus);

        // Bind event listeners
        this.handleInput = this.debounce((e) => this.searchItems(e.target.value, resultsContainer, hiddenItemIdInput, onItemSelect), this.searchDelay);
        this.handleBlur = () => this.hideResults(resultsContainer);
        this.handleFocus = (e) => {
            if (e.target.value.length >= this.minSearchLength) {
                this.showResults(resultsContainer);
            } else if (e.target.value.length === 0) {
                // Show top 3 items when focus without input
                this.loadTopItems(resultsContainer, hiddenItemIdInput, onItemSelect);
            }
        };

        searchInput.addEventListener('input', this.handleInput);
        searchInput.addEventListener('blur', this.handleBlur);
        searchInput.addEventListener('focus', this.handleFocus);

        // Handle clicks on results
        resultsContainer.addEventListener('click', (e) => {
            const resultItem = e.target.closest('.search-result-item');
            if (resultItem) {
                this.selectItem(resultItem, searchInput, hiddenItemIdInput, resultsContainer, onItemSelect);
            }
        });

        console.log('ASNItemAutocomplete: Initialized for', inputId);
    }

    /**
     * Search items with debouncing
     */
    async searchItems(query, resultsContainer, hiddenItemIdInput, onItemSelect) {
        if (!query || query.length < this.minSearchLength) {
            this.hideResults(resultsContainer);
            return;
        }

        try {
            // Get current Purchase Order ID from global context
            const poId = window.asnManager?.selectedPurchaseOrderId || null;
            console.log('ASNItemAutocomplete: Searching for:', query, 'Purchase Order ID:', poId);
            
            const url = poId 
                ? `/api/asn/items/search?q=${encodeURIComponent(query)}&purchaseOrderId=${poId}`
                : `/api/asn/items/search?q=${encodeURIComponent(query)}`;
                
            const response = await fetch(url);
            const items = await response.json();

            if (items && items.length > 0) {
                this.displayResults(items, resultsContainer);
            } else {
                this.hideResults(resultsContainer);
            }
        } catch (error) {
            console.error('ASNItemAutocomplete: Search error:', error);
            this.hideResults(resultsContainer);
        }
    }

    /**
     * Load top 3 items when focus without input
     */
    async loadTopItems(resultsContainer, hiddenItemIdInput, onItemSelect) {
        try {
            // Get current Purchase Order ID from global context
            const poId = window.asnManager?.selectedPurchaseOrderId || null;
            console.log('ASNItemAutocomplete: Loading top 3 items for Purchase Order ID:', poId);
            
            const url = poId 
                ? `/api/asn/items/top?purchaseOrderId=${poId}`
                : '/api/asn/items/top';
                
            const response = await fetch(url);
            const items = await response.json();
            
            if (items && items.length > 0) {
                this.displayResults(items, resultsContainer);
            } else {
                this.hideResults(resultsContainer);
            }
        } catch (error) {
            console.error('ASNItemAutocomplete: Error loading top items:', error);
            this.hideResults(resultsContainer);
        }
    }

    /**
     * Display search results - Simple format without icon
     */
    displayResults(items, resultsContainer) {
        const html = items.map(item => `
            <div class="search-result-item" data-item-id="${item.id}" data-item-code="${item.itemCode}" data-item-name="${item.name}" data-unit="${item.unit}" data-purchase-price="${item.purchasePrice}">
                ${item.itemCode} - ${item.name}
            </div>
        `).join('');

        resultsContainer.innerHTML = html;
        this.showResults(resultsContainer);
    }

    /**
     * Select an item from search results
     */
    selectItem(resultItem, searchInput, hiddenItemIdInput, resultsContainer, onItemSelect) {
        const itemId = resultItem.dataset.itemId;
        const itemCode = resultItem.dataset.itemCode;
        const itemName = resultItem.dataset.itemName;
        const unit = resultItem.dataset.unit;
        const purchasePrice = resultItem.dataset.purchasePrice;

        // Update input fields
        searchInput.value = `${itemCode} - ${itemName}`;
        if (hiddenItemIdInput) {
            hiddenItemIdInput.value = itemId;
        }

        // Hide results
        this.hideResults(resultsContainer);

        // Call callback if provided
        if (onItemSelect && typeof onItemSelect === 'function') {
            onItemSelect({
                id: itemId,
                itemCode: itemCode,
                name: itemName,
                unit: unit,
                purchasePrice: parseFloat(purchasePrice)
            });
        }

        console.log('ASNItemAutocomplete: Item selected:', itemCode, itemName);
    }

    /**
     * Show search results
     */
    showResults(resultsContainer) {
        resultsContainer.style.display = 'block';
    }

    /**
     * Hide search results
     */
    hideResults(resultsContainer) {
        // Add delay to allow click events to fire
        setTimeout(() => {
            resultsContainer.style.display = 'none';
        }, 150);
    }

    /**
     * Debounce function
     */
    debounce(func, wait) {
        return (...args) => {
            clearTimeout(this.debounceTimer);
            this.debounceTimer = setTimeout(() => func.apply(this, args), wait);
        };
    }

    /**
     * Clear search input and results
     */
    clearSearch(inputId, resultsId, itemIdInput) {
        const searchInput = document.getElementById(inputId);
        const resultsContainer = document.getElementById(resultsId);
        const hiddenItemIdInput = document.getElementById(itemIdInput);

        if (searchInput) searchInput.value = '';
        if (hiddenItemIdInput) hiddenItemIdInput.value = '';
        if (resultsContainer) this.hideResults(resultsContainer);
    }

    /**
     * Set selected item programmatically
     */
    setSelectedItem(inputId, resultsId, itemIdInput, item) {
        const searchInput = document.getElementById(inputId);
        const hiddenItemIdInput = document.getElementById(itemIdInput);

        if (searchInput) {
            searchInput.value = `${item.itemCode} - ${item.name}`;
        }
        if (hiddenItemIdInput) {
            hiddenItemIdInput.value = item.id;
        }
    }
}
