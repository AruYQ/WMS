/**
 * Report Manager - Report generation for Admin
 */
class ReportManager {
    constructor() {
        this.currentReportType = null;
        this.selectedSupplierIds = [];
        this.allSuppliers = [];
        this.allCustomers = [];
        this.allItems = [];
        this.inventoryItemSearchInput = document.getElementById('inventoryItemSearch');
        this.isSuppliersLoaded = false;
        this.isCustomersLoaded = false;
        this.isItemsLoaded = false;

        if (this.inventoryItemSearchInput) {
            this.inventoryItemSearchInput.addEventListener('input', () => this.filterInventoryItems());
        }
    }

    async showReportForm(reportType) {
        this.currentReportType = reportType;
        document.getElementById('reportType').value = reportType;
        const titleMap = {
            inventory: 'Putaway & Picking',
            inbound: 'Inbound',
            outbound: 'Outbound',
            stock: 'Stock',
            supplier: 'Supplier',
            customer: 'Customer'
        };
        const displayTitle = titleMap[reportType] || (reportType.charAt(0).toUpperCase() + reportType.slice(1));
        document.getElementById('reportFormTitle').textContent = `${displayTitle} Report`;
        document.getElementById('reportFormCard').style.display = 'block';
        document.getElementById('reportResultsCard').style.display = 'none';

        document.querySelectorAll('[data-report-section]').forEach(section => {
            section.style.display = section.dataset.reportSection === reportType ? 'block' : 'none';
        });

        const loaders = [];
        if (reportType === 'inbound' || reportType === 'stock') {
            loaders.push(this.loadSuppliers());
        }
        if (reportType === 'outbound') {
            loaders.push(this.loadCustomers());
        }
        if (reportType === 'inventory' || reportType === 'stock') {
            loaders.push(this.loadItems());
        }

        await Promise.all(loaders);
    }

    async loadSuppliers(force = false) {
        if (!force && this.allSuppliers.length > 0) {
            this.refreshSupplierSelects();
            return;
        }

        try {
            const response = await fetch('/api/purchaseorder/suppliers');
            const result = await response.json();

            if (result.success && result.data) {
                this.allSuppliers = result.data;
                this.isSuppliersLoaded = true;
                this.refreshSupplierSelects();
            }
        } catch (error) {
            console.error('Error loading suppliers:', error);
        }
    }

    refreshSupplierSelects() {
        this.populateSelectOptions(
            document.getElementById('supplierId'),
            this.allSuppliers,
            supplier => ({ value: supplier.id, text: supplier.name })
        );
        this.populateSelectOptions(
            document.getElementById('stockSupplierId'),
            this.allSuppliers,
            supplier => ({ value: supplier.id, text: supplier.name })
        );
    }

    async loadCustomers(force = false) {
        if (!force && this.allCustomers.length > 0) {
            this.populateSelectOptions(
                document.getElementById('outboundCustomerId'),
                this.allCustomers,
                customer => ({ value: customer.id, text: `${customer.code || ''} ${customer.name}`.trim() })
            );
            return;
        }

        try {
            const response = await fetch('/api/customer?page=1&pageSize=1000');
            const result = await response.json();

            if (result.success && result.data) {
                this.allCustomers = result.data;
                this.isCustomersLoaded = true;
                this.populateSelectOptions(
                    document.getElementById('outboundCustomerId'),
                    this.allCustomers,
                    customer => ({ value: customer.id, text: `${customer.code || ''} ${customer.name}`.trim() })
                );
            }
        } catch (error) {
            console.error('Error loading customers:', error);
        }
    }

    async loadItems(force = false) {
        if (!force && this.allItems.length > 0) {
            this.filterInventoryItems();
            return;
        }

        try {
            const response = await fetch('/api/item?page=1&pageSize=1000');
            const result = await response.json();
            const success = result.success ?? result.Success;
            const data = result.data ?? result.Data;

            if (success && Array.isArray(data)) {
                this.allItems = data;
                this.isItemsLoaded = true;
                this.filterInventoryItems();
            }
        } catch (error) {
            console.error('Error loading items:', error);
        }
    }

    filterInventoryItems() {
        const select = document.getElementById('inventoryItemId');
        if (!select) return;

        const search = (this.inventoryItemSearchInput?.value || '').toLowerCase();
        const filteredItems = this.allItems.filter(item => {
            const code = (item.itemCode ?? item.ItemCode ?? '').toLowerCase();
            const name = (item.name ?? item.Name ?? '').toLowerCase();
            return !search || code.includes(search) || name.includes(search);
        });

        this.populateSelectOptions(select, filteredItems, item => ({
            value: item.id ?? item.Id,
            text: `${item.itemCode ?? item.ItemCode ?? ''} - ${item.name ?? item.Name ?? ''}`.trim()
        }));
    }

    populateSelectOptions(selectElement, items, getOption) {
        if (!selectElement || !items) return;
        const existingOptions = selectElement.querySelectorAll('option');
        existingOptions.forEach(opt => {
            if (opt.value !== '') {
                opt.remove();
            }
        });

        items.forEach(item => {
            const { value, text } = getOption(item);
            const option = document.createElement('option');
            option.value = value;
            option.textContent = text;
            selectElement.appendChild(option);
        });
    }

    showMultiSupplierModal() {
        const modal = new bootstrap.Modal(document.getElementById('multiSupplierModal'));
        this.renderSupplierMultiSelect();
        modal.show();
    }

    renderSupplierMultiSelect() {
        const container = document.getElementById('supplierMultiSelectList');
        container.innerHTML = this.allSuppliers.map(supplier => `
            <div class="form-check">
                <input class="form-check-input supplier-multi-check" type="checkbox" value="${supplier.id}" id="supplierCheck${supplier.id}" ${this.selectedSupplierIds.includes(supplier.id) ? 'checked' : ''}>
                <label class="form-check-label" for="supplierCheck${supplier.id}">
                    ${supplier.name} ${supplier.email ? `(${supplier.email})` : ''}
                </label>
            </div>
        `).join('');

        // Search functionality
        document.getElementById('supplierSearchInput').addEventListener('input', (e) => {
            const search = e.target.value.toLowerCase();
            container.querySelectorAll('.form-check').forEach(item => {
                const label = item.querySelector('label').textContent.toLowerCase();
                item.style.display = label.includes(search) ? 'block' : 'none';
            });
        });
    }

    confirmMultiSuppliers() {
        const checked = document.querySelectorAll('.supplier-multi-check:checked');
        this.selectedSupplierIds = Array.from(checked).map(cb => parseInt(cb.value));
        
        const display = document.getElementById('selectedSuppliers');
        if (this.selectedSupplierIds.length > 0) {
            const names = this.selectedSupplierIds.map(id => {
                const supplier = this.allSuppliers.find(s => s.id === id);
                return supplier ? supplier.name : '';
            }).filter(n => n).join(', ');
            display.textContent = `Selected: ${names}`;
            display.className = 'mt-2 small text-primary';
        } else {
            display.textContent = 'No suppliers selected';
            display.className = 'mt-2 small text-muted';
        }
        
        bootstrap.Modal.getInstance(document.getElementById('multiSupplierModal')).hide();
    }

    buildInboundRequestData(fromDate, toDate) {
        const requestData = {
            fromDate: fromDate,
            toDate: toDate
        };

        // Document types
        requestData.IncludePO = document.getElementById('includePO').checked;
        requestData.IncludeASN = document.getElementById('includeASN').checked;

        // Supplier filters
        const supplierId = document.getElementById('supplierId').value;
        if (supplierId) {
            requestData.SupplierId = parseInt(supplierId);
        }
        if (this.selectedSupplierIds.length > 0) {
            requestData.SupplierIds = this.selectedSupplierIds;
        }

        // Status filters
        const poStatuses = Array.from(document.querySelectorAll('.po-status:checked')).map(cb => cb.value);
        if (poStatuses.length > 0) {
            requestData.POStatuses = poStatuses;
        }

        const asnStatuses = Array.from(document.querySelectorAll('.asn-status:checked')).map(cb => cb.value);
        if (asnStatuses.length > 0) {
            requestData.ASNStatuses = asnStatuses;
        }

        requestData.IncludeCancelled = document.getElementById('includeCancelled').checked;

        // Document number filters
        const poNumberFilter = document.getElementById('poNumberFilter').value;
        if (poNumberFilter) requestData.PONumberFilter = poNumberFilter;

        const asnNumberFilter = document.getElementById('asnNumberFilter').value;
        if (asnNumberFilter) requestData.ASNNumberFilter = asnNumberFilter;

        // Date ranges
        const poFromDate = document.getElementById('poFromDate').value;
        if (poFromDate) requestData.POFromDate = poFromDate;
        const poToDate = document.getElementById('poToDate').value;
        if (poToDate) requestData.POToDate = poToDate;

        const asnFromDate = document.getElementById('asnFromDate').value;
        if (asnFromDate) requestData.ASNFromDate = asnFromDate;
        const asnToDate = document.getElementById('asnToDate').value;
        if (asnToDate) requestData.ASNToDate = asnToDate;

        // Amount ranges
        const minPOAmount = document.getElementById('minPOAmount').value;
        if (minPOAmount) requestData.MinPOAmount = parseFloat(minPOAmount);
        const maxPOAmount = document.getElementById('maxPOAmount').value;
        if (maxPOAmount) requestData.MaxPOAmount = parseFloat(maxPOAmount);

        const minASNAmount = document.getElementById('minASNAmount').value;
        if (minASNAmount) requestData.MinASNAmount = parseFloat(minASNAmount);
        const maxASNAmount = document.getElementById('maxASNAmount').value;
        if (maxASNAmount) requestData.MaxASNAmount = parseFloat(maxASNAmount);

        // Item count filters
        const minItemsCount = document.getElementById('minItemsCount').value;
        if (minItemsCount) requestData.MinItemsCount = parseInt(minItemsCount);
        const maxItemsCount = document.getElementById('maxItemsCount').value;
        if (maxItemsCount) requestData.MaxItemsCount = parseInt(maxItemsCount);

        // Sorting
        requestData.SortBy = document.getElementById('sortBy').value;
        const sortOrder = document.querySelector('input[name="sortOrder"]:checked');
        requestData.SortOrder = sortOrder ? sortOrder.value : 'ASC';

        return requestData;
    }

    buildOutboundRequestData(fromDate, toDate) {
        const requestData = {
            fromDate,
            toDate
        };

        const customerId = document.getElementById('outboundCustomerId')?.value;
        if (customerId) {
            requestData.CustomerId = parseInt(customerId);
        }

        const statuses = Array.from(document.querySelectorAll('.so-status:checked')).map(cb => cb.value);
        if (statuses.length > 0) {
            requestData.Statuses = statuses;
        }

        return requestData;
    }

    buildInventoryRequestData(fromDate, toDate) {
        const requestData = {
            fromDate,
            toDate,
            IncludePutaway: document.getElementById('includePutawayMovements').checked,
            IncludePicking: document.getElementById('includePickingMovements').checked
        };

        const itemId = document.getElementById('inventoryItemId')?.value;
        if (itemId) {
            requestData.ItemId = parseInt(itemId);
        }

        const itemSearch = document.getElementById('inventoryItemSearch')?.value;
        if (itemSearch) {
            requestData.ItemSearch = itemSearch;
        }

        return requestData;
    }

    buildStockRequestData(fromDate, toDate) {
        const requestData = {
            fromDate,
            toDate,
            IncludeZeroStock: document.getElementById('stockIncludeZero').checked
        };

        const supplierId = document.getElementById('stockSupplierId')?.value;
        if (supplierId) {
            requestData.SupplierId = parseInt(supplierId);
        }

        const category = document.getElementById('stockCategory')?.value;
        if (category) {
            requestData.Category = category;
        }

        const itemSearch = document.getElementById('stockItemSearch')?.value;
        if (itemSearch) {
            requestData.ItemSearch = itemSearch;
        }

        return requestData;
    }

