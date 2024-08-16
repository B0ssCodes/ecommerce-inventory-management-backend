using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class MaterializedViewRepository : IMaterializedViewRepository
    {
        private readonly DapperContext _db;
        public MaterializedViewRepository(DapperContext db)
        {
            _db = db;
        }

        public async Task CreateCategoryMV()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                // Check if the materialized view exists before creating
                var checkViewQuery = @"
            SELECT EXISTS (
                SELECT 1
                FROM pg_matviews
                WHERE matviewname = 'mv_category_analytics'
                )";

                var viewExists = await connection.ExecuteScalarAsync<bool>(checkViewQuery);

                // If the materialized view already exists just return, else create it.
                if (viewExists)
                {
                    return;
                }
                else
                {
                    var query = @"
                    CREATE MATERIALIZED VIEW mv_category_analytics
                    AS
                    SELECT c.category_id_pkey,
                           c.category_name,
                           COALESCE(SUM(CASE WHEN t.transaction_type_id = 2 THEN ti.transaction_item_quantity ELSE 0 END), 0) AS products_sold, 
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 2 THEN ti.transaction_item_price ELSE 0 END), 0) AS stock_value,
                            NOW() AS last_updated
                    FROM category c
                    LEFT JOIN product p ON c.category_id_pkey = p.category_id
                    LEFT JOIN transaction_item ti ON p.product_id_pkey = ti.product_id
                    LEFT JOIN transaction t ON ti.transaction_id = t.transaction_id_pkey
                    GROUP BY c.category_id_pkey, c.category_name
                    ORDER BY stock_value DESC;";

                    await connection.ExecuteAsync(query);
                }

            }
        }

        public async Task CreateProductMV()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                // Check if the materialized view exists before creating
                var checkViewQuery = @"
            SELECT EXISTS (
                SELECT 1
                FROM pg_matviews
                WHERE matviewname = 'mv_product_analytics'
                )";

                var viewExists = await connection.ExecuteScalarAsync<bool>(checkViewQuery);

                // If the materialized view already exists just return, else create it.
                if (viewExists)
                {
                    return;
                }
                else
                {
                    var createViewQuery = @"
                        CREATE MATERIALIZED VIEW mv_product_analytics AS
                        WITH date_range AS (
                            SELECT 
                                NOW() - INTERVAL '7 days' AS start_date,
                                NOW() AS end_date
                        )
                        SELECT 
                            p.product_id_pkey,
                            p.product_name,
                            p.sku,
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 1 AND t.transaction_date >= dr.start_date AND t.transaction_date <= dr.end_date THEN ti.transaction_item_quantity ELSE 0 END), 0) AS units_bought,
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 2 AND t.transaction_date >= dr.start_date AND t.transaction_date <= dr.end_date THEN ti.transaction_item_quantity ELSE 0 END), 0) AS units_sold,
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 1 AND t.transaction_date >= dr.start_date AND t.transaction_date <= dr.end_date THEN ti.transaction_item_quantity * p.product_cost_price ELSE 0 END), 0) AS money_spent,
                            COALESCE(SUM(CASE WHEN t.transaction_type_id = 2 AND t.transaction_date >= dr.start_date AND t.transaction_date <= dr.end_date THEN ti.transaction_item_quantity * p.product_price ELSE 0 END), 0) AS money_earned,
                            dr.start_date,
                            dr.end_date
                        FROM product p
                        LEFT JOIN transaction_item ti ON p.product_id_pkey = ti.product_id
                        LEFT JOIN transaction t ON ti.transaction_id = t.transaction_id_pkey
                        CROSS JOIN date_range dr
                        WHERE t.transaction_date >= dr.start_date AND t.transaction_date <= dr.end_date
                        GROUP BY p.product_id_pkey, p.product_name, p.sku, dr.start_date, dr.end_date
                        ORDER BY p.product_id_pkey;";

                    await connection.ExecuteAsync(createViewQuery);
                }

            }
        }

        public async Task CreateVendorMV()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                // Check if the materialized view exists before creating
                var checkViewQuery = @"
            SELECT EXISTS (
                SELECT 1
                FROM pg_matviews
                WHERE matviewname = 'mv_vendor_analytics'
                )";

                var viewExists = await connection.ExecuteScalarAsync<bool>(checkViewQuery);

                // If the materialized view already exists just return, else create it.
                if (viewExists)
                {
                    return;
                }
                else
                {
                    var createViewQuery = @"
                        CREATE MATERIALIZED VIEW mv_vendor_analytics 
                        AS
                        
            SELECT 
                v.vendor_id_pkey,
                v.vendor_name,
                v.vendor_email,
                COALESCE(SUM(ti.transaction_item_quantity), 0) AS products_sold,
                COALESCE(SUM(ti.transaction_item_price), 0) AS stock_value,
                NOW() AS last_updated
            FROM vendor v
            LEFT JOIN transaction t ON v.vendor_id_pkey = t.vendor_id
            LEFT JOIN transaction_item ti ON t.transaction_id_pkey = ti.transaction_id
            WHERE t.transaction_type_id = 1
            GROUP BY v.vendor_id_pkey, v.vendor_name, v.vendor_email
            ORDER BY products_sold DESC;";

                    await connection.ExecuteAsync(createViewQuery);
                }

            }
        }

        public async Task RefreshCategoryMV()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = "REFRESH MATERIALIZED VIEW mv_category_analytics";
                await connection.ExecuteAsync(query);
            }
        }

        public async Task RefreshProductMV()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {

                var query = "REFRESH MATERIALIZED VIEW mv_product_analytics;";
                await connection.ExecuteAsync(query);
            }
        }

        public async Task RefreshVendorMV()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {

                var query = "REFRESH MATERIALIZED VIEW mv_vendor_analytics;";
                await connection.ExecuteAsync(query);
            }
        }
    }
}
