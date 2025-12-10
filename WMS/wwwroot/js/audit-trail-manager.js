/**
 * Audit Trail Manager - View audit logs and user activities
 */
class AuditTrailManager {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 50;
        this.init();
    }

    async init() {
        console.log('AuditTrailManager: Initializing...');
        await this.loadFilterOptions(); // Load filter options first
        await this.loadStatistics();
        await this.loadAuditLogs();
    }

    async loadStatistics() {
        try {
            const fromDate = document.getElementById('filterFromDate').value;
            const toDate = document.getElementById('filterToDate').value;
            
            console.log('AuditTrailManager: Loading statistics from', fromDate, 'to', toDate);
            const response = await fetch(
                `/api/audittrail/statistics?fromDate=${fromDate}&toDate=${toDate}`
            );
            console.log('AuditTrailManager: Statistics response status:', response.status);
            
            const result = await response.json();
            console.log('AuditTrailManager: Statistics data:', result);

            if (result.success) {
                const stats = result.data;
                document.getElementById('totalActions').textContent = stats.totalActions || 0;
                document.getElementById('createActions').textContent = stats.createActions || 0;
                document.getElementById('updateActions').textContent = stats.updateActions || 0;
                document.getElementById('deleteActions').textContent = stats.deleteActions || 0;
            } else {
                console.error('AuditTrailManager: Error loading statistics:', result.message);
            }
        } catch (error) {
            console.error('AuditTrailManager: Exception loading statistics:', error);
        }
    }

    async loadAuditLogs() {
        try {
            const formData = new FormData(document.getElementById('auditFilterForm'));
            const params = new URLSearchParams();
            params.append('page', this.currentPage);
            params.append('pageSize', this.pageSize);
            
            if (formData.get('fromDate')) params.append('fromDate', formData.get('fromDate'));
            if (formData.get('toDate')) params.append('toDate', formData.get('toDate'));
            if (formData.get('action')) params.append('action', formData.get('action'));
            if (formData.get('module')) params.append('module', formData.get('module'));

            console.log('AuditTrailManager: Loading audit logs with params:', params.toString());
            const response = await fetch(`/api/audittrail?${params.toString()}`);
            console.log('AuditTrailManager: Response status:', response.status);
            
            if (!response.ok) {
                console.error('AuditTrailManager: HTTP error:', response.status, response.statusText);
                this.showError(`Error loading audit logs: ${response.status} ${response.statusText}`);
                const container = document.getElementById('auditLogsTableContainer');
                if (container) {
                    container.innerHTML = '<div class="text-center py-5"><p class="text-danger">Error loading audit logs</p></div>';
                }
                return;
            }
            
            const result = await response.json();
            console.log('AuditTrailManager: Response data:', result);
            console.log('AuditTrailManager: result.success:', result.success);
            console.log('AuditTrailManager: result.data:', result.data);
            
            if (result.success && result.data) {
                console.log('AuditTrailManager: result.data keys:', Object.keys(result.data));
                console.log('AuditTrailManager: result.data.items:', result.data.items);
                console.log('AuditTrailManager: result.data.Items:', result.data.Items);
                this.renderAuditLogsTable(result.data);
            } else {
                console.error('AuditTrailManager: Error in response:', result.message);
                this.showError(result.message || 'Error loading audit logs');
                const container = document.getElementById('auditLogsTableContainer');
                if (container) {
                    container.innerHTML = '<div class="text-center py-5"><p>No audit logs found</p></div>';
                }
            }
        } catch (error) {
            console.error('AuditTrailManager: Exception loading audit logs:', error);
            console.error('AuditTrailManager: Error stack:', error.stack);
            this.showError('Error loading audit logs: ' + error.message);
            const container = document.getElementById('auditLogsTableContainer');
            if (container) {
                container.innerHTML = '<div class="text-center py-5"><p class="text-danger">Error loading audit logs</p></div>';
            }
        }
    }

    renderAuditLogsTable(data) {
        const container = document.getElementById('auditLogsTableContainer');
        
        if (!container) {
            console.error('AuditTrailManager: Container element not found');
            return;
        }
        
        // Handle both camelCase (items) and PascalCase (Items) for compatibility
        // ASP.NET Core default JSON serializer uses camelCase, but some configs might use PascalCase
        const items = data.items || data.Items || [];
        const totalItems = data.totalItems || data.TotalItems || 0;
        const page = data.page || data.Page || 1;
        const totalPages = data.totalPages || data.TotalPages || 0;
        const hasPrevious = data.hasPrevious !== undefined ? data.hasPrevious : (data.HasPrevious !== undefined ? data.HasPrevious : false);
        const hasNext = data.hasNext !== undefined ? data.hasNext : (data.HasNext !== undefined ? data.HasNext : false);
        
        console.log('AuditTrailManager: renderAuditLogsTable - items count:', items.length);
        console.log('AuditTrailManager: renderAuditLogsTable - totalItems:', totalItems);
        console.log('AuditTrailManager: renderAuditLogsTable - page:', page, 'totalPages:', totalPages);
        
        if (!items || items.length === 0) {
            container.innerHTML = '<div class="text-center py-5"><p>No audit logs found</p></div>';
            return;
        }

        let html = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead>
                        <tr>
                            <th>Timestamp</th>
                            <th>User</th>
                            <th>Action</th>
                            <th>Module</th>
                            <th>Description</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        items.forEach(log => {
            // Handle both camelCase and PascalCase for log properties
            const logId = log.id || log.Id || 0;
            const logUsername = log.username || log.Username || '-';
            const logAction = log.action || log.Action || '-';
            const logModule = log.module || log.Module || '-';
            const logEntityDescription = log.entityDescription || log.EntityDescription || '-';
            const logTimestamp = log.timestamp || log.Timestamp;
            const logIsSuccess = log.isSuccess !== undefined ? log.isSuccess : (log.IsSuccess !== undefined ? log.IsSuccess : true);
            const logBadgeColor = log.badgeColor || log.BadgeColor || 'primary';

            const statusBadge = logIsSuccess 
                ? '<span class="badge bg-success">Success</span>'
                : '<span class="badge bg-danger">Failed</span>';

            const actionBadge = `<span class="badge bg-${logBadgeColor}">${logAction}</span>`;

            html += `
                <tr>
                    <td>${logTimestamp ? new Date(logTimestamp).toLocaleString() : '-'}</td>
                    <td>${logUsername}</td>
                    <td>${actionBadge}</td>
                    <td>${logModule}</td>
                    <td>${logEntityDescription}</td>
                    <td>${statusBadge}</td>
                    <td>
                        <button class="btn btn-sm btn-info" onclick="auditTrailManager.viewDetails(${logId})">
                            <i class="fas fa-eye"></i>
                        </button>
                    </td>
                </tr>
            `;
        });

        html += `
                    </tbody>
                </table>
            </div>
            <div class="d-flex justify-content-between align-items-center mt-3">
                <div>Showing ${items.length} of ${totalItems} entries</div>
                <div>
                    ${hasPrevious ? `<button class="btn btn-sm btn-primary" onclick="auditTrailManager.previousPage()">Previous</button>` : ''}
                    <span class="mx-2">Page ${page} of ${totalPages}</span>
                    ${hasNext ? `<button class="btn btn-sm btn-primary" onclick="auditTrailManager.nextPage()">Next</button>` : ''}
                </div>
            </div>
        `;

        container.innerHTML = html;
    }

    async viewDetails(id) {
        try {
            const response = await fetch(`/api/audittrail/${id}`);
            const result = await response.json();

            if (result.success) {
                const log = result.data;
                const container = document.getElementById('auditLogDetailsContainer');
                container.innerHTML = `
                    <div class="row">
                        <div class="col-md-6">
                            <p><strong>User:</strong> ${log.username}</p>
                            <p><strong>Action:</strong> ${log.action}</p>
                            <p><strong>Module:</strong> ${log.module}</p>
                            <p><strong>Timestamp:</strong> ${new Date(log.timestamp).toLocaleString()}</p>
                        </div>
                        <div class="col-md-6">
                            <p><strong>IP Address:</strong> ${log.ipAddress || '-'}</p>
                            <p><strong>Status:</strong> ${log.isSuccess ? 'Success' : 'Failed'}</p>
                            <p><strong>Entity:</strong> ${log.entityDescription || '-'}</p>
                        </div>
                    </div>
                    ${log.notes ? `<hr><p><strong>Notes:</strong> ${log.notes}</p>` : ''}
                `;
                new bootstrap.Modal(document.getElementById('viewAuditLogModal')).show();
            }
        } catch (error) {
            console.error('Error viewing details:', error);
        }
    }

    async applyFilters() {
        this.currentPage = 1;
        await this.loadStatistics();
        await this.loadAuditLogs();
    }

    previousPage() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.loadAuditLogs();
        }
    }

    nextPage() {
        this.currentPage++;
        this.loadAuditLogs();
    }

    /**
     * Load unique Actions and Modules from database for filter dropdowns
     */
    async loadFilterOptions() {
        try {
            console.log('AuditTrailManager: Loading filter options...');
            const response = await fetch('/api/audittrail/filter-options');
            const result = await response.json();

            if (result.success) {
                const { actions, modules } = result.data;
                
                // Populate Action dropdown
                const actionSelect = document.getElementById('filterAction');
                if (actionSelect) {
                    // Keep "All" option, then add unique actions
                    actionSelect.innerHTML = '<option value="">All</option>';
                    actions.forEach(action => {
                        const option = document.createElement('option');
                        option.value = action;
                        option.textContent = this.formatActionName(action);
                        actionSelect.appendChild(option);
                    });
                    console.log('AuditTrailManager: Populated Action dropdown with', actions.length, 'options');
                }

                // Populate Module dropdown
                const moduleSelect = document.getElementById('filterModule');
                if (moduleSelect) {
                    // Keep "All" option, then add unique modules
                    moduleSelect.innerHTML = '<option value="">All</option>';
                    modules.forEach(module => {
                        const option = document.createElement('option');
                        option.value = module;
                        option.textContent = this.formatModuleName(module);
                        moduleSelect.appendChild(option);
                    });
                    console.log('AuditTrailManager: Populated Module dropdown with', modules.length, 'options');
                }

                console.log('AuditTrailManager: Filter options loaded successfully', { actions, modules });
            } else {
                console.error('AuditTrailManager: Error loading filter options:', result.message);
            }
        } catch (error) {
            console.error('AuditTrailManager: Exception loading filter options:', error);
        }
    }

    /**
     * Format action names for display
     */
    formatActionName(action) {
        const actionMap = {
            'CREATE': 'Create',
            'UPDATE': 'Update',
            'DELETE': 'Delete',
            'VIEW': 'View',
            'CANCEL': 'Cancel',
            'PROCESS': 'Process',
            'CHANGE STATUS': 'Change Status',
            'EXPORT': 'Export',
            'SEND': 'Send',
            'RECEIVE': 'Receive',
            'SHIP': 'Ship',
            'COMPLETE': 'Complete',
            'APPROVE': 'Approve',
            'REJECT': 'Reject'
        };
        return actionMap[action] || action;
    }

    /**
     * Format module names for display
     */
    formatModuleName(module) {
        const moduleMap = {
            'PurchaseOrder': 'Purchase Order',
            'SalesOrder': 'Sales Order',
            'ASN': 'ASN (Receiving)',
            'Picking': 'Picking',
            'Putaway': 'Putaway',
            'Inventory': 'Inventory',
            'Item': 'Item',
            'Company': 'Company',
            'Customer': 'Customer',
            'Supplier': 'Supplier',
            'Location': 'Location',
            'User': 'User',
            'Role': 'Role',
            'Permission': 'Permission'
        };
        return moduleMap[module] || module;
    }

    showError(message) {
        alert(message);
    }
}

