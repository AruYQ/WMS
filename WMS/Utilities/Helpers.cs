// Utilities/Helpers.cs
// Helper classes dan utility functions untuk aplikasi WMS

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WMS.Models;

namespace WMS.Utilities
{
    /// <summary>
    /// Helper untuk generate nomor dokumen
    /// </summary>
    public static class DocumentNumberHelper
    {
        /// <summary>
        /// Generate nomor Purchase Order
        /// </summary>
        public static string GeneratePONumber(DateTime date, int sequence)
        {
            return string.Format(Constants.PO_NUMBER_FORMAT, date, sequence);
        }

        /// <summary>
        /// Generate nomor ASN
        /// </summary>
        public static string GenerateASNNumber(DateTime date, int sequence)
        {
            return string.Format(Constants.ASN_NUMBER_FORMAT, date, sequence);
        }

        /// <summary>
        /// Generate nomor Sales Order
        /// </summary>
        public static string GenerateSONumber(DateTime date, int sequence)
        {
            return string.Format(Constants.SO_NUMBER_FORMAT, date, sequence);
        }

        /// <summary>
        /// Parse nomor dokumen untuk mendapatkan tanggal dan sequence
        /// </summary>
        public static (DateTime Date, int Sequence) ParseDocumentNumber(string documentNumber, string prefix)
        {
            try
            {
                var parts = documentNumber.Split('-');
                if (parts.Length >= 3 && parts[0] == prefix)
                {
                    var dateStr = parts[1];
                    var sequenceStr = parts[2];

                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date) &&
                        int.TryParse(sequenceStr, out var sequence))
                    {
                        return (date, sequence);
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return (DateTime.Today, 1);
        }

        /// <summary>
        /// Validate format nomor dokumen
        /// </summary>
        public static bool ValidateDocumentNumber(string documentNumber, string expectedPrefix)
        {
            if (string.IsNullOrEmpty(documentNumber))
                return false;

            var pattern = $@"^{expectedPrefix}-\d{{8}}-\d{{3}}$";
            return Regex.IsMatch(documentNumber, pattern);
        }
    }

    /// <summary>
    /// Helper untuk validasi data
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validate email format
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate Indonesian phone number
        /// </summary>
        public static bool IsValidIndonesianPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove all non-digits
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Check various Indonesian phone number patterns
            return digits.Length >= 10 && digits.Length <= 15 &&
                   (digits.StartsWith("62") || digits.StartsWith("0"));
        }

        /// <summary>
        /// Validate location code format (A-01-01)
        /// </summary>
        public static bool IsValidLocationCode(string locationCode)
        {
            if (string.IsNullOrWhiteSpace(locationCode))
                return false;

            return Regex.IsMatch(locationCode, Constants.LOCATION_CODE_PATTERN);
        }

        /// <summary>
        /// Validate item code (no special characters, max length)
        /// </summary>
        public static bool IsValidItemCode(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode) || itemCode.Length > 50)
                return false;

            return Regex.IsMatch(itemCode, @"^[A-Za-z0-9\-_]+$");
        }

        /// <summary>
        /// Validate positive number
        /// </summary>
        public static bool IsPositiveNumber(decimal number)
        {
            return number > 0;
        }

        /// <summary>
        /// Validate quantity (non-negative integer)
        /// </summary>
        public static bool IsValidQuantity(int quantity)
        {
            return quantity >= 0;
        }

        /// <summary>
        /// Validate date range
        /// </summary>
        public static bool IsValidDateRange(DateTime fromDate, DateTime toDate)
        {
            return fromDate <= toDate;
        }
    }

    /// <summary>
    /// Helper untuk konversi dan formatting
    /// </summary>
    public static class FormatHelper
    {
        /// <summary>
        /// Format file size in bytes to human readable
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Format duration in minutes to human readable
        /// </summary>
        public static string FormatDuration(int minutes)
        {
            if (minutes < 60)
                return $"{minutes} menit";

            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;

            if (hours < 24)
            {
                return remainingMinutes == 0
                    ? $"{hours} jam"
                    : $"{hours} jam {remainingMinutes} menit";
            }

            var days = hours / 24;
            var remainingHours = hours % 24;

            var result = $"{days} hari";
            if (remainingHours > 0)
                result += $" {remainingHours} jam";

            return result;
        }

        /// <summary>
        /// Format quantity dengan unit
        /// </summary>
        public static string FormatQuantity(int quantity, string unit)
        {
            return $"{quantity:N0} {unit}";
        }

        /// <summary>
        /// Format percentage dengan warna untuk UI
        /// </summary>
        public static (string Text, string CssClass) FormatPercentageWithColor(double percentage)
        {
            var text = percentage.ToPercentage(1);
            var cssClass = percentage switch
            {
                >= 80 => "text-success",
                >= 60 => "text-warning",
                >= 40 => "text-info",
                _ => "text-danger"
            };

            return (text, cssClass);
        }

        /// <summary>
        /// Format status dengan badge CSS class
        /// </summary>
        public static (string Text, string CssClass) FormatStatusBadge(string status)
        {
            var cssClass = status.ToUpper() switch
            {
                "ACTIVE" or "AVAILABLE" or "COMPLETED" or "SUCCESS" => "badge bg-success",
                "PENDING" or "PROCESSING" or "WARNING" => "badge bg-warning",
                "INACTIVE" or "CANCELLED" or "ERROR" => "badge bg-danger",
                "DRAFT" or "NEW" => "badge bg-secondary",
                _ => "badge bg-light text-dark"
            };

            return (status, cssClass);
        }
    }

    /// <summary>
    /// Helper untuk kalkulasi bisnis
    /// </summary>
    public static class CalculationHelper
    {
        /// <summary>
        /// Calculate warehouse fee berdasarkan harga dan quantity
        /// </summary>
        public static decimal CalculateWarehouseFee(decimal unitPrice, int quantity)
        {
            return WarehouseFeeHelper.CalculateWarehouseFee(unitPrice, quantity);
        }

        /// <summary>
        /// Calculate total value dengan tax (jika diperlukan)
        /// </summary>
        public static decimal CalculateTotalWithTax(decimal subtotal, decimal taxPercentage = 0)
        {
            var taxAmount = subtotal * (taxPercentage / 100);
            return subtotal + taxAmount;
        }

        /// <summary>
        /// Calculate percentage change
        /// </summary>
        public static double CalculatePercentageChange(decimal oldValue, decimal newValue)
        {
            if (oldValue == 0)
                return newValue == 0 ? 0 : 100;

            return (double)((newValue - oldValue) / oldValue * 100);
        }

        /// <summary>
        /// Calculate average dari list nilai
        /// </summary>
        public static decimal CalculateAverage(IEnumerable<decimal> values)
        {
            if (!values.Any())
                return 0;

            return values.Average();
        }

        /// <summary>
        /// Calculate capacity utilization percentage
        /// </summary>
        public static double CalculateCapacityUtilization(int currentCapacity, int maxCapacity)
        {
            if (maxCapacity == 0)
                return 0;

            return (double)currentCapacity / maxCapacity * 100;
        }

        /// <summary>
        /// Calculate stock turnover ratio
        /// </summary>
        public static double CalculateStockTurnover(decimal costOfGoodsSold, decimal averageInventoryValue)
        {
            if (averageInventoryValue == 0)
                return 0;

            return (double)(costOfGoodsSold / averageInventoryValue);
        }

        /// <summary>
        /// Calculate days of inventory outstanding
        /// </summary>
        public static double CalculateDaysInventoryOutstanding(decimal averageInventoryValue, decimal costOfGoodsSold)
        {
            if (costOfGoodsSold == 0)
                return 0;

            return (double)(averageInventoryValue / costOfGoodsSold * 365);
        }
    }

    /// <summary>
    /// Helper untuk security dan hashing
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Generate MD5 hash untuk string
        /// </summary>
        public static string GenerateMD5Hash(string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate random string untuk reference number
        /// </summary>
        public static string GenerateRandomString(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Sanitize input untuk mencegah XSS
        /// </summary>
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Basic HTML encoding
            return System.Web.HttpUtility.HtmlEncode(input);
        }
    }

    /// <summary>
    /// Helper untuk file operations
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Get file extension dari filename
        /// </summary>
        public static string GetFileExtension(string filename)
        {
            return Path.GetExtension(filename)?.ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Check apakah file extension diizinkan
        /// </summary>
        public static bool IsAllowedFileExtension(string filename)
        {
            var extension = GetFileExtension(filename);
            var allowedExtensions = Constants.ALLOWED_FILE_EXTENSIONS.Split(',');

            return allowedExtensions.Contains(extension);
        }

        /// <summary>
        /// Generate unique filename untuk avoid collision
        /// </summary>
        public static string GenerateUniqueFileName(string originalFilename)
        {
            var extension = GetFileExtension(originalFilename);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilename);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            return $"{nameWithoutExtension}_{timestamp}{extension}";
        }

        /// <summary>
        /// Validate file size
        /// </summary>
        public static bool IsValidFileSize(long fileSizeInBytes)
        {
            var maxSizeInBytes = Constants.MAX_FILE_SIZE_MB * 1024 * 1024;
            return fileSizeInBytes <= maxSizeInBytes;
        }

        /// <summary>
        /// Get MIME type dari file extension
        /// </summary>
        public static string GetMimeType(string filename)
        {
            var extension = GetFileExtension(filename);

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".csv" => "text/csv",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }

    /// <summary>
    /// Helper untuk logging dan audit
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Create audit log entry
        /// </summary>
        public static string CreateAuditLogEntry(string action, string entityName, int entityId, string details, string userName = Constants.SYSTEM_USER)
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {userName}: {action} {entityName} (ID: {entityId}) - {details}";
        }

        /// <summary>
        /// Create error log entry
        /// </summary>
        public static string CreateErrorLogEntry(Exception exception, string context = "")
        {
            var contextInfo = string.IsNullOrEmpty(context) ? "" : $" Context: {context}";
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR{contextInfo} - {exception.GetFullMessage()}";
        }

        /// <summary>
        /// Create activity log entry untuk dashboard
        /// </summary>
        public static string CreateActivityLogEntry(string activityType, string description, string referenceNumber = "", string userName = Constants.SYSTEM_USER)
        {
            var refInfo = string.IsNullOrEmpty(referenceNumber) ? "" : $" Ref: {referenceNumber}";
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {userName}: {activityType} - {description}{refInfo}";
        }
    }

    /// <summary>
    /// Helper untuk email operations
    /// </summary>
    public static class EmailHelper
    {
        /// <summary>
        /// Validate multiple email addresses
        /// </summary>
        public static bool ValidateEmailList(string emailList, char separator = ';')
        {
            if (string.IsNullOrWhiteSpace(emailList))
                return false;

            var emails = emailList.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            return emails.All(email => ValidationHelper.IsValidEmail(email.Trim()));
        }

        /// <summary>
        /// Clean dan format email list
        /// </summary>
        public static string CleanEmailList(string emailList, char separator = ';')
        {
            if (string.IsNullOrWhiteSpace(emailList))
                return string.Empty;

            var emails = emailList.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(email => email.Trim())
                .Where(email => ValidationHelper.IsValidEmail(email))
                .Distinct();

            return string.Join(separator + " ", emails);
        }

        /// <summary>
        /// Create email subject dengan prefix
        /// </summary>
        public static string CreateEmailSubject(string documentType, string documentNumber, string action = "")
        {
            var actionText = string.IsNullOrEmpty(action) ? "" : $" - {action}";
            return $"[WMS] {documentType} {documentNumber}{actionText}";
        }

        /// <summary>
        /// Generate email signature
        /// </summary>
        public static string GenerateEmailSignature()
        {
            return $@"
<br/><br/>
---<br/>
<strong>Warehouse Management System</strong><br/>
Generated automatically on {DateTime.Now.ToIndonesianDateTime()}<br/>
Please do not reply to this email.
";
        }
    }

    /// <summary>
    /// Helper untuk database operations
    /// </summary>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Build SQL WHERE clause dari filter parameters
        /// </summary>
        public static (string WhereClause, Dictionary<string, object> Parameters) BuildWhereClause(Dictionary<string, object?> filters)
        {
            var conditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            foreach (var filter in filters.Where(f => f.Value != null))
            {
                var paramName = $"@{filter.Key}";
                conditions.Add($"{filter.Key} = {paramName}");
                parameters[paramName] = filter.Value!;
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
            return (whereClause, parameters);
        }

        /// <summary>
        /// Escape SQL string untuk mencegah injection (basic)
        /// </summary>
        public static string EscapeSqlString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace("'", "''");
        }

        /// <summary>
        /// Build ORDER BY clause
        /// </summary>
        public static string BuildOrderByClause(string sortField, string sortDirection = "ASC")
        {
            if (string.IsNullOrWhiteSpace(sortField))
                return "";

            var direction = sortDirection.ToUpper() == "DESC" ? "DESC" : "ASC";
            return $"ORDER BY {sortField} {direction}";
        }
    }

    /// <summary>
    /// Helper untuk chart dan reporting
    /// </summary>
    public static class ChartHelper
    {
        /// <summary>
        /// Generate colors untuk chart
        /// </summary>
        public static List<string> GetChartColors(int count)
        {
            var colors = new List<string>
            {
                "#007bff", "#28a745", "#ffc107", "#dc3545", "#17a2b8",
                "#6f42c1", "#e83e8c", "#fd7e14", "#20c997", "#6c757d"
            };

            var result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                result.Add(colors[i % colors.Count]);
            }

            return result;
        }

        /// <summary>
        /// Generate chart data untuk stock levels
        /// </summary>
        public static Dictionary<string, int> GenerateStockLevelData(List<Inventory> inventories)
        {
            var data = new Dictionary<string, int>
            {
                ["TINGGI"] = 0,
                ["SEDANG"] = 0,
                ["RENDAH"] = 0,
                ["KRITIS"] = 0,
                ["KOSONG"] = 0
            };

            foreach (var inventory in inventories)
            {
                var level = GetStockLevel(inventory.Quantity);
                data[level]++;
            }

            return data;
        }

        /// <summary>
        /// Get stock level kategori
        /// </summary>
        private static string GetStockLevel(int quantity)
        {
            if (quantity == 0) return "KOSONG";
            if (quantity <= Constants.CRITICAL_STOCK_THRESHOLD) return "KRITIS";
            if (quantity <= Constants.LOW_STOCK_THRESHOLD) return "RENDAH";
            if (quantity <= 50) return "SEDANG";
            return "TINGGI";
        }

        /// <summary>
        /// Format data untuk pie chart
        /// </summary>
        public static object FormatPieChartData(Dictionary<string, int> data)
        {
            var colors = GetChartColors(data.Count);

            return new
            {
                labels = data.Keys.ToArray(),
                datasets = new[]
                {
                    new
                    {
                        data = data.Values.ToArray(),
                        backgroundColor = colors.ToArray(),
                        borderWidth = 1
                    }
                }
            };
        }

        /// <summary>
        /// Format data untuk line chart
        /// </summary>
        public static object FormatLineChartData(string label, Dictionary<string, decimal> data)
        {
            return new
            {
                labels = data.Keys.ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label,
                        data = data.Values.ToArray(),
                        borderColor = "#007bff",
                        backgroundColor = "rgba(0, 123, 255, 0.1)",
                        fill = true
                    }
                }
            };
        }
    }

    /// <summary>
    /// Helper untuk export operations
    /// </summary>
    public static class ExportHelper
    {
        /// <summary>
        /// Get export filename dengan timestamp
        /// </summary>
        public static string GetExportFileName(string baseFileName, string extension = "xlsx")
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{baseFileName}_{timestamp}.{extension}";
        }

        /// <summary>
        /// Convert data ke CSV format
        /// </summary>
        public static string ConvertToCSV<T>(IEnumerable<T> data, bool includeHeader = true)
        {
            var sb = new StringBuilder();
            var properties = typeof(T).GetProperties();

            // Header
            if (includeHeader)
            {
                sb.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));
            }

            // Data rows
            foreach (var item in data)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item)?.ToString() ?? "";
                    return $"\"{value.Replace("\"", "\"\"")}\""; // Escape quotes
                });

                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sanitize data untuk export (remove HTML, special chars)
        /// </summary>
        public static string SanitizeForExport(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            // Remove HTML tags
            var noHtml = Regex.Replace(input, "<.*?>", "");

            // Replace common problematic characters
            return noHtml
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();
        }
    }

    /// <summary>
    /// Helper untuk performance monitoring
    /// </summary>
    public static class PerformanceHelper
    {
        /// <summary>
        /// Measure execution time untuk method
        /// </summary>
        public static async Task<(T Result, TimeSpan ExecutionTime)> MeasureExecutionTime<T>(Func<Task<T>> func)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await func();
            stopwatch.Stop();

            return (result, stopwatch.Elapsed);
        }

        /// <summary>
        /// Measure execution time untuk synchronous method
        /// </summary>
        public static (T Result, TimeSpan ExecutionTime) MeasureExecutionTime<T>(Func<T> func)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = func();
            stopwatch.Stop();

            return (result, stopwatch.Elapsed);
        }

        /// <summary>
        /// Log slow operations
        /// </summary>
        public static void LogSlowOperation(string operationName, TimeSpan executionTime, TimeSpan threshold)
        {
            if (executionTime > threshold)
            {
                var message = $"SLOW OPERATION: {operationName} took {executionTime.TotalMilliseconds:F2}ms (threshold: {threshold.TotalMilliseconds}ms)";
                // Log to your preferred logging system
                Console.WriteLine(message);
            }
        }
    }

    /// <summary>
    /// Helper untuk caching operations
    /// </summary>
    public static class CacheHelper
    {
        /// <summary>
        /// Generate cache key dengan parameters
        /// </summary>
        public static string GenerateCacheKey(string baseKey, params object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return baseKey;

            var paramString = string.Join("_", parameters.Select(p => p?.ToString() ?? "null"));
            return $"{baseKey}_{paramString}";
        }

        /// <summary>
        /// Check apakah cache masih valid berdasarkan timestamp
        /// </summary>
        public static bool IsCacheValid(DateTime cacheTimestamp, int expiryMinutes)
        {
            return DateTime.Now.Subtract(cacheTimestamp).TotalMinutes < expiryMinutes;
        }

        /// <summary>
        /// Generate cache tags untuk invalidation
        /// </summary>
        public static List<string> GenerateCacheTags(string entityType, int? entityId = null, params string[] additionalTags)
        {
            var tags = new List<string> { entityType };

            if (entityId.HasValue)
                tags.Add($"{entityType}_{entityId}");

            if (additionalTags != null)
                tags.AddRange(additionalTags);

            return tags;
        }
    }
}