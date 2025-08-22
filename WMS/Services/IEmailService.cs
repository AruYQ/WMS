namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk Email operations
    /// Handles email sending untuk Purchase Orders, notifications, dll
    /// </summary>
    public interface IEmailService
    {
        // Basic Email Operations
        Task<bool> SendEmailAsync(string to, string subject, string body);
        Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body);
        Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string attachmentName);

        // Purchase Order Email Operations
        Task<bool> SendPurchaseOrderEmailAsync(string supplierEmail, string poNumber, string emailContent);
        Task<string> GeneratePurchaseOrderEmailTemplateAsync(object poData);

        // Notification Email Operations
        Task<bool> SendLowStockNotificationAsync(string adminEmail, IEnumerable<object> lowStockItems);
        Task<bool> SendASNArrivalNotificationAsync(string adminEmail, object asnData);
        Task<bool> SendSalesOrderConfirmationAsync(string customerEmail, object soData);

        // Email Template Operations
        Task<string> GetEmailTemplateAsync(string templateName);
        Task<string> ProcessEmailTemplateAsync(string template, Dictionary<string, object> variables);

        // Email Validation Operations
        Task<bool> ValidateEmailAddressAsync(string email);
        Task<bool> IsEmailServiceAvailableAsync();

        // Email Configuration
        Task<bool> TestEmailConfigurationAsync();
        Task<Dictionary<string, object>> GetEmailServiceStatusAsync();
    }
}