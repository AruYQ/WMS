namespace WMS.Configuration
{
    /// <summary>
    /// Configuration settings untuk company management
    /// </summary>
    public class CompanySettings
    {
        /// <summary>
        /// Default company untuk development/testing
        /// </summary>
        public DefaultCompanySettings DefaultCompany { get; set; } = new DefaultCompanySettings();

        /// <summary>
        /// Default admin user untuk setiap company baru
        /// </summary>
        public DefaultAdminSettings DefaultAdmin { get; set; } = new DefaultAdminSettings();

        /// <summary>
        /// Company limits
        /// </summary>
        public int DefaultMaxUsers { get; set; } = 5;
        public string DefaultSubscriptionPlan { get; set; } = "Free";
        public int DefaultSubscriptionDays { get; set; } = 30;

        /// <summary>
        /// Multi-tenancy settings
        /// </summary>
        public bool EnableCompanyIsolation { get; set; } = true;
        public bool AllowCrossTenantAccess { get; set; } = false;
        public bool RequireCompanySelection { get; set; } = true;
    }

    public class DefaultCompanySettings
    {
        public string Name { get; set; } = "Default Company";
        public string Code { get; set; } = "DEFAULT";
        public string Email { get; set; } = "admin@defaultcompany.com";
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class DefaultAdminSettings
    {
        public string Username { get; set; } = "admin";
        public string Email { get; set; } = "admin@defaultcompany.com";
        public string FullName { get; set; } = "System Administrator";
        public string Password { get; set; } = "admin123";
    }
}