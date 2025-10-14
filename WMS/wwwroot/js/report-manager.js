/**
 * Report Manager - Report generation for Admin
 */
class ReportManager {
    constructor() {
        this.currentReportType = null;
    }

    showReportForm(reportType) {
        this.currentReportType = reportType;
        document.getElementById('reportType').value = reportType;
        document.getElementById('reportFormTitle').textContent = 
            reportType.charAt(0).toUpperCase() + reportType.slice(1) + ' Report';
        document.getElementById('reportFormCard').style.display = 'block';
        document.getElementById('reportResultsCard').style.display = 'none';
    }

    async generateReport() {
        const fromDate = document.getElementById('fromDate').value;
        const toDate = document.getElementById('toDate').value;

        if (!fromDate || !toDate) {
            alert('Please select date range');
            return;
        }

        try {
            const response = await fetch(
                `/api/report/${this.currentReportType}?fromDate=${fromDate}&toDate=${toDate}`
            );
            const result = await response.json();

            if (result.success) {
                this.displayReport(result.data);
            } else {
                alert('Error generating report');
            }
        } catch (error) {
            console.error('Error:', error);
            alert('Error generating report');
        }
    }

    displayReport(data) {
        const container = document.getElementById('reportResultsContainer');
        let html = `
            <div class="row mb-3">
                <div class="col-md-4">
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
                            <h3>${data.totalPurchaseOrders}</h3>
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
                        <th>Supplier</th>
                        <th>Status</th>
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
                        <td><span class="badge bg-primary">${line.type}</span></td>
                        <td>${line.supplierName}</td>
                        <td>${line.status}</td>
                        <td>${line.totalItems}</td>
                        <td>Rp ${line.totalAmount.toLocaleString()}</td>
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
                        <td>${line.totalItems}</td>
                        <td>Rp ${line.totalAmount.toLocaleString()}</td>
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
            alert('Please generate a report first');
            return;
        }

        const data = {
            reportType: this.currentReportType,
            fromDate: fromDate,
            toDate: toDate,
            format: format
        };

        try {
            const response = await fetch('/api/report/export', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `${this.currentReportType}_report.${format === 'excel' ? 'xlsx' : 'pdf'}`;
                a.click();
            } else {
                alert('Export feature not yet implemented');
            }
        } catch (error) {
            console.error('Error:', error);
            alert('Export feature not yet implemented');
        }
    }
}

