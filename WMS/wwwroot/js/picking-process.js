// picking-process.js
// JavaScript untuk Process Picking UI

$(document).ready(function () {
    console.log('Picking Process JS loaded');

    // Initialize pick modal
    initializePickModal();

    // Auto-dismiss alerts
    setTimeout(function () {
        $('.alert').fadeOut('slow');
    }, 5000);
});

/**
 * Initialize pick item modal
 */
function initializePickModal() {
    // Handle click on Pick button
    $('.btn-pick').on('click', function () {
        const detailId = $(this).data('detail-id');
        const pickingId = $(this).data('picking-id');
        const itemId = $(this).data('item-id');
        const itemName = $(this).data('item-name');
        const locationId = $(this).data('location-id');
        const locationCode = $(this).data('location-code');
        const remaining = $(this).data('remaining');
        const unit = $(this).data('unit');

        console.log('Opening pick modal:', {
            detailId,
            pickingId,
            itemId,
            locationId,
            remaining
        });

        // Populate modal fields
        $('#pickingDetailId').val(detailId);
        $('#pickingId').val(pickingId);
        $('#itemId').val(itemId);
        $('#locationId').val(locationId);
        $('#modalItemName').text(itemName);
        $('#modalLocationCode').text(locationCode);
        $('#modalRemaining').text(remaining);
        $('#modalUnit').text(unit);

        // Set quantity input
        $('#quantityToPick').val(remaining);
        $('#quantityToPick').attr('max', remaining);

        // Show modal
        const pickModal = new bootstrap.Modal(document.getElementById('pickModal'));
        pickModal.show();

        // Focus on quantity input
        setTimeout(function () {
            $('#quantityToPick').focus().select();
        }, 500);
    });

    // Validate quantity on input
    $('#quantityToPick').on('input', function () {
        const value = parseInt($(this).val());
        const max = parseInt($(this).attr('max'));

        if (value > max) {
            $(this).val(max);
            showWarning(`Cannot pick more than ${max} units`);
        }

        if (value < 1) {
            $(this).val(1);
        }
    });

    // Handle form submission
    $('#pickForm').on('submit', function (e) {
        const quantity = parseInt($('#quantityToPick').val());
        const remaining = parseInt($('#modalRemaining').text());

        if (quantity <= 0 || quantity > remaining) {
            e.preventDefault();
            showError(`Invalid quantity. Must be between 1 and ${remaining}`);
            return false;
        }

        // Show loading state
        $(this).find('button[type="submit"]').prop('disabled', true).html(
            '<i class="fas fa-spinner fa-spin"></i> Processing...'
        );

        return true;
    });
}

/**
 * Show warning message
 */
function showWarning(message) {
    console.warn(message);
    
    // Create and show toast notification
    const toast = `
        <div class="toast align-items-center text-white bg-warning border-0 position-fixed top-0 end-0 m-3" role="alert" style="z-index: 9999;">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-exclamation-triangle me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    $('body').append(toast);
    const toastElement = $('.toast').last();
    const bsToast = new bootstrap.Toast(toastElement[0]);
    bsToast.show();
    
    setTimeout(function() {
        toastElement.remove();
    }, 3000);
}

/**
 * Show error message
 */
function showError(message) {
    console.error(message);
    
    // Create and show toast notification
    const toast = `
        <div class="toast align-items-center text-white bg-danger border-0 position-fixed top-0 end-0 m-3" role="alert" style="z-index: 9999;">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-exclamation-circle me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    $('body').append(toast);
    const toastElement = $('.toast').last();
    const bsToast = new bootstrap.Toast(toastElement[0]);
    bsToast.show();
    
    setTimeout(function() {
        toastElement.remove();
    }, 3000);
}

/**
 * Show success message
 */
function showSuccess(message) {
    console.log(message);
    
    // Create and show toast notification
    const toast = `
        <div class="toast align-items-center text-white bg-success border-0 position-fixed top-0 end-0 m-3" role="alert" style="z-index: 9999;">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-check-circle me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    $('body').append(toast);
    const toastElement = $('.toast').last();
    const bsToast = new bootstrap.Toast(toastElement[0]);
    bsToast.show();
    
    setTimeout(function() {
        toastElement.remove();
    }, 3000);
}

/**
 * Update row status after picking
 */
function updateRowStatus(detailId, picked, remaining) {
    const row = $(`.picking-row[data-detail-id="${detailId}"]`);
    
    if (row.length) {
        // Update picked quantity
        row.find('td:nth-child(5) strong').text(picked);
        
        // Update remaining quantity
        row.find('td:nth-child(6) strong').text(remaining);
        
        // Update status
        if (remaining === 0) {
            row.find('td:nth-child(7) span').removeClass('bg-secondary bg-warning').addClass('bg-success').text('Sudah Dipick');
            row.find('td:nth-child(8)').html('<span class="badge bg-success"><i class="fas fa-check"></i> Done</span>');
        } else if (picked > 0) {
            row.find('td:nth-child(7) span').removeClass('bg-secondary').addClass('bg-warning').text('Kurang');
        }
        
        // Highlight row briefly
        row.addClass('table-success');
        setTimeout(function() {
            row.removeClass('table-success');
        }, 2000);
    }
}

/**
 * Calculate totals
 */
function calculateTotals() {
    let totalRequired = 0;
    let totalPicked = 0;
    
    $('.picking-row').each(function() {
        const required = parseInt($(this).find('td:nth-child(4) strong').text()) || 0;
        const picked = parseInt($(this).find('td:nth-child(5) strong').text()) || 0;
        
        totalRequired += required;
        totalPicked += picked;
    });
    
    // Update footer totals
    $('tfoot th:nth-child(4)').text(totalRequired);
    $('tfoot th:nth-child(5)').text(totalPicked);
    $('tfoot th:nth-child(6)').text(totalRequired - totalPicked);
    
    // Update completion percentage
    const completion = totalRequired > 0 ? (totalPicked / totalRequired * 100).toFixed(1) : 0;
    $('.progress-bar').css('width', completion + '%').text(completion + '% Complete');
}

/**
 * Keyboard shortcuts
 */
$(document).on('keydown', function(e) {
    // ESC to close modal
    if (e.key === 'Escape' && $('#pickModal').hasClass('show')) {
        bootstrap.Modal.getInstance(document.getElementById('pickModal')).hide();
    }
    
    // Enter to submit when modal is open and quantity is focused
    if (e.key === 'Enter' && $('#quantityToPick').is(':focus')) {
        e.preventDefault();
        $('#pickForm').submit();
    }
});
