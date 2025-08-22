using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WMS.Services;

namespace WMS.Services
{
    /// <summary>
    /// Service implementation untuk Email operations
    /// Handles email sending menggunakan SMTP
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Load email configuration from appsettings.json
            _smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? "";
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
            _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "";
            _fromName = _configuration["EmailSettings:FromName"] ?? "WMS System";
            _enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");
        }

        #region Basic Email Operations

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            return await SendEmailAsync(new[] { to }, subject, body);
        }

       
        public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body)
        {
            if (!await IsEmailServiceAvailableAsync())
            {
                _logger.LogWarning("Email service is not available - missing configuration");
                return false;
            }

            SmtpClient client = null;
            MailMessage message = null;

            try
            {
                _logger.LogInformation($"Attempting to send email to: {string.Join(", ", to)}");
                _logger.LogInformation($"SMTP Config: Host={_smtpHost}, Port={_smtpPort}, SSL={_enableSsl}");

                // Create SMTP client with proper disposal
                client = new SmtpClient(_smtpHost, _smtpPort);

                // CRITICAL FIX: Proper SMTP configuration for Gmail
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _enableSsl;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Timeout = 30000; // 30 seconds timeout

                // Additional Gmail-specific settings
                if (_smtpHost.Contains("gmail"))
                {
                    client.Port = 587;
                    client.EnableSsl = true;
                    _logger.LogInformation("Applied Gmail-specific settings");
                }

                message = new MailMessage();
                message.From = new MailAddress(_fromEmail, _fromName);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = body.Contains("<html>") || body.Contains("<div>") || body.Contains("<p>");

                // Add recipients with validation
                var validEmails = new List<string>();
                foreach (var email in to)
                {
                    if (await ValidateEmailAddressAsync(email))
                    {
                        message.To.Add(new MailAddress(email));
                        validEmails.Add(email);
                        _logger.LogDebug($"Added valid email: {email}");
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid email address skipped: {email}");
                    }
                }

                if (message.To.Count == 0)
                {
                    _logger.LogWarning("No valid email addresses provided");
                    return false;
                }

                _logger.LogInformation($"Sending email to: {string.Join(", ", validEmails)}");

                // CRITICAL: Use Task.Run to prevent UI blocking and add timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var sendTask = Task.Run(async () =>
                {
                    await client.SendMailAsync(message);
                }, cts.Token);

                await sendTask;

                _logger.LogInformation($"Email sent successfully to {string.Join(", ", validEmails)}");
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Email sending was cancelled due to timeout");
                return false;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, $"SMTP Error - Code: {smtpEx.StatusCode}, Message: {smtpEx.Message}");

                // Log specific Gmail errors
                if (smtpEx.Message.Contains("authentication"))
                {
                    _logger.LogError("Gmail authentication failed - check app password");
                }
                else if (smtpEx.Message.Contains("timeout"))
                {
                    _logger.LogError("SMTP connection timed out");
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"General error sending email: {ex.Message}");
                return false;
            }
            finally
            {
                // Ensure proper disposal
                message?.Dispose();
                client?.Dispose();
            }
        }

        public async Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string attachmentName)
        {
            if (!await IsEmailServiceAvailableAsync())
            {
                _logger.LogWarning("Email service is not available");
                return false;
            }

            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort);

                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _enableSsl;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Timeout = 60000;

                using var message = new MailMessage();
                message.From = new MailAddress(_fromEmail, _fromName);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = body.Contains("<html>") || body.Contains("<div>") || body.Contains("<p>");

                if (!await ValidateEmailAddressAsync(to))
                {
                    _logger.LogWarning($"Invalid email address: {to}");
                    return false;
                }

                message.To.Add(new MailAddress(to));

                // Add attachment
                using var attachmentStream = new MemoryStream(attachment);
                using var emailAttachment = new Attachment(attachmentStream, attachmentName);
                message.Attachments.Add(emailAttachment);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email with attachment sent successfully to {to}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email with attachment to {to}");
                return false;
            }
        }

        #endregion

        #region Purchase Order Email Operations

        public async Task<bool> SendPurchaseOrderEmailAsync(string supplierEmail, string poNumber, string emailContent)
        {
            var subject = $"Purchase Order {poNumber} from {_fromName}";
            return await SendEmailAsync(supplierEmail, subject, emailContent);
        }

        public async Task<string> GeneratePurchaseOrderEmailTemplateAsync(object poData)
        {
            var template = await GetEmailTemplateAsync("PurchaseOrder");

            if (string.IsNullOrEmpty(template))
            {
                // Default template if no template file exists
                template = @"
Dear Supplier,

Please find below our Purchase Order details:

Purchase Order Number: {{PONumber}}
Order Date: {{OrderDate}}
Expected Delivery Date: {{ExpectedDeliveryDate}}
Total Amount: {{TotalAmount}}

Items Ordered:
{{ItemDetails}}

{{Notes}}

Please confirm receipt of this order and provide estimated delivery date.

Best regards,
{{CompanyName}}
";
            }

            var variables = new Dictionary<string, object>();

            // Convert poData object to dictionary for template processing
            foreach (var prop in poData.GetType().GetProperties())
            {
                variables[prop.Name] = prop.GetValue(poData)?.ToString() ?? "";
            }

            return await ProcessEmailTemplateAsync(template, variables);
        }

        #endregion

        #region Notification Email Operations

        public async Task<bool> SendLowStockNotificationAsync(string adminEmail, IEnumerable<object> lowStockItems)
        {
            var subject = "Low Stock Alert - Immediate Attention Required";

            var body = @"
<html>
<body>
<h2>Low Stock Alert</h2>
<p>The following items are running low on stock:</p>
<table border='1' style='border-collapse: collapse;'>
<tr>
    <th>Item Code</th>
    <th>Item Name</th>
    <th>Current Stock</th>
    <th>Location</th>
</tr>";

            foreach (var item in lowStockItems)
            {
                var itemType = item.GetType();
                var itemCode = itemType.GetProperty("ItemCode")?.GetValue(item)?.ToString() ?? "";
                var itemName = itemType.GetProperty("ItemName")?.GetValue(item)?.ToString() ?? "";
                var stock = itemType.GetProperty("CurrentStock")?.GetValue(item)?.ToString() ?? "";
                var location = itemType.GetProperty("LocationCode")?.GetValue(item)?.ToString() ?? "";

                body += $@"
<tr>
    <td>{itemCode}</td>
    <td>{itemName}</td>
    <td>{stock}</td>
    <td>{location}</td>
</tr>";
            }

            body += @"
</table>
<p>Please review and consider restocking these items.</p>
<br>
<p>Best regards,<br>WMS System</p>
</body>
</html>";

            return await SendEmailAsync(adminEmail, subject, body);
        }

        public async Task<bool> SendASNArrivalNotificationAsync(string adminEmail, object asnData)
        {
            var asnType = asnData.GetType();
            var asnNumber = asnType.GetProperty("ASNNumber")?.GetValue(asnData)?.ToString() ?? "";
            var supplierName = asnType.GetProperty("SupplierName")?.GetValue(asnData)?.ToString() ?? "";

            var subject = $"ASN Arrival Notification - {asnNumber}";
            var body = $@"
<html>
<body>
<h2>Advanced Shipping Notice Arrival</h2>
<p>The following ASN has arrived and is ready for processing:</p>
<ul>
    <li><strong>ASN Number:</strong> {asnNumber}</li>
    <li><strong>Supplier:</strong> {supplierName}</li>
    <li><strong>Arrival Date:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</li>
</ul>
<p>Please proceed with the putaway process.</p>
<br>
<p>Best regards,<br>WMS System</p>
</body>
</html>";

            return await SendEmailAsync(adminEmail, subject, body);
        }

        public async Task<bool> SendSalesOrderConfirmationAsync(string customerEmail, object soData)
        {
            var soType = soData.GetType();
            var soNumber = soType.GetProperty("SONumber")?.GetValue(soData)?.ToString() ?? "";
            var totalAmount = soType.GetProperty("TotalAmount")?.GetValue(soData)?.ToString() ?? "";
            var warehouseFee = soType.GetProperty("TotalWarehouseFee")?.GetValue(soData)?.ToString() ?? "";
            var grandTotal = soType.GetProperty("GrandTotal")?.GetValue(soData)?.ToString() ?? "";

            var subject = $"Sales Order Confirmation - {soNumber}";
            var body = $@"
<html>
<body>
<h2>Sales Order Confirmation</h2>
<p>Thank you for your order. Below are the details:</p>
<ul>
    <li><strong>Order Number:</strong> {soNumber}</li>
    <li><strong>Order Date:</strong> {DateTime.Now:dd/MM/yyyy}</li>
    <li><strong>Subtotal:</strong> {totalAmount}</li>
    <li><strong>Warehouse Fee:</strong> {warehouseFee}</li>
    <li><strong>Grand Total:</strong> {grandTotal}</li>
</ul>
<p>We will notify you once your order is ready for shipment.</p>
<br>
<p>Thank you for your business!<br>WMS Team</p>
</body>
</html>";

            return await SendEmailAsync(customerEmail, subject, body);
        }

        #endregion

        #region Email Template Operations

        public async Task<string> GetEmailTemplateAsync(string templateName)
        {
            try
            {
                var templatePath = Path.Combine("EmailTemplates", $"{templateName}.html");
                if (File.Exists(templatePath))
                {
                    return await File.ReadAllTextAsync(templatePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load email template: {templateName}");
            }

            return string.Empty;
        }

        public async Task<string> ProcessEmailTemplateAsync(string template, Dictionary<string, object> variables)
        {
            var processedTemplate = template;

            foreach (var variable in variables)
            {
                var placeholder = $"{{{{{variable.Key}}}}}";
                var value = variable.Value?.ToString() ?? "";
                processedTemplate = processedTemplate.Replace(placeholder, value);
            }

            return await Task.FromResult(processedTemplate);
        }

        #endregion

        #region Email Validation Operations

        public async Task<bool> ValidateEmailAddressAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                var isValid = emailRegex.IsMatch(email);
                return await Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating email address: {email}");
                return false;
            }
        }

        public async Task<bool> IsEmailServiceAvailableAsync()
        {
            // Check if all required email settings are configured
            var isConfigured = !string.IsNullOrEmpty(_smtpHost) &&
                              !string.IsNullOrEmpty(_smtpUsername) &&
                              !string.IsNullOrEmpty(_smtpPassword) &&
                              !string.IsNullOrEmpty(_fromEmail);

            return await Task.FromResult(isConfigured);
        }

        #endregion

        #region Email Configuration

        public async Task<bool> TestEmailConfigurationAsync()
        {
            try
            {
                if (!await IsEmailServiceAvailableAsync())
                    return false;

                using var client = new SmtpClient(_smtpHost, _smtpPort);

                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _enableSsl;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Timeout = 10000; // 10 seconds timeout

                _logger.LogInformation("Email service test passed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email service test failed");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetEmailServiceStatusAsync()
        {
            var status = new Dictionary<string, object>
            {
                ["IsConfigured"] = await IsEmailServiceAvailableAsync(),
                ["SmtpServer"] = _smtpHost,
                ["SmtpPort"] = _smtpPort,
                ["FromEmail"] = _fromEmail,
                ["EnableSsl"] = _enableSsl,
                ["LastTestResult"] = await TestEmailConfigurationAsync(),
                ["TestDate"] = DateTime.Now
            };

            return status;
        }

        #endregion
    }
}