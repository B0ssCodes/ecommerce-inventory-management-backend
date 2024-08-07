using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class ProductAnalyticsRepository : IProductAnalyticsRepository
    {
        private readonly DapperContext _db;

        public ProductAnalyticsRepository(DapperContext db)
        {
            _db = db;
        }

        public async Task<List<ProductAnalyticsResponseDTO>> GetProductAnalytics()
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

                var startDate = DateTime.Now.AddDays(-7);

                var parameters = new { StartDate = startDate };

                var result = await connection.QueryAsync<ProductAnalyticsResponseDTO>(query, parameters);

                var analyticsList = result.ToList();

                // Calculate profit for each product
                foreach (var analytics in analyticsList)
                {
                    analytics.Profit = analytics.MoneyEarned - analytics.MoneySpent;
                }

                return analyticsList;
            }
        }
    }
}