    buildSupplierRequestData(fromDate, toDate) {
        const requestData = {
            fromDate,
            toDate
        };

        const status = document.getElementById('supplierStatus')?.value;
        if (status !== '') {
            requestData.SupplierIsActive = status === 'true';
        }

        const search = document.getElementById('supplierSearch')?.value;
        if (search) {
            requestData.SupplierSearch = search;
        }

        const sortBy = document.getElementById('supplierSortBy')?.value;
        if (sortBy) {
            requestData.SupplierSortBy = sortBy;
        }

        const sortOrder = document.querySelector('input[name="supplierSortOrder"]:checked')?.value;
        if (sortOrder) {
            requestData.SupplierSortOrder = sortOrder;
        }

        return requestData;
    }

    buildCustomerRequestData(fromDate, toDate) {
        const requestData = {
            fromDate,
            toDate
        };

        const status = document.getElementById('customerStatus')?.value;
        if (status !== '') {
            requestData.CustomerIsActive = status === 'true';
        }

        const customerType = document.getElementById('customerTypeFilter')?.value;
        if (customerType) {
            requestData.CustomerType = customerType;
        }

        const search = document.getElementById('customerSearch')?.value;
        if (search) {
            requestData.CustomerSearch = search;
        }

        const sortBy = document.getElementById('customerSortBy')?.value;
        if (sortBy) {
            requestData.CustomerSortBy = sortBy;
        }

        const sortOrder = document.querySelector('input[name="customerSortOrder"]:checked')?.value;
        if (sortOrder) {
            requestData.CustomerSortOrder = sortOrder;
        }

        return requestData;
    }

    async generateReport() {
        const fromDate = document.getElementById('fromDate').value;
        const toDate = document.getElementById('toDate').value;

        if (!fromDate || !toDate) {
            alert('Please select date range');
            return;
        }

        try {
            let requestData;
            let endpoint;

            switch (this.currentReportType) {
                case 'inbound':
                    requestData = this.buildInboundRequestData(fromDate, toDate);
                    endpoint = '/api/report/inbound';
                    break;
                case 'outbound':
                    requestData = this.buildOutboundRequestData(fromDate, toDate);
                    endpoint = '/api/report/outbound';
                    break;
                case 'inventory':
                    requestData = this.buildInventoryRequestData(fromDate, toDate);
                    endpoint = '/api/report/inventory-movement';
                    break;
                case 'stock':
                    requestData = this.buildStockRequestData(fromDate, toDate);
                    endpoint = '/api/report/stock';
                    break;
                case 'supplier':
                    requestData = this.buildSupplierRequestData(fromDate, toDate);
                    endpoint = '/api/report/supplier';
                    break;
                case 'customer':
                    requestData = this.buildCustomerRequestData(fromDate, toDate);
                    endpoint = '/api/report/customer';
                    break;
                default:
                    alert('Unknown report type');
                    return;
            }

            const response = await fetch(endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestData)
            });

            const result = await response.json();
            if (!response.ok) {
                throw new Error(result?.message || 'Server error');
            }

            const success = result.success ?? result.Success;
            const data = result.data ?? result.Data;

            if (success && data) {
                this.displayReport(data);
            } else {
                alert('Error generating report: ' + (result?.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error:', error);
            alert('Error generating report');
        }
    }
    resetForm() {
        document.getElementById('reportForm').reset();
        // Reset dates to default (last 30 days)
        const today = new Date();
        const lastMonth = new Date(today);
        lastMonth.setDate(lastMonth.getDate() - 30);
        document.getElementById('toDate').valueAsDate = today;
        document.getElementById('fromDate').valueAsDate = lastMonth;
        
        // Reset document type checkboxes
        document.getElementById('includePO').checked = true;
        document.getElementById('includeASN').checked = true;

        // Reset status checkboxes
        document.querySelectorAll('.po-status, .asn-status, .so-status').forEach(cb => cb.checked = false);
        document.getElementById('includeCancelled').checked = false;
        const outboundPickings = document.getElementById('includeOutboundPickings');
        if (outboundPickings) outboundPickings.checked = false;
        
        // Reset multi-selects
        this.selectedSupplierIds = [];
        const supplierDisplay = document.getElementById('selectedSuppliers');
        if (supplierDisplay) {
            supplierDisplay.textContent = 'No suppliers selected';
            supplierDisplay.className = 'mt-2 small text-muted';
        }
        
        // Reset sorting
        document.getElementById('sortBy').value = 'Date';
        document.getElementById('sortOrderASC').checked = true;

        // Reset dropdowns
        const supplierSelect = document.getElementById('supplierId');
        if (supplierSelect) supplierSelect.value = '';
        const outboundCustomerSelect = document.getElementById('outboundCustomerId');
        if (outboundCustomerSelect) outboundCustomerSelect.value = '';
        const inventoryItemSelect = document.getElementById('inventoryItemId');
        if (inventoryItemSelect) inventoryItemSelect.value = '';
        const stockSupplierSelect = document.getElementById('stockSupplierId');
        if (stockSupplierSelect) stockSupplierSelect.value = '';
        const stockCategorySelect = document.getElementById('stockCategory');
        if (stockCategorySelect) stockCategorySelect.value = '';

        // Reset search inputs
        if (this.inventoryItemSearchInput) {
            this.inventoryItemSearchInput.value = '';
        }
        const stockItemSearch = document.getElementById('stockItemSearch');
        if (stockItemSearch) stockItemSearch.value = '';
        const stockIncludeZero = document.getElementById('stockIncludeZero');
        if (stockIncludeZero) stockIncludeZero.checked = false;

        const includePutawayMovements = document.getElementById('includePutawayMovements');
        if (includePutawayMovements) includePutawayMovements.checked = true;
        const includePickingMovements = document.getElementById('includePickingMovements');
        if (includePickingMovements) includePickingMovements.checked = true;

        // Reset Supplier Report filters
        const supplierStatus = document.getElementById('supplierStatus');
        if (supplierStatus) supplierStatus.value = '';
        const supplierSearch = document.getElementById('supplierSearch');
        if (supplierSearch) supplierSearch.value = '';
        const supplierSortBy = document.getElementById('supplierSortBy');
        if (supplierSortBy) supplierSortBy.value = 'Name';
        const supplierSortOrderASC = document.getElementById('supplierSortOrderASC');
        if (supplierSortOrderASC) supplierSortOrderASC.checked = true;

        // Reset Customer Report filters
        const customerStatus = document.getElementById('customerStatus');
        if (customerStatus) customerStatus.value = '';
        const customerTypeFilter = document.getElementById('customerTypeFilter');
        if (customerTypeFilter) customerTypeFilter.value = '';
        const customerSearch = document.getElementById('customerSearch');
        if (customerSearch) customerSearch.value = '';
        const customerSortBy = document.getElementById('customerSortBy');
        if (customerSortBy) customerSortBy.value = 'Name';
        const customerSortOrderASC = document.getElementById('customerSortOrderASC');
        if (customerSortOrderASC) customerSortOrderASC.checked = true;

        this.filterInventoryItems();
    }

    displayReport(data) {
        const container = document.getElementById('reportResultsContainer');
        let html = `
            <div class="row mb-3">
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Period</h6>
                            <p>${new Date(data.fromDate).toLocaleDateString()} - ${new Date(data.toDate).toLocaleDateString()}</p>
                        </div>
                    </div>
                </div>
        `;

        if (this.currentReportType === 'inbound') {
            html += `
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total POs</h6>
                            <h3>${data.totalPurchaseOrders || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total ASN</h6>
                            <h3>${data.totalASN || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Received</h6>
                            <h3>${data.totalReceived || 0}</h3>
                        </div>
                    </div>
                </div>
            </div>
            <div class="row mb-3">
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total PO Value</h6>
                            <h4>Rp ${(data.totalPOValue || 0).toLocaleString()}</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total ASN Value</h6>
                            <h4>Rp ${(data.totalASNValue || 0).toLocaleString()}</h4>
                        </div>
                    </div>
                </div>
            </div>
            <table class="table table-bordered table-striped">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Document</th>
                        <th>Type</th>
                        <th>Supplier</th>
                        <th>Status</th>
                        <th>QTY</th>
                        <th>Items</th>
                        <th>Amount/Details</th>
                    </tr>
                </thead>
                <tbody>
            `;
            data.lines.forEach(line => {
                let typeBadgeClass = 'bg-primary';
                if (line.type === 'ASN') typeBadgeClass = 'bg-info';
                // Putaway removed - Inbound Report only shows PO and ASN
                
                let qty = '';
                let items = '';
                let amountDetails = '';
                
                if (line.type === 'PO') {
                    qty = line.totalQuantity || 0;
                    items = line.totalItems || 0;
                    amountDetails = `Rp ${(line.totalAmount || 0).toLocaleString()}`;
                } else if (line.type === 'ASN') {
                    qty = line.totalQuantityASN || 0;
                    items = line.totalItemsASN || 0;
                    amountDetails = `Rp ${(line.totalAmountASN || 0).toLocaleString()}`;
                    if (line.pONumberForASN) {
                        amountDetails += `<br><small class="text-muted">PO: ${line.pONumberForASN}</small>`;
                    }
                } else {
                    qty = '-';
                    items = '-';
                    amountDetails = '-';
                }
                
                html += `
                    <tr>
                        <td>${new Date(line.date).toLocaleDateString()}</td>
                        <td>${line.documentNumber || '-'}</td>
                        <td><span class="badge ${typeBadgeClass}">${line.type}</span></td>
                        <td>${line.supplierName || '-'}</td>
                        <td>${line.status || '-'}</td>
                        <td>${qty}</td>
                        <td>${items}</td>
                        <td>${amountDetails}</td>
                    </tr>
                `;
            });
            html += '</tbody></table>';
        } else if (this.currentReportType === 'outbound') {
            html += `
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total SOs</h6>
                            <h3>${data.totalSalesOrders}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Value</h6>
                            <h3>Rp ${data.totalValue.toLocaleString()}</h3>
                        </div>
                    </div>
                </div>
            </div>
            <table class="table table-bordered">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Document</th>
                        <th>Type</th>
                        <th>Customer</th>
                        <th>Status</th>
                        <th>QTY</th>
                        <th>Items</th>
                        <th>Amount</th>
                    </tr>
                </thead>
                <tbody>
            `;
            data.lines.forEach(line => {
                html += `
                    <tr>
                        <td>${new Date(line.date).toLocaleDateString()}</td>
                        <td>${line.documentNumber}</td>
                        <td><span class="badge bg-success">${line.type}</span></td>
                        <td>${line.customerName}</td>
                        <td>${line.status}</td>
                        <td>${line.totalQuantity || 0}</td>
                        <td>${line.totalItems || 0}</td>
                        <td>Rp ${line.totalAmount.toLocaleString()}</td>
                    </tr>
                `;
            });
            html += '</tbody></table>';
        } else if (this.currentReportType === 'inventory') {
            html += `
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Movements</h6>
                            <h3>${data.totalMovements || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Putaway Qty</h6>
                            <h3>${data.totalPutawayQuantity || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Picking Qty</h6>
                            <h3>${data.totalPickingQuantity || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Distinct Items</h6>
                            <h3>${data.totalItemsInvolved || 0}</h3>
                        </div>
                    </div>
                </div>
            </div>
            <table class="table table-bordered table-striped">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Movement</th>
                        <th>Item</th>
                        <th>Quantity</th>
                        <th>Reference</th>
                    </tr>
                </thead>
                <tbody>
            `;
            (data.lines || []).forEach(line => {
                const badgeClass = line.movementType === 'Putaway'
                    ? 'bg-success'
                    : line.movementType === 'Picking'
                        ? 'bg-danger'
                        : 'bg-secondary';

                html += `
                    <tr>
                        <td>${new Date(line.date).toLocaleString()}</td>
                        <td><span class="badge ${badgeClass}">${line.movementType}</span></td>
                        <td>
                            <strong>${line.itemCode || '-'}</strong><br>
                            <small class="text-muted">${line.itemName || '-'}</small>
                        </td>
                        <td>${line.quantity ?? 0}</td>
                        <td>${line.reference || '-'}</td>
                    </tr>
                `;
            });
            html += '</tbody></table>';
        } else if (this.currentReportType === 'stock') {
            const totalInventoryValue = data.totalInventoryValue || 0;
            html += `
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Items</h6>
                            <h3>${data.totalDistinctItems || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Quantity</h6>
                            <h3>${data.totalQuantity || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Inventory Value</h6>
                            <h3>Rp ${totalInventoryValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Period</h6>
                            <p>${new Date(data.fromDate).toLocaleDateString()} - ${new Date(data.toDate).toLocaleDateString()}</p>
                        </div>
                    </div>
                </div>
            </div>
            <table class="table table-bordered table-striped">
                <thead>
                    <tr>
                        <th>Item</th>
                        <th>Unit</th>
                        <th>Total Quantity</th>
                        <th>Average Cost</th>
                        <th>Total Value</th>
                        <th>Locations</th>
                    </tr>
                </thead>
                <tbody>
            `;
            (data.lines || []).forEach(line => {
                const totalValue = line.totalValue || 0;
                const averageCost = line.averageCost || 0;
                const locations = (line.locations || []).join(', ') || '-';

                html += `
                    <tr>
                        <td>
                            <strong>${line.itemCode}</strong><br>
                            <small class="text-muted">${line.itemName}</small>
                        </td>
                        <td>${line.unit || '-'}</td>
                        <td>${line.totalQuantity ?? 0}</td>
                        <td>Rp ${averageCost.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                        <td>Rp ${totalValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                        <td>
                            <strong>${line.locationCount || 0}</strong> lokasi<br>
                            <small class="text-muted">${locations}</small>
                        </td>
                    </tr>
                `;
            });
            html += '</tbody></table>';
        } else if (this.currentReportType === 'supplier') {
            html += `
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Suppliers</h6>
                            <h3>${data.totalSuppliers || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Active Suppliers</h6>
                            <h3>${data.activeSuppliers || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Purchase Orders</h6>
                            <h3>${data.totalPurchaseOrders || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total PO Value</h6>
                            <h3>Rp ${(data.totalPOValue || 0).toLocaleString()}</h3>
                        </div>
                    </div>
                </div>
            </div>
            <div class="row mb-3">
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Items</h6>
                            <h3>${data.totalItems || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h6>Inactive Suppliers</h6>
                            <h3>${data.inactiveSuppliers || 0}</h3>
                        </div>
                    </div>
                </div>
            </div>
            <table class="table table-bordered table-striped">
                <thead>
                    <tr>
                        <th>Supplier Name</th>
                        <th>Code</th>
                        <th>Email</th>
                        <th>Phone</th>
                        <th>City</th>
                        <th>Status</th>
                        <th>Total PO</th>
                        <th>Total Items</th>
                        <th>Total PO Value</th>
                        <th>Last PO Date</th>
                    </tr>
                </thead>
                <tbody>
            `;
            (data.lines || []).forEach(line => {
                const statusBadge = line.isActive ? 'bg-success' : 'bg-secondary';
                html += `
                    <tr>
                        <td><strong>${line.supplierName || '-'}</strong></td>
                        <td>${line.code || '-'}</td>
                        <td>${line.email || '-'}</td>
                        <td>${line.phone || '-'}</td>
                        <td>${line.city || '-'}</td>
                        <td><span class="badge ${statusBadge}">${line.isActive ? 'Active' : 'Inactive'}</span></td>
                        <td>${line.totalPurchaseOrders || 0}</td>
                        <td>${line.totalItems || 0}</td>
                        <td>Rp ${(line.totalPOValue || 0).toLocaleString()}</td>
                        <td>${line.lastPODate ? new Date(line.lastPODate).toLocaleDateString() : '-'}</td>
                    </tr>
                `;
            });
            html += '</tbody></table>';
        } else if (this.currentReportType === 'customer') {
            html += `
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Customers</h6>
                            <h3>${data.totalCustomers || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Active Customers</h6>
                            <h3>${data.activeCustomers || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total Sales Orders</h6>
                            <h3>${data.totalSalesOrders || 0}</h3>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card">
                        <div class="card-body">
                            <h6>Total SO Value</h6>
                            <h3>Rp ${(data.totalSOValue || 0).toLocaleString()}</h3>
                        </div>
                    </div>
                </div>
            </div>
            <div class="row mb-3">
                <div class="col-md-4">
                    <div class="card">
                        <div class="card-body">
                            <h6>Inactive Customers</h6>
                            <h3>${data.inactiveCustomers || 0}</h3>
                        </div>
                    </div>
                </div>
            </div>
            <table class="table table-bordered table-striped">
                <thead>
                    <tr>
                        <th>Customer Name</th>
                        <th>Code</th>
                        <th>Email</th>
                        <th>Phone</th>
                        <th>City</th>
                        <th>Type</th>
                        <th>Status</th>
                        <th>Total SO</th>
                        <th>Total SO Value</th>
                        <th>Last SO Date</th>
                    </tr>
                </thead>
                <tbody>
            `;
            (data.lines || []).forEach(line => {
                const statusBadge = line.isActive ? 'bg-success' : 'bg-secondary';
                html += `
                    <tr>
                        <td><strong>${line.customerName || '-'}</strong></td>
                        <td>${line.code || '-'}</td>
                        <td>${line.email || '-'}</td>
                        <td>${line.phone || '-'}</td>
                        <td>${line.city || '-'}</td>
                        <td>${line.customerType || '-'}</td>
                        <td><span class="badge ${statusBadge}">${line.isActive ? 'Active' : 'Inactive'}</span></td>
                        <td>${line.totalSalesOrders || 0}</td>
                        <td>Rp ${(line.totalSOValue || 0).toLocaleString()}</td>
                        <td>${line.lastSODate ? new Date(line.lastSODate).toLocaleDateString() : '-'}</td>
                    </tr>
                `;
            });
            html += '</tbody></table>';
        }

        container.innerHTML = html;
        document.getElementById('reportResultsCard').style.display = 'block';
    }

    async exportReport(format) {
        const fromDate = document.getElementById('fromDate').value;
        const toDate = document.getElementById('toDate').value;

        if (!fromDate || !toDate || !this.currentReportType) {
            alert('Please select date range first');
            return;
        }

        // Build request data sesuai report type
        let requestData;
        switch (this.currentReportType) {
            case 'inbound':
                requestData = this.buildInboundRequestData(fromDate, toDate);
                break;
            case 'outbound':
                requestData = this.buildOutboundRequestData(fromDate, toDate);
                break;
            case 'inventory':
                requestData = this.buildInventoryRequestData(fromDate, toDate);
                break;
            case 'stock':
                requestData = this.buildStockRequestData(fromDate, toDate);
                break;
            case 'supplier':
                requestData = this.buildSupplierRequestData(fromDate, toDate);
                break;
            case 'customer':
                requestData = this.buildCustomerRequestData(fromDate, toDate);
                break;
            default:
                alert('Unknown report type');
                return;
        }
        
        requestData.reportType = this.currentReportType.charAt(0).toUpperCase() + this.currentReportType.slice(1);
        requestData.format = format;

        try {
            const response = await fetch('/api/report/export', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestData)
            });

            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `${this.currentReportType}_report_${new Date().toISOString().split('T')[0]}.${format === 'excel' ? 'xlsx' : 'pdf'}`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
            } else {
                const error = await response.json();
                alert('Export failed: ' + (error.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error:', error);
            alert('Error exporting report');
        }
    }
}

