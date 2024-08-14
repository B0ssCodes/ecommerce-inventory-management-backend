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
        private readonly IMaterializedViewRepository _mvRepository;

        public AnalyticsRepository(DapperContext db, IMaterializedViewRepository mvRepository)
        {
            _db = db;
            _mvRepository = mvRepository;
        }

        public async Task<IEnumerable<ProductAnalyticsResponseDTO>> GetProductAnalytics()
        {
            await _mvRepository.CreateProductMV();
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    SELECT 
                        product_id_pkey AS ProductID,
                        product_name AS ProductName,
                        sku AS ProductSKU,
                        units_bought AS UnitsBought,
                        units_sold AS UnitsSold,
                        money_spent AS MoneySpent,
                        money_earned AS MoneyEarned,
                        start_date AS FromDate,
                        end_date AS ToDate
                    FROM mv_product_analytics
                    ORDER BY money_earned DESC;";

                var result = await connection.QueryAsync<ProductAnalyticsResponseDTO>(query);

                // Calculate profit for each product
                foreach (var analytics in result)
                {
                    analytics.Profit = analytics.MoneyEarned - analytics.MoneySpent;
                }

                return result;
            }
        }

        public async Task<IEnumerable<VendorAnalyticsResponseDTO>> GetVendorAnalytics(int vendorCount)
        {
            await _mvRepository.CreateVendorMV();
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
            SELECT 
                vendor_id_pkey AS VendorID,
                vendor_name AS VendorName,
                vendor_email AS VendorEmail,
                products_sold AS ProductsSold,
                stock_value AS StockValue,
                last_updated AS LastUpdated
            FROM mv_vendor_analytics
            LIMIT @VendorCount;";

                var parameters = new { VendorCount = vendorCount };

                var result = await connection.QueryAsync<VendorAnalyticsResponseDTO>(query, parameters);

                return result;
            }
        }



        public async Task<IEnumerable<CategoryAnalyticsResponseDTO>> GetCategoryAnalytics(int CategoryCount)
        {
            await _mvRepository.CreateCategoryMV();
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    SELECT category_id_pkey AS CategoryID,
                           category_name AS CategoryName,
                           products_sold AS ProductsSold, 
                           stock_value AS StockValue,
                           last_updated AS LastUpdated
                    FROM mv_category_analytics
                    LIMIT @CategoryCount;";

                var parameters = new { CategoryCount = CategoryCount };

                var result = await connection.QueryAsync<CategoryAnalyticsResponseDTO>(query, parameters);

                return result;
            }
        }


    }
}