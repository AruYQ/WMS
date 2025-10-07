let itemIndex = 0;
let items = [];

document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, initializing Sales Order form...');
    
    // Load items for dropdown
    loadItems();

    // Handle quantity and price changes for existing items
    document.addEventListener('input', function(e) {
        if (e.target.classList.contains('quantity-input') || e.target.classList.contains('price-input')) {
            const index = e.target.getAttribute('data-index');
            updateItemCalculation(index);
        }
    });

    updateDisplay();
});

async function loadItems() {
    try {
        const response = await fetch('/SalesOrder/GetAvailableItems');
        const data = await response.json();
        
        const itemSelect = document.getElementById('itemSelect');
        itemSelect.innerHTML = '<option value="">-- Select Item --</option>';
        
        data.forEach(item => {
            const option = document.createElement('option');
            option.value = item.id;
            option.textContent = `${item.itemCode} - ${item.itemName} (Stock: ${item.availableStock})`;
            option.dataset.unit = item.unit;
            option.dataset.stock = item.availableStock;
            option.dataset.standardPrice = item.standardPrice;
            itemSelect.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading items:', error);
        showError('Failed to load items. Please refresh the page.');
    }
}

function updateItemCalculation(index) {
    const quantityInput = document.querySelector(`input[name="Details[${index}].Quantity"]`);
    const priceInput = document.querySelector(`input[name="Details[${index}].UnitPrice"]`);
    const totalDisplay = document.querySelector(`.total-price-display[data-index="${index}"]`);

    if (quantityInput && priceInput && totalDisplay) {
        const quantity = parseFloat(quantityInput.value) || 0;
        const price = parseFloat(priceInput.value) || 0;
        const total = quantity * price;

        totalDisplay.textContent = formatCurrency(total);
        updateSummary();
    }
}

function addItem() {
    console.log('Attempting to add item...');

    const itemSelect = document.getElementById('itemSelect');
    const quantity = document.getElementById('quantity');
    const unitPrice = document.getElementById('unitPrice');
    const itemNotes = document.getElementById('itemNotes');

    // Clear previous validation
    document.querySelectorAll('#itemForm .form-control, #itemForm .form-select').forEach(input => {
        input.classList.remove('is-invalid');
    });

    // Validate required fields
    let isValid = true;
    let errorMessages = [];

    if (!itemSelect?.value) {
        itemSelect?.classList.add('is-invalid');
        errorMessages.push('Please select an item');
        isValid = false;
    }

    if (!quantity?.value || parseInt(quantity.value) <= 0) {
        quantity?.classList.add('is-invalid');
        errorMessages.push('Please enter a valid quantity');
        isValid = false;
    }

    if (!unitPrice?.value || parseFloat(unitPrice.value) <= 0) {
        unitPrice?.classList.add('is-invalid');
        errorMessages.push('Please enter a valid unit price');
        isValid = false;
    }

    if (!isValid) {
        showError('Validation failed: ' + errorMessages.join(', '));
        return;
    }

    // Check for duplicate items
    const selectedItemId = parseInt(itemSelect.value);
    const existingItem = items.find(item => item.itemId === selectedItemId);
    if (existingItem) {
        showError('This item is already added to the order.');
        itemSelect.classList.add('is-invalid');
        return;
    }

    // Check stock availability
    const availableStock = parseInt(itemSelect.selectedOptions[0].dataset.stock);
    const requestedQuantity = parseInt(quantity.value);
    
    if (requestedQuantity > availableStock) {
        showError(`Insufficient stock. Available: ${availableStock}, Requested: ${requestedQuantity}`);
        quantity.classList.add('is-invalid');
        return;
    }

    // Create new item object
    const newItem = {
        id: itemIndex++,
        itemId: selectedItemId,
        itemCode: itemSelect.selectedOptions[0].textContent.split(' - ')[0],
        itemName: itemSelect.selectedOptions[0].textContent.split(' - ')[1].split(' (Stock:')[0],
        itemUnit: itemSelect.selectedOptions[0].dataset.unit,
        quantity: parseInt(quantity.value),
        unitPrice: parseFloat(unitPrice.value),
        totalPrice: parseInt(quantity.value) * parseFloat(unitPrice.value),
        availableStock: availableStock,
        notes: itemNotes?.value || ''
    };

    console.log('Adding new item:', newItem);

        // Add item to array
        items.push(newItem);
        console.log('Items array after adding:', items);
        
        // Update display
        updateDisplay();

        // Close modal
        const modal = bootstrap.Modal.getInstance(document.getElementById('addItemModal'));
        if (modal) {
            modal.hide();
        }

        // Clear form
        clearItemForm();

        console.log('Item added successfully, total items:', items.length);
        showSuccess('Item added successfully!');
}

function removeItem(id) {
    console.log('Removing item with ID:', id);
    if (confirm('Are you sure you want to remove this item?')) {
        const itemsBefore = items.length;
        items = items.filter(item => item.id !== id);
        console.log('Items before:', itemsBefore, 'Items after:', items.length);
        
        updateDisplay();
        showSuccess('Item removed successfully!');
    }
}

function updateDisplay() {
    const tbody = document.getElementById('itemsTableBody');
    const noItemsMessage = document.getElementById('noItemsMessage');
    const itemsTableContainer = document.getElementById('itemsTableContainer');

    console.log('Updating display with', items.length, 'items');
    console.log('Items data:', items);
    console.log('Elements found:', {
        tbody: !!tbody,
        noItemsMessage: !!noItemsMessage,
        itemsTableContainer: !!itemsTableContainer
    });

    if (items.length === 0) {
        // Show no items message, hide table
        if (tbody) tbody.innerHTML = '';
        if (noItemsMessage) noItemsMessage.style.display = 'block';
        if (itemsTableContainer) itemsTableContainer.style.display = 'none';
    } else {
        // Hide no items message, show table
        if (noItemsMessage) noItemsMessage.style.display = 'none';
        if (itemsTableContainer) itemsTableContainer.style.display = 'block';

        if (tbody) {
            console.log('Rendering items to table...');
            tbody.innerHTML = items.map((item, index) => {
                console.log(`Rendering item ${index}:`, item);
                return `
                    <tr>
                        <td>
                            <div class="fw-medium">${item.itemCode}</div>
                            <div class="small text-muted">${item.itemName}</div>
                            <input type="hidden" name="Details[${index}].Id" value="${item.id}" />
                            <input type="hidden" name="Details[${index}].ItemId" value="${item.itemId}" />
                            <input type="hidden" name="Details[${index}].ItemCode" value="${item.itemCode}" />
                            <input type="hidden" name="Details[${index}].ItemName" value="${item.itemName}" />
                            <input type="hidden" name="Details[${index}].ItemUnit" value="${item.itemUnit}" />
                        </td>
                        <td>${item.itemUnit}</td>
                        <td class="text-end">
                            <input type="number" name="Details[${index}].Quantity" value="${item.quantity}" 
                                   class="form-control form-control-sm text-end quantity-input" data-index="${index}" min="1" step="1" />
                        </td>
                        <td class="text-end">
                            <div class="input-group input-group-sm">
                                <span class="input-group-text">Rp</span>
                                <input type="number" name="Details[${index}].UnitPrice" value="${item.unitPrice}" 
                                       class="form-control text-end price-input" data-index="${index}" min="0" step="0.01" />
                            </div>
                        </td>
                        <td class="text-end fw-medium text-success">
                            <span class="total-price-display" data-index="${index}">${formatCurrency(item.totalPrice)}</span>
                        </td>
                        <td class="text-center">
                            <span class="badge ${item.availableStock >= item.quantity ? 'bg-success' : 'bg-danger'}">
                                ${item.availableStock}
                            </span>
                        </td>
                        <td class="text-center">
                            <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeItem(${item.id})" title="Remove Item">
                                <i class="fas fa-trash"></i>
                            </button>
                            <input type="hidden" name="Details[${index}].Notes" value="${item.notes || ''}" />
                        </td>
                    </tr>
                `;
            }).join('');
            console.log('Table HTML generated:', tbody.innerHTML);
        }
    }

    updateSummary();
}

function updateSummary() {
    const totalQuantity = items.reduce((sum, item) => sum + (item.quantity || 0), 0);
    const totalAmount = items.reduce((sum, item) => sum + (item.totalPrice || 0), 0);

    // Update display elements
    document.getElementById('itemCount').textContent = items.length;
    document.getElementById('totalQuantity').textContent = formatNumber(totalQuantity);
    document.getElementById('totalAmount').textContent = formatCurrency(totalAmount);

    // Enable/disable save button
    const saveButton = document.getElementById('saveButton');
    if (saveButton) {
        saveButton.disabled = items.length === 0;
    }

    console.log('Summary updated - Total amount:', totalAmount, 'Total items:', items.length);
}

function clearForm() {
    if (confirm('Are you sure you want to clear the form? All data will be lost.')) {
        const form = document.getElementById('salesOrderForm');
        if (form) form.reset();

        items = [];
        itemIndex = 0;
        updateDisplay();
        showSuccess('Form cleared successfully!');
    }
}

function clearItemForm() {
    const elements = ['itemSelect', 'quantity', 'unitPrice', 'itemNotes'];
    elements.forEach(id => {
        const element = document.getElementById(id);
        if (element) element.value = '';
    });

    // Remove validation classes
    document.querySelectorAll('#itemForm .form-control, #itemForm .form-select').forEach(input => {
        input.classList.remove('is-invalid', 'is-valid');
    });

    // Hide stock info
    const stockInfo = document.getElementById('stockInfo');
    if (stockInfo) stockInfo.style.display = 'none';
}

// Auto-fill item details when item is selected
document.getElementById('itemSelect').addEventListener('change', function() {
    const selectedOption = this.selectedOptions[0];
    if (selectedOption && selectedOption.value) {
        const unit = selectedOption.dataset.unit;
        const stock = selectedOption.dataset.stock;
        const standardPrice = selectedOption.dataset.standardPrice;

        document.getElementById('itemUnit').value = unit || '';
        document.getElementById('unitPrice').value = standardPrice || '';

        // Show stock info
        const stockInfo = document.getElementById('stockInfo');
        const stockInfoText = document.getElementById('stockInfoText');
        if (stockInfo && stockInfoText) {
            stockInfoText.textContent = `Available stock: ${stock} ${unit}`;
            stockInfo.style.display = 'block';
            stockInfo.className = 'alert alert-info';
        }
    } else {
        clearItemForm();
    }
});

        // Form submission handler
        document.getElementById('salesOrderForm').addEventListener('submit', function(e) {
            console.log('Form submission attempted, items count:', items.length);

            if (items.length === 0) {
                e.preventDefault();
                showError('Please add at least one item to the order.');
                return false;
            }

            // Validate customer selection
            const customerId = document.getElementById('CustomerId')?.value;
            if (!customerId) {
                e.preventDefault();
                showError('Please select a customer.');
                return false;
            }

            // Add items to form data before submission
            console.log('Adding items to form data...');
            addItemsToForm();

            console.log('Form validation passed, submitting...');

            // Show loading state
            const submitBtn = document.getElementById('saveButton');
            if (submitBtn) {
                const originalText = submitBtn.innerHTML;
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Creating Order...';

                // Re-enable button after timeout (in case of error)
                setTimeout(() => {
                    if (submitBtn.disabled) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = originalText;
                    }
                }, 10000);
            }

            return true;
        });

        // Function to add items to form data
        function addItemsToForm() {
            const form = document.getElementById('salesOrderForm');
            if (!form) return;

            // Remove existing item inputs
            const existingInputs = form.querySelectorAll('input[name^="Details["]');
            existingInputs.forEach(input => input.remove());

            // Add items as hidden inputs
            items.forEach((item, index) => {
                const inputs = [
                    { name: `Details[${index}].Id`, value: item.id },
                    { name: `Details[${index}].ItemId`, value: item.itemId },
                    { name: `Details[${index}].ItemCode`, value: item.itemCode },
                    { name: `Details[${index}].ItemName`, value: item.itemName },
                    { name: `Details[${index}].ItemUnit`, value: item.itemUnit },
                    { name: `Details[${index}].Quantity`, value: item.quantity },
                    { name: `Details[${index}].UnitPrice`, value: item.unitPrice },
                    { name: `Details[${index}].Notes`, value: item.notes || '' }
                ];

                inputs.forEach(input => {
                    const hiddenInput = document.createElement('input');
                    hiddenInput.type = 'hidden';
                    hiddenInput.name = input.name;
                    hiddenInput.value = input.value;
                    form.appendChild(hiddenInput);
                });
            });

            console.log('Items added to form data:', items.length);
        }

function formatCurrency(amount) {
    if (isNaN(amount) || amount === null || amount === undefined) {
        amount = 0;
    }
    return new Intl.NumberFormat('id-ID', {
        style: 'currency',
        currency: 'IDR',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount);
}

function formatNumber(number) {
    if (isNaN(number) || number === null || number === undefined) {
        number = 0;
    }
    return new Intl.NumberFormat('id-ID').format(number);
}

        function showSuccess(message) {
            console.log('Success:', message);
            // You can implement a toast notification here
            // For now, just log to console
        }

        function showError(message) {
            console.error('Error:', message);
            // Create a more user-friendly error display
            const errorDiv = document.createElement('div');
            errorDiv.className = 'alert alert-danger alert-dismissible fade show';
            errorDiv.innerHTML = `
                <i class="fas fa-exclamation-triangle me-2"></i>
                <strong>Error:</strong> ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;
            
            // Insert at the top of the form
            const form = document.getElementById('salesOrderForm');
            if (form) {
                form.insertBefore(errorDiv, form.firstChild);
                
                // Auto-remove after 5 seconds
                setTimeout(() => {
                    if (errorDiv.parentNode) {
                        errorDiv.remove();
                    }
                }, 5000);
            }
        }
