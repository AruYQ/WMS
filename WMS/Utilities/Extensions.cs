// Utilities/Extensions.cs
// Extension methods untuk aplikasi WMS

using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using WMS.Models;

namespace WMS.Utilities
{
    /// <summary>
    /// Extension methods untuk string
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Mengecek apakah string kosong atau null
        /// </summary>
        public static bool IsNullOrEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Mengecek apakah string kosong, null, atau hanya whitespace
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Memotong string jika melebihi panjang maksimum
        /// </summary>
        public static string Truncate(this string value, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength - suffix.Length) + suffix;
        }

        /// <summary>
        /// Mengubah string menjadi Title Case
        /// </summary>
        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
        }

        /// <summary>
        /// Membersihkan string dari karakter yang tidak diinginkan untuk kode
        /// </summary>
        public static string ToCleanCode(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return new string(value.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray()).ToUpper();
        }

        /// <summary>
        /// Format nomor telepon Indonesia
        /// </summary>
        public static string FormatPhoneNumber(this string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            // Remove all non-digits
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Format based on length
            return digits.Length switch
            {
                >= 11 when digits.StartsWith("62") => $"+62 {digits.Substring(2, 3)}-{digits.Substring(5, 4)}-{digits.Substring(9)}",
                >= 10 when digits.StartsWith("0") => $"{digits.Substring(0, 4)}-{digits.Substring(4, 4)}-{digits.Substring(8)}",
                _ => phoneNumber
            };
        }

        /// <summary>
        /// Mengubah string menjadi safe filename
        /// </summary>
        public static string ToSafeFileName(this string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return filename;

            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());
        }
    }

    /// <summary>
    /// Extension methods untuk DateTime
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Format tanggal Indonesia
        /// </summary>
        public static string ToIndonesianDate(this DateTime date)
        {
            return date.ToString(Constants.DATE_FORMAT);
        }

        /// <summary>
        /// Format tanggal dan waktu Indonesia
        /// </summary>
        public static string ToIndonesianDateTime(this DateTime dateTime)
        {
            return dateTime.ToString(Constants.DATETIME_FORMAT);
        }

        /// <summary>
        /// Mendapatkan deskripsi waktu relatif (seperti "2 jam lalu")
        /// </summary>
        public static string ToTimeAgo(this DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalDays >= 365)
                return $"{(int)(timeSpan.TotalDays / 365)} tahun lalu";
            if (timeSpan.TotalDays >= 30)
                return $"{(int)(timeSpan.TotalDays / 30)} bulan lalu";
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} hari lalu";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} jam lalu";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} menit lalu";

            return "Baru saja";
        }

        /// <summary>
        /// Mendapatkan awal hari (00:00:00)
        /// </summary>
        public static DateTime StartOfDay(this DateTime date)
        {
            return date.Date;
        }

        /// <summary>
        /// Mendapatkan akhir hari (23:59:59)
        /// </summary>
        public static DateTime EndOfDay(this DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Mendapatkan awal minggu (Senin)
        /// </summary>
        public static DateTime StartOfWeek(this DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Mendapatkan awal bulan
        /// </summary>
        public static DateTime StartOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        /// <summary>
        /// Mendapatkan akhir bulan
        /// </summary>
        public static DateTime EndOfMonth(this DateTime date)
        {
            return date.StartOfMonth().AddMonths(1).AddDays(-1);
        }

        /// <summary>
        /// Cek apakah tanggal adalah hari kerja
        /// </summary>
        public static bool IsWorkingDay(this DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
        }
    }

    /// <summary>
    /// Extension methods untuk decimal (currency dan number formatting)
    /// </summary>
    public static class DecimalExtensions
    {
        /// <summary>
        /// Format sebagai mata uang Indonesia
        /// </summary>
        public static string ToCurrency(this decimal amount)
        {
            return amount.ToString(Constants.CURRENCY_FORMAT);
        }

        /// <summary>
        /// Format sebagai mata uang dengan simbol kustom
        /// </summary>
        public static string ToCurrency(this decimal amount, string currencySymbol)
        {
            return $"{currencySymbol} {amount:N0}";
        }

        /// <summary>
        /// Format sebagai persentase
        /// </summary>
        public static string ToPercentage(this decimal value, int decimals = 1)
        {
            return (value * 100).ToString($"F{decimals}") + "%";
        }

        /// <summary>
        /// Format sebagai persentase dari double
        /// </summary>
        public static string ToPercentage(this double value, int decimals = 1)
        {
            return (value * 100).ToString($"F{decimals}") + "%";
        }

        /// <summary>
        /// Membulatkan ke ribuan terdekat
        /// </summary>
        public static decimal RoundToThousand(this decimal value)
        {
            return Math.Round(value / 1000) * 1000;
        }

        /// <summary>
        /// Cek apakah nilai positif
        /// </summary>
        public static bool IsPositive(this decimal value)
        {
            return value > 0;
        }

        /// <summary>
        /// Cek apakah nilai negatif
        /// </summary>
        public static bool IsNegative(this decimal value)
        {
            return value < 0;
        }
    }

    /// <summary>
    /// Extension methods untuk Enum
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Mendapatkan deskripsi dari enum menggunakan DescriptionAttribute
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();

            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        ///<summary>
        /// Mengubah enum menjadi list untuk dropdown
        /// </summary>
        public static List<(T Value, string Text)> ToSelectList<T>() where T : struct, Enum
        {
            return Enum.GetValues<T>()
                .Select(e => (e, e.GetDescription()))
                .ToList();
        }
    }

    /// <summary>
    /// Extension methods untuk IQueryable
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Pagination untuk IQueryable
        /// </summary>
        public static IQueryable<T> ToPaged<T>(this IQueryable<T> query, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = Constants.DEFAULT_PAGE_SIZE;

            return query.Skip((page - 1) * pageSize).Take(pageSize);
        }

        /// <summary>
        /// Conditional Where clause
        /// </summary>
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return condition ? query.Where(predicate) : query;
        }

        /// <summary>
        /// Dynamic ordering
        /// </summary>
        public static IQueryable<T> OrderByField<T>(this IQueryable<T> query, string fieldName, bool ascending = true)
        {
            if (string.IsNullOrEmpty(fieldName))
                return query;

            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
            var property = System.Linq.Expressions.Expression.Property(parameter, fieldName);
            var lambda = System.Linq.Expressions.Expression.Lambda(property, parameter);

            var methodName = ascending ? "OrderBy" : "OrderByDescending";
            var method = typeof(Queryable).GetMethods()
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .Single()
                .MakeGenericMethod(typeof(T), property.Type);

            return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda });
        }
    }

    /// <summary>
    /// Extension methods untuk IEnumerable
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Safe ForEach untuk IEnumerable
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Cek apakah collection kosong atau null
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
        {
            return source == null || !source.Any();
        }

        /// <summary>
        /// Chunk collection menjadi batch
        /// </summary>
        public static IEnumerable<IEnumerable<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value));
        }

        /// <summary>
        /// Distinct by property
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            var seen = new HashSet<TKey>();
            return source.Where(item => seen.Add(keySelector(item)));
        }
    }

    /// <summary>
    /// Extension methods untuk Model classes
    /// </summary>
    public static class ModelExtensions
    {
        /// <summary>
        /// Copy properties from one object to another
        /// </summary>
        public static void CopyPropertiesFrom<T>(this T target, T source) where T : class
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
            {
                var value = property.GetValue(source);
                property.SetValue(target, value);
            }
        }

        /// <summary>
        /// Convert object to JSON string
        /// </summary>
        public static string ToJson<T>(this T obj) where T : class
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// Convert JSON string to object
        /// </summary>
        public static T? FromJson<T>(this string json) where T : class
        {
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    /// <summary>
    /// Extension methods untuk BaseEntity
    /// </summary>
    public static class BaseEntityExtensions
    {
        /// <summary>
        /// Cek apakah entity baru dibuat (ID = 0)
        /// </summary>
        public static bool IsNew(this BaseEntity entity)
        {
            return entity.Id == 0;
        }

        /// <summary>
        /// Set audit fields untuk create
        /// </summary>
        public static void SetCreated(this BaseEntity entity, string createdBy = Constants.SYSTEM_USER)
        {
            entity.CreatedDate = DateTime.Now;
            entity.CreatedBy = createdBy;
        }

        /// <summary>
        /// Set audit fields untuk update
        /// </summary>
        public static void SetUpdated(this BaseEntity entity, string updatedBy = Constants.SYSTEM_USER)
        {
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedBy = updatedBy;
        }
    }

    /// <summary>
    /// Extension methods untuk Exception handling
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Get full exception message including inner exceptions
        /// </summary>
        public static string GetFullMessage(this Exception exception)
        {
            var messages = new List<string> { exception.Message };
            var innerException = exception.InnerException;

            while (innerException != null)
            {
                messages.Add(innerException.Message);
                innerException = innerException.InnerException;
            }

            return string.Join(" | ", messages);
        }

        /// <summary>
        /// Get exception details for logging
        /// </summary>
        public static string GetDetails(this Exception exception)
        {
            return $"Message: {exception.GetFullMessage()}\nStackTrace: {exception.StackTrace}";
        }
    }
}