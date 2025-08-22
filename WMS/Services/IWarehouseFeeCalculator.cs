namespace WMS.Services
{
    /// <summary>
    /// Service interface untuk Warehouse Fee calculation
    /// Implements business rules untuk warehouse fee berdasarkan actual price
    /// Fee structure:
    /// - ≤ 1,000,000: 5%
    /// - > 1,000,000 ≤ 10,000,000: 3%  
    /// - > 10,000,000: 1%
    /// </summary>
    public interface IWarehouseFeeCalculator
    {
        // Basic Fee Calculation
        decimal CalculateFeeRate(decimal actualPrice);
        decimal CalculateFeeAmount(decimal actualPrice);
        decimal CalculateTotalFee(decimal actualPrice, int quantity);

        // Fee Analysis
        string GetFeeTier(decimal actualPrice);
        string GetFeeTierDescription(decimal actualPrice);
        decimal GetFeeTierThreshold(int tier);
        IEnumerable<object> GetAllFeeTiers();

        // Validation
        bool IsValidPrice(decimal actualPrice);
        bool IsPriceInTier(decimal actualPrice, int tier);

        // Reporting
        Dictionary<string, decimal> GetFeeStatistics(IEnumerable<decimal> prices);
        decimal CalculateAverageFeeRate(IEnumerable<decimal> prices);
        Dictionary<string, int> GetPriceDistributionByTier(IEnumerable<decimal> prices);

        // Business Logic
        decimal GetOptimalPriceForTier(int targetTier);
        IEnumerable<object> GetFeeOptimizationSuggestions(decimal currentPrice);
        decimal CalculateFeeImpact(decimal originalPrice, decimal newPrice, int quantity);
    }
}