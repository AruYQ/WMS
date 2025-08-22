using WMS.Services;
using WMS.Utilities;

namespace WMS.Services
{
    /// <summary>
    /// Service implementation untuk Warehouse Fee calculation
    /// Implements the business rules untuk warehouse fee berdasarkan actual price
    /// </summary>
    public class WarehouseFeeCalculator : IWarehouseFeeCalculator
    {
        // Fee tier constants
        private const decimal TIER_1_THRESHOLD = 1000000m;      // 1 juta
        private const decimal TIER_2_THRESHOLD = 10000000m;     // 10 juta

        private const decimal TIER_1_RATE = 0.05m;  // 5% untuk ≤ 1 juta
        private const decimal TIER_2_RATE = 0.03m;  // 3% untuk 1-10 juta
        private const decimal TIER_3_RATE = 0.01m;  // 1% untuk > 10 juta

        #region Basic Fee Calculation

        public decimal CalculateFeeRate(decimal actualPrice)
        {
            if (!IsValidPrice(actualPrice))
                return 0m;

            if (actualPrice <= TIER_1_THRESHOLD)
                return TIER_1_RATE;
            else if (actualPrice <= TIER_2_THRESHOLD)
                return TIER_2_RATE;
            else
                return TIER_3_RATE;
        }

        public decimal CalculateFeeAmount(decimal actualPrice)
        {
            if (!IsValidPrice(actualPrice))
                return 0m;

            var feeRate = CalculateFeeRate(actualPrice);
            return actualPrice * feeRate;
        }

        public decimal CalculateTotalFee(decimal actualPrice, int quantity)
        {
            if (!IsValidPrice(actualPrice) || quantity <= 0)
                return 0m;

            var feePerUnit = CalculateFeeAmount(actualPrice);
            return feePerUnit * quantity;
        }

        #endregion

        #region Fee Analysis

        public string GetFeeTier(decimal actualPrice)
        {
            if (!IsValidPrice(actualPrice))
                return "Invalid";

            if (actualPrice <= TIER_1_THRESHOLD)
                return "Tier 1";
            else if (actualPrice <= TIER_2_THRESHOLD)
                return "Tier 2";
            else
                return "Tier 3";
        }

        public string GetFeeTierDescription(decimal actualPrice)
        {
            var tier = GetFeeTier(actualPrice);
            var rate = CalculateFeeRate(actualPrice);

            return tier switch
            {
                "Tier 1" => $"Harga Rendah (≤ {TIER_1_THRESHOLD:C}) - Fee: {rate:P}",
                "Tier 2" => $"Harga Menengah ({TIER_1_THRESHOLD:C} - {TIER_2_THRESHOLD:C}) - Fee: {rate:P}",
                "Tier 3" => $"Harga Tinggi (> {TIER_2_THRESHOLD:C}) - Fee: {rate:P}",
                _ => "Invalid Price"
            };
        }

        public decimal GetFeeTierThreshold(int tier)
        {
            return tier switch
            {
                1 => TIER_1_THRESHOLD,
                2 => TIER_2_THRESHOLD,
                3 => decimal.MaxValue,
                _ => 0m
            };
        }

        public IEnumerable<object> GetAllFeeTiers()
        {
            return new[]
            {
                new
                {
                    Tier = 1,
                    Name = "Tier 1 - Harga Rendah",
                    PriceRange = $"≤ {TIER_1_THRESHOLD:C}",
                    FeeRate = TIER_1_RATE,
                    FeePercentage = $"{TIER_1_RATE:P}",
                    Description = "Barang dengan harga rendah dikenakan fee tertinggi"
                },
                new
                {
                    Tier = 2,
                    Name = "Tier 2 - Harga Menengah",
                    PriceRange = $"{TIER_1_THRESHOLD:C} - {TIER_2_THRESHOLD:C}",
                    FeeRate = TIER_2_RATE,
                    FeePercentage = $"{TIER_2_RATE:P}",
                    Description = "Barang dengan harga menengah dikenakan fee sedang"
                },
                new
                {
                    Tier = 3,
                    Name = "Tier 3 - Harga Tinggi",
                    PriceRange = $"> {TIER_2_THRESHOLD:C}",
                    FeeRate = TIER_3_RATE,
                    FeePercentage = $"{TIER_3_RATE:P}",
                    Description = "Barang dengan harga tinggi dikenakan fee terendah"
                }
            };
        }

        #endregion

        #region Validation

        public bool IsValidPrice(decimal actualPrice)
        {
            return actualPrice > 0;
        }

        public bool IsPriceInTier(decimal actualPrice, int tier)
        {
            if (!IsValidPrice(actualPrice))
                return false;

            return tier switch
            {
                1 => actualPrice <= TIER_1_THRESHOLD,
                2 => actualPrice > TIER_1_THRESHOLD && actualPrice <= TIER_2_THRESHOLD,
                3 => actualPrice > TIER_2_THRESHOLD,
                _ => false
            };
        }

        #endregion

        #region Reporting

        public Dictionary<string, decimal> GetFeeStatistics(IEnumerable<decimal> prices)
        {
            var validPrices = prices.Where(IsValidPrice).ToList();

            if (!validPrices.Any())
            {
                return new Dictionary<string, decimal>
                {
                    ["TotalItems"] = 0,
                    ["AverageFeeRate"] = 0,
                    ["TotalFeeAmount"] = 0,
                    ["Tier1Count"] = 0,
                    ["Tier2Count"] = 0,
                    ["Tier3Count"] = 0
                };
            }

            var tier1Items = validPrices.Count(p => IsPriceInTier(p, 1));
            var tier2Items = validPrices.Count(p => IsPriceInTier(p, 2));
            var tier3Items = validPrices.Count(p => IsPriceInTier(p, 3));

            return new Dictionary<string, decimal>
            {
                ["TotalItems"] = validPrices.Count,
                ["AverageFeeRate"] = CalculateAverageFeeRate(validPrices),
                ["TotalFeeAmount"] = validPrices.Sum(CalculateFeeAmount),
                ["AveragePrice"] = validPrices.Average(),
                ["MinPrice"] = validPrices.Min(),
                ["MaxPrice"] = validPrices.Max(),
                ["Tier1Count"] = tier1Items,
                ["Tier2Count"] = tier2Items,
                ["Tier3Count"] = tier3Items,
                ["Tier1Percentage"] = validPrices.Count > 0 ? (decimal)tier1Items / validPrices.Count * 100 : 0,
                ["Tier2Percentage"] = validPrices.Count > 0 ? (decimal)tier2Items / validPrices.Count * 100 : 0,
                ["Tier3Percentage"] = validPrices.Count > 0 ? (decimal)tier3Items / validPrices.Count * 100 : 0
            };
        }

        public decimal CalculateAverageFeeRate(IEnumerable<decimal> prices)
        {
            var validPrices = prices.Where(IsValidPrice).ToList();

            if (!validPrices.Any())
                return 0m;

            var totalFeeRate = validPrices.Sum(CalculateFeeRate);
            return totalFeeRate / validPrices.Count;
        }

        public Dictionary<string, int> GetPriceDistributionByTier(IEnumerable<decimal> prices)
        {
            var validPrices = prices.Where(IsValidPrice).ToList();

            return new Dictionary<string, int>
            {
                ["Tier1"] = validPrices.Count(p => IsPriceInTier(p, 1)),
                ["Tier2"] = validPrices.Count(p => IsPriceInTier(p, 2)),
                ["Tier3"] = validPrices.Count(p => IsPriceInTier(p, 3))
            };
        }

        #endregion

        #region Business Logic

        public decimal GetOptimalPriceForTier(int targetTier)
        {
            return targetTier switch
            {
                1 => TIER_1_THRESHOLD,
                2 => TIER_1_THRESHOLD + 1, // Just above tier 1
                3 => TIER_2_THRESHOLD + 1, // Just above tier 2
                _ => 0m
            };
        }

        public IEnumerable<object> GetFeeOptimizationSuggestions(decimal currentPrice)
        {
            if (!IsValidPrice(currentPrice))
            {
                return new[] { new { Suggestion = "Invalid price provided", Type = "Error" } };
            }

            var suggestions = new List<object>();
            var currentTier = GetFeeTier(currentPrice);
            var currentRate = CalculateFeeRate(currentPrice);
            var currentFee = CalculateFeeAmount(currentPrice);

            suggestions.Add(new
            {
                Type = "Current Status",
                Suggestion = $"Current price {currentPrice:C} is in {currentTier} with {currentRate:P} fee rate",
                FeeAmount = currentFee
            });

            // Suggest moving to lower fee tier if close to threshold
            if (IsPriceInTier(currentPrice, 1) && currentPrice >= TIER_1_THRESHOLD * 0.9m)
            {
                var suggestedPrice = TIER_1_THRESHOLD + 1;
                var newFee = CalculateFeeAmount(suggestedPrice);
                var savings = currentFee - newFee;

                suggestions.Add(new
                {
                    Type = "Tier Optimization",
                    Suggestion = $"Consider adjusting price to {suggestedPrice:C} to move to Tier 2 (3% fee)",
                    CurrentFee = currentFee,
                    NewFee = newFee,
                    PotentialSavings = savings,
                    SavingsPercentage = currentFee > 0 ? (savings / currentFee) * 100 : 0
                });
            }

            if (IsPriceInTier(currentPrice, 2) && currentPrice >= TIER_2_THRESHOLD * 0.9m)
            {
                var suggestedPrice = TIER_2_THRESHOLD + 1;
                var newFee = CalculateFeeAmount(suggestedPrice);
                var savings = currentFee - newFee;

                suggestions.Add(new
                {
                    Type = "Tier Optimization",
                    Suggestion = $"Consider adjusting price to {suggestedPrice:C} to move to Tier 3 (1% fee)",
                    CurrentFee = currentFee,
                    NewFee = newFee,
                    PotentialSavings = savings,
                    SavingsPercentage = currentFee > 0 ? (savings / currentFee) * 100 : 0
                });
            }

            // Suggest bulk optimization for high-volume items
            suggestions.Add(new
            {
                Type = "Volume Consideration",
                Suggestion = "For high-volume items, even small fee rate reductions can lead to significant savings",
                Recommendation = "Consider negotiating prices near tier thresholds for maximum efficiency"
            });

            return suggestions;
        }

        public decimal CalculateFeeImpact(decimal originalPrice, decimal newPrice, int quantity)
        {
            if (!IsValidPrice(originalPrice) || !IsValidPrice(newPrice) || quantity <= 0)
                return 0m;

            var originalTotalFee = CalculateTotalFee(originalPrice, quantity);
            var newTotalFee = CalculateTotalFee(newPrice, quantity);

            return originalTotalFee - newTotalFee; // Positive = savings, Negative = additional cost
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get fee rate as percentage string for display
        /// </summary>
        public string GetFeeRateDisplay(decimal actualPrice)
        {
            var rate = CalculateFeeRate(actualPrice);
            return $"{rate:P2}";
        }

        /// <summary>
        /// Get CSS class for styling based on fee tier
        /// </summary>
        public string GetFeeTierCssClass(decimal actualPrice)
        {
            if (!IsValidPrice(actualPrice))
                return "text-muted";

            return GetFeeTier(actualPrice) switch
            {
                "Tier 1" => "text-danger", // High fee - red
                "Tier 2" => "text-warning", // Medium fee - yellow
                "Tier 3" => "text-success", // Low fee - green
                _ => "text-muted"
            };
        }

        /// <summary>
        /// Calculate the break-even point for tier changes
        /// </summary>
        public object GetTierBreakEvenAnalysis(decimal currentPrice, int quantity)
        {
            if (!IsValidPrice(currentPrice) || quantity <= 0)
                return new { Error = "Invalid input parameters" };

            var currentTier = GetFeeTier(currentPrice);
            var currentFee = CalculateTotalFee(currentPrice, quantity);

            var analysis = new
            {
                CurrentPrice = currentPrice,
                CurrentTier = currentTier,
                CurrentTotalFee = currentFee,
                Quantity = quantity,
                TierThresholds = new
                {
                    Tier1Threshold = TIER_1_THRESHOLD,
                    Tier2Threshold = TIER_2_THRESHOLD,
                    Tier1ToTier2Savings = IsPriceInTier(currentPrice, 1) ?
                        CalculateFeeImpact(currentPrice, TIER_1_THRESHOLD + 1, quantity) : 0,
                    Tier2ToTier3Savings = IsPriceInTier(currentPrice, 2) ?
                        CalculateFeeImpact(currentPrice, TIER_2_THRESHOLD + 1, quantity) : 0
                },
                Recommendations = GetFeeOptimizationSuggestions(currentPrice)
            };

            return analysis;
        }

        #endregion
    }
}