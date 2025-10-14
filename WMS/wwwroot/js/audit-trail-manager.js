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
            
            const result = await response.json();
            console.log('AuditTrailManager: Response data:', result);

            if (result.success) {
                this.renderAuditLogsTable(result.data);
            } else {
                console.error('AuditTrailManager: Error in response:', result.message);
                this.showError(result.message || 'Error loading audit logs');
            }
        } catch (error) {
            console.error('AuditTrailManager: Exception loading audit logs:', error);
            this.showError('Error loading audit logs: ' + error.message);
        }
    }

    renderAuditLogsTable(data) {
        const container = document.getElementById('auditLogsTableContainer');
        
        if (!data.items || data.items.length === 0) {
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

        data.items.forEach(log => {
            const statusBadge = log.isSuccess 
                ? '<span class="badge bg-success">Success</span>'
                : '<span class="badge bg-danger">Failed</span>';

            const actionBadge = `<span class="badge bg-${log.badgeColor}">${log.action}</span>`;

            html += `
                <tr>
                    <td>${new Date(log.timestamp).toLocaleString()}</td>
                    <td>${log.username}</td>
                    <td>${actionBadge}</td>
                    <td>${log.module}</td>
                    <td>${log.entityDescription || '-'}</td>
                    <td>${statusBadge}</td>
                    <td>
                        <button class="btn btn-sm btn-info" onclick="auditTrailManager.viewDetails(${log.id})">
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
                <div>Showing ${data.items.length} of ${data.totalItems} entries</div>
                <div>
                    ${data.hasPrevious ? `<button class="btn btn-sm btn-primary" onclick="auditTrailManager.previousPage()">Previous</button>` : ''}
                    <span class="mx-2">Page ${data.page} of ${data.totalPages}</span>
                    ${data.hasNext ? `<button class="btn btn-sm btn-primary" onclick="auditTrailManager.nextPage()">Next</button>` : ''}
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

    showError(message) {
        alert(message);
    }
}

