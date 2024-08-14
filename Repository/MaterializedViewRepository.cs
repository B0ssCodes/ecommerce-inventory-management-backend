using Dapper;
using Inventory_Management_Backend.Data;
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

        public async Task RefreshAndPaginateProduct()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    var getTotalProductsQuery = "SELECT COUNT(product_id_pkey) FROM product";

                    int totalProducts = await connection.ExecuteScalarAsync<int>(getTotalProductsQuery, transaction);

                    int pageSize = 20;

                    int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

                    for (int i = 0; i < totalPages; i++)
                    {
                        int offset = i * pageSize;
                        string pageName = $"mv_product_{i + 1}";

                        // product_page_1
                        // product_page_2
                        // product_page_3

                        // Product start at page 1 and we are sorting by product_id descending, so page 1 will have the latest products

                        var checkViewExistsQuery = $"SELECT EXISTS (SELECT 1 FROM pg_matviews WHERE matviewname = @PageName)";

                        var existsParameters = new { PageName = pageName };

                        var viewExists = await connection.ExecuteScalarAsync<bool>(checkViewExistsQuery, existsParameters, transaction);
                        if (viewExists)
                        {
                            var refreshViewQuery = $"REFRESH MATERIALIZED VIEW {pageName}";
                            await connection.ExecuteAsync(refreshViewQuery);
                        }
                        else
                        {
                            var createViewQuery = $@"
                                CREATE MATERIALIZED VIEW @PageName
                                AS
                                SELECT 
                                    p.product_id_pkey AS product_id_pkey, 
                                    p.sku AS sku, 
                                    p.product_name AS product_name, 
                                    p.product_description AS product_description, 
                                    p.product_price AS product_price, 
                                    p.product_cost_price AS product_cost, 
                                    c.category_id AS category_id,
                                    c.category_name AS category_name,    
                                    (SELECT COUNT(image_id_pkey) FROM image WHERE product_id = p.product_id_pkey) AS ImageCount,
                                FROM product p
                                JOIN category c ON p.category_id = c.category_id_pkey
                                WHERE (@SearchQuery IS NULL OR p.product_name ILIKE '%' || @SearchQuery || '%' 
                                       OR p.product_description ILIKE '%' || @SearchQuery || '%' 
                                       OR p.sku ILIKE '%' || @SearchQuery || '%')
                                AND deleted = false 
                                ORDER BY p.product_id_pkey DESC
                                LIMIT @PageSize OFFSET @Offset";

                            var createParameters = new { PageName = pageName, SearchQuery = "", PageSize = pageSize, Offset = offset };

                            await connection.ExecuteAsync(createViewQuery, createParameters);
                        }
                    }
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
