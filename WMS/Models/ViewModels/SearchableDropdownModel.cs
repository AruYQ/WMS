// ViewModel untuk Searchable Dropdown Component

namespace WMS.Models.ViewModels
{
    /// <summary>
    /// Model untuk SearchableDropdown component
    /// </summary>
    public class SearchableDropdownModel
    {
        /// <summary>
        /// Entity type untuk search (supplier, customer, item, location, etc.)
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Field name untuk form binding
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Field ID untuk JavaScript
        /// </summary>
        public string FieldId { get; set; } = string.Empty;

        /// <summary>
        /// Placeholder text untuk input
        /// </summary>
        public string Placeholder { get; set; } = "Select an option...";

        /// <summary>
        /// Selected value (display text)
        /// </summary>
        public string? SelectedValue { get; set; }

        /// <summary>
        /// Selected ID value
        /// </summary>
        public string? SelectedId { get; set; }

        /// <summary>
        /// Apakah field required
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// Apakah allow clear selection
        /// </summary>
        public bool AllowClear { get; set; } = true;

        /// <summary>
        /// Apakah show dropdown button
        /// </summary>
        public bool ShowDropdown { get; set; } = true;

        /// <summary>
        /// Help text untuk user
        /// </summary>
        public string? HelpText { get; set; }

        /// <summary>
        /// Error message untuk validation
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// CSS class tambahan
        /// </summary>
        public string? CssClass { get; set; }

        /// <summary>
        /// Additional data untuk search configuration
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Generate field ID jika tidak disediakan
        /// </summary>
        public string GetFieldId()
        {
            return !string.IsNullOrEmpty(FieldId) ? FieldId : $"{EntityType}_{FieldName}";
        }
    }
}
