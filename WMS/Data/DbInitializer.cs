using WMS.Models;
using Microsoft.EntityFrameworkCore;

namespace WMS.Data
{
    /// <summary>
    /// Helper class untuk initialize database dengan sample data
    /// </summary>
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Pastikan database sudah dibuat
            context.Database.EnsureCreated();

            // Check apakah sudah ada data
            if (context.Items.Any())
            {
                return; // Database sudah di-seed
            }

            // Seed akan dilakukan otomatis via OnModelCreating
            // Method ini bisa digunakan untuk seed data tambahan jika diperlukan
        }
    }
}