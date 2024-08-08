using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly DapperContext _db;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "ProductAnalytics";

        public AnalyticsRepository(DapperContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IEnumerable<ProductAnalyticsResponseDTO>> GetProductAnalytics(int refreshDays)
        {
            if (!_cache.TryGetValue(CacheKey, out IEnumerable<ProductAnalyticsResponseDTO> analyticsList))
            {
                using (IDbConnection connection = _db.CreateConnection())
                {
                    connection.Open();

                    var query = @"
                        SELECT 
                            p.product_id_pkey AS ProductID,
                            p.product_name AS ProductName,
                            p.sku AS ProductSKU,
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 1 THEN ti.transaction_item_quantity ELSE 0 END), 0) AS UnitsBought,
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 2 THEN ti.transaction_item_quantity ELSE 0 END), 0) AS UnitsSold,
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 1 THEN ti.transaction_item_quantity * p.product_cost_price ELSE 0 END), 0) AS MoneySpent,
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 2 THEN ti.transaction_item_quantity * p.product_price ELSE 0 END), 0) AS MoneyEarned
                        FROM product p
                        LEFT JOIN transaction_item ti ON p.product_id_pkey = ti.product_id
                        LEFT JOIN transaction t ON ti.transaction_id = t.transaction_id_pkey AND t.transaction_date >= @StartDate
                        GROUP BY p.product_id_pkey, p.product_name, p.sku
                        ORDER BY p.product_id_pkey;";

                    var startDate = DateTime.Now.AddDays(-refreshDays);

                    var parameters = new { StartDate = startDate };

                    var result = await connection.QueryAsync<ProductAnalyticsResponseDTO>(query, parameters);

                    analyticsList = result;

                    // Calculate profit for each product
                    foreach (var analytics in analyticsList)
                    {
                        analytics.Profit = analytics.MoneyEarned - analytics.MoneySpent;
                        analytics.FromDate = startDate;
                        analytics.ToDate = DateTime.Now;
                    }

                    // Set cache options
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromDays(refreshDays));

                    // Save data in cache
                    _cache.Set(CacheKey, analyticsList, cacheEntryOptions);
                }
            }

            return analyticsList;
        }

        public  async Task ResetProductAnalyticsCache()
        {
            _cache.Remove(CacheKey);
        }

        public async Task<IEnumerable<VendorAnalyticsResponseDTO>> GetVendorAnalytics(int vendorCount)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
            SELECT 
                v.vendor_id_pkey AS VendorID,
                v.vendor_name AS VendorName,
                v.vendor_email AS VendorEmail,
                COALESCE(SUM(ti.transaction_item_quantity), 0) AS ProductsSold,
                COALESCE(SUM(ti.transaction_item_price), 0) AS StockValue
            FROM vendor v
            LEFT JOIN transaction t ON v.vendor_id_pkey = t.vendor_id
            LEFT JOIN transaction_item ti ON t.transaction_id_pkey = ti.transaction_id
            WHERE t.transaction_type_id = 1
            GROUP BY v.vendor_id_pkey, v.vendor_name, v.vendor_email
            ORDER BY ProductsSold DESC
            LIMIT @VendorCount;";

                var parameters = new { VendorCount = vendorCount };

                var result = await connection.QueryAsync<VendorAnalyticsResponseDTO>(query, parameters);

                return result;
            }
        }



        public async Task<IEnumerable<CategoryAnalyticsResponseDTO>> GetCategoryAnalytics(int CategoryCount)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    SELECT c.category_id_pkey AS CategoryID,
                           c.category_name AS CategoryName,
                           COALESCE(SUM(CASE WHEN t.transaction_type_id = 2 THEN ti.transaction_item_quantity ELSE 0 END), 0) AS ProductsSold, 
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 2 THEN ti.transaction_item_price ELSE 0 END), 0) AS StockValue
                    FROM category c
                    LEFT JOIN product p ON c.category_id_pkey = p.category_id
                    LEFT JOIN transaction_item ti ON p.product_id_pkey = ti.product_id
                    LEFT JOIN transaction t ON ti.transaction_id = t.transaction_id_pkey
                    GROUP BY c.category_id_pkey, c.category_name
                    ORDER BY StockValue DESC
                    LIMIT @CategoryCount;";
                
                var parameters = new { CategoryCount = CategoryCount };

                var result = await connection.QueryAsync<CategoryAnalyticsResponseDTO>(query, parameters);

                return result;
            }
        }
    }
}