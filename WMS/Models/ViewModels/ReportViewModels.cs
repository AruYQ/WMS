using System.ComponentModel.DataAnnotations;

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Base request for report generation
    /// </summary>
    public class ReportRequest
    {
        [Required]
        [Display(Name = "From Date")]
        public DateTime FromDate { get; set; } = DateTime.Now.AddMonths(-1);

        [Required]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; } = DateTime.Now;

        [Display(Name = "Format")]
        public string Format { get; set; } = "HTML"; // HTML, Excel, PDF
    }

    /// <summary>
    /// Inbound report (PO & ASN)
    /// </summary>
    public class InboundReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalPurchaseOrders { get; set; }
        public int TotalASN { get; set; }
        public int TotalReceived { get; set; }
        public decimal TotalValue { get; set; }
        public List<InboundReportLine> Lines { get; set; } = new();

        public class InboundReportLine
        {
            public DateTime Date { get; set; }
            public string DocumentNumber { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // PO or ASN
            public string SupplierName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int TotalItems { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }

    /// <summary>
    /// Outbound report (SO & Picking)
    /// </summary>
    public class OutboundReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalSalesOrders { get; set; }
        public int TotalPickings { get; set; }
        public int TotalShipped { get; set; }
        public decimal TotalValue { get; set; }
        public List<OutboundReportLine> Lines { get; set; } = new();

        public class OutboundReportLine
        {
            public DateTime Date { get; set; }
            public string DocumentNumber { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // SO or Picking
            public string CustomerName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int TotalItems { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }

    /// <summary>
    /// Inventory movement report
    /// </summary>
    public class InventoryMovementReportData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<InventoryMovementLine> Lines { get; set; } = new();

        public class InventoryMovementLine
        {
            public DateTime Date { get; set; }
            public string ItemCode { get; set; } = string.Empty;
            public string ItemName { get; set; } = string.Empty;
            public string LocationCode { get; set; } = string.Empty;
            public string MovementType { get; set; } = string.Empty; // IN, OUT, ADJUST
            public int Quantity { get; set; }
            public int BeforeQuantity { get; set; }
            public int AfterQuantity { get; set; }
            public string Reference { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// Report template info (for saved reports)
    /// </summary>
    public class ReportTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Export request for reports
    /// </summary>
    public class ReportExportRequest
    {
        [Required]
        public string ReportType { get; set; } = string.Empty; // Inbound, Outbound, InventoryMovement

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        [Required]
        public string Format { get; set; } = "Excel"; // Excel, PDF
    }
}

