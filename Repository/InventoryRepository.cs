﻿using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly DapperContext _db;
        public InventoryRepository(DapperContext db)
        {
            _db = db;
        }

        public async Task IncreaseInventory(int inventoryID, TransactionItemRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                var query = @"
                    UPDATE inventory
                    SET inventory_stock = inventory_stock + @Quantity,
                        inventory_cost = inventory_cost + @Price
                    WHERE inventory_id_pkey = @InventoryID;";

                var parameters = new
                {
                    Quantity = requestDTO.Quantity,
                    Price = requestDTO.Price,
                    InventoryID = inventoryID
                };

                await connection.ExecuteAsync(query, parameters);

            }
        }

        public async Task<int> CreateInventory(int productID, IDbConnection? connection, IDbTransaction? transaction)
        {
            bool isNewConnection = false;
            bool isNewTransaction = false;

            if (connection == null)
            {
                connection = _db.CreateConnection();
                connection.Open();
                isNewConnection = true;
            }
            if (transaction == null)
            {
                transaction = connection.BeginTransaction();
            }

            try
            {
                var query = @"
                    INSERT INTO inventory (inventory_stock, inventory_cost, product_id)
                    VALUES (@Quantity, @Price, @ProductID)
                    RETURNING inventory_id_pkey as InventoryID;";

                var parameters = new
                {
                    Quantity = 0,
                    Price = 0,
                    ProductID = productID
                };

                int result = await connection.QueryFirstOrDefaultAsync<int>(query, parameters, transaction);

                return result;
            }
                
            catch (Exception ex)
            {
                if (isNewTransaction)
                {
                    transaction.Rollback();
                }
                throw new Exception("Error while creating inventory");
            }
            finally
            {
                if (isNewTransaction)
                {
                    transaction.Commit();
                    transaction.Dispose();
                }
                if (isNewConnection)
                {
                    connection.Close();
                }
            }
        }

        public async Task DecreaseInventory(int inventoryID, TransactionItemRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var getQuantityquery = @"
                        SELECT inventory_stock
                        FROM inventory
                        WHERE inventory_id_pkey = @InventoryID;";

                    var quantityParameters = new { InventoryID = inventoryID };

                    int quantity = connection.QueryFirstOrDefault<int>(getQuantityquery, quantityParameters, transaction);

                    // If the quantity in stock is less than the quantity requested, throw an exception
                    if (quantity < requestDTO.Quantity)
                    {
                        throw new System.Exception("Not enough stock");
                    }

                    // If the quantity in stock is equal to the quantity requested, delete the inventory
                    if (quantity == requestDTO.Quantity)
                    {
                        var query = @"
                            UPDATE inventory
                            SET inventory_stock = @Quantity,
                                inventory_cost = @Price
                            WHERE inventory_id_pkey = @InventoryID;";

                        var parameters = new { Quantity = 0, Price = 0, InventoryID = inventoryID };

                        await connection.ExecuteAsync(query, parameters, transaction);
                    }

                    // If the quantity in stock is greater than the quantity requested, decrease the inventory
                    else
                    {
                        var getCostQuery = @"
                            SELECT p.product_cost_price
                            FROM product p 
                            JOIN inventory i
                            ON p.product_id_pkey = i.product_id
                            WHERE i.inventory_id_pkey = @InventoryID;";

                        decimal? cost = connection.QueryFirstOrDefault<int>(getCostQuery, quantityParameters, transaction);

                        if (cost == null)
                        {
                            throw new Exception("Product not found");
                        }

                        if (cost.HasValue)
                        {
                            decimal newCost = cost.Value * requestDTO.Quantity;

                            var query = @"
                            UPDATE inventory
                            SET inventory_stock = inventory_stock - @Quantity,
                             inventory_cost = inventory_cost - @Price
                            WHERE inventory_id_pkey = @InventoryID;";

                            var parameters = new
                            {
                                Quantity = requestDTO.Quantity,
                                Price = newCost,
                                InventoryID = inventoryID,
                            };

                            await connection.ExecuteAsync(query, parameters, transaction);
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        public async Task DeleteInventory(int inventoryID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                    DELETE FROM inventory 
                    WHERE inventory_id_pkey = @InventoryID;";

                var parameters = new { InventoryID = inventoryID };

                await connection.ExecuteAsync(query, parameters);
            }
        }

        public async Task<(List<AllInventoryResponseDTO>, int)> GetInventories(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;
                var searchQuery = paginationParams.Search;

                // Define the mapping between frontend field names and database field names
                var fieldMapping = new Dictionary<string, string>
        {
            { "quantity", "inventory_stock" },
            { "price", "inventory_cost" },
            { "productName", "product_name" },
            { "productSKU", "sku" },
            { "productPrice", "product_cost_price" }
        };

                var baseQuery = @"
    WITH InventoryCTE AS (
        SELECT i.inventory_id_pkey AS InventoryID,
               i.inventory_stock AS Quantity,
               i.inventory_cost AS Price,
               b.bin_name AS BinName,
               p.product_id_pkey AS ProductID,
               p.product_name AS ProductName,
               p.sku AS ProductSKU,
               p.product_cost_price AS ProductPrice,
               COUNT(*) OVER() AS TotalCount
        FROM inventory i
        JOIN product p ON i.product_id = p.product_id_pkey
        LEFT JOIN inventory_location il ON i.inventory_id_pkey = il.inventory_id
        LEFT JOIN warehouse_bin b ON il.warehouse_bin_id = b.warehouse_bin_id_pkey
        WHERE (@SearchQuery IS NULL OR p.product_name ILIKE '%' || @SearchQuery || '%'
                OR p.sku ILIKE '%' || @SearchQuery || '%'
                OR CAST(i.inventory_stock AS TEXT) ILIKE '%' || @SearchQuery || '%')
        AND p.deleted = false
        AND i.inventory_stock > 0
    ";

                // Add dynamic filters
                if (paginationParams.Filters != null && paginationParams.Filters.Count > 0)
                {
                    foreach (Filter filter in paginationParams.Filters)
                    {
                        string field = "";
                        if (fieldMapping.ContainsKey(filter.Field.ToLower()))
                        {
                            field = fieldMapping[filter.Field.ToLower()];
                        }

                        // Use parameterized queries to safely include the value
                        baseQuery += $" AND {field} {filter.Operator} '{filter.Value}'";
                    }
                }

                baseQuery += @")
        SELECT InventoryID, Quantity, Price, BinName, ProductID, ProductName, ProductSKU, ProductPrice, TotalCount
        FROM InventoryCTE";

                if (paginationParams.SortBy != null)
                {
                    string sortBy = char.ToUpper(paginationParams.SortBy[0]) + paginationParams.SortBy.Substring(1);
                    baseQuery += " ORDER BY " + sortBy + " " + (paginationParams.SortOrder == "asc" ? "ASC" : "DESC");
                }
                else
                {
                    baseQuery += " ORDER BY InventoryID DESC";
                }

                baseQuery += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    Offset = offset,
                    PageSize = paginationParams.PageSize,
                    SearchQuery = searchQuery
                };

                var result = await connection.QueryAsync<AllInventoryResponseDTO, long, (AllInventoryResponseDTO, long)>(
                    baseQuery,
                    (inventory, totalCount) => (inventory, totalCount),
                    parameters,
                    splitOn: "TotalCount"
                );

                var inventories = result.Select(x => x.Item1).ToList();
                var totalCount = result.Any() ? (int)result.First().Item2 : 0; // Explicitly cast to int

                return (inventories, totalCount);
            }
        }

        public async Task<InventoryResponseDTO> GetInventory(int inventoryID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                    SELECT inventory_id_pkey AS InventoryID,
                           inventory_stock AS Quantity,
                           inventory_cost AS Price,
                           product_id AS ProductID
                    FROM inventory
                    WHERE inventory_id_pkey = @InventoryID;";
                var parameters = new { InventoryID = inventoryID };

                InventoryResponseDTO result = await connection.QueryFirstOrDefaultAsync<InventoryResponseDTO>(query, parameters);

                if (result == null)
                {
                    throw new Exception("Inventory not found");
                }
                return result;

            }
        }

        public async Task<int> InventoryExists(int productID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                    SELECT COALESCE (
                        (SELECT inventory_id_pkey
                         FROM inventory
                         WHERE product_id = @ProductID
                         LIMIT 1),
                        0
                    ) as InventoryID;";

                var parameters = new { ProductID = productID };

                int result = await connection.QueryFirstOrDefaultAsync<int>(query, parameters);

                return result;
            }
        }

        public async Task<(List<AllInventoryResponseDTO>, int)> GetLowStockInventories(int minStockQuantity,PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;

                var query = @"
                 WITH InventoryCTE AS (
                    SELECT i.inventory_id_pkey AS InventoryID,
                           i.inventory_stock AS Quantity,
                           i.inventory_cost AS Price,
                           p.product_id_pkey AS ProductID,
                           p.product_name AS ProductName,
                           p.sku AS ProductSKU,
                           p.product_cost_price AS ProductPrice,
                           COUNT (*) OVER() AS TotalCount
                    FROM inventory i
                    JOIN product p ON i.product_id = p.product_id_pkey
                    WHERE (@SearchQuery IS NULL OR p.product_name ILIKE '%' || @SearchQuery || '%'
                            OR p.sku ILIKE '%' || @SearchQuery || '%'
                            OR CAST(i.inventory_stock AS TEXT) ILIKE '%' || @SearchQuery || '%')
                            AND p.deleted = false
                            AND i.inventory_stock < @MinStockQuantity
                            AND i.inventory_stock > 0
                    )   
                    SELECT InventoryID, Quantity, Price, ProductID, ProductName, ProductSKU, ProductPrice, TotalCount
                    FROM InventoryCTE
                    ORDER BY InventoryID DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    MinStockQuantity = minStockQuantity,
                    Offset = offset,
                    PageSize = paginationParams.PageSize,
                    SearchQuery = paginationParams.Search
                };

                var result = await connection.QueryAsync<AllInventoryResponseDTO, long, (AllInventoryResponseDTO, long)>(
                    query,
                    (inventory, totalCount) => (inventory, totalCount),
                    parameters,
                    splitOn: "TotalCount"
                    );

                var inventories = result.Select(x => x.Item1).ToList();
                var totalCount = (int)result.FirstOrDefault().Item2;

                return (inventories, totalCount);

            }
        }

        public async Task<(List<ProductWithoutInventoryDTO>, int)> GetOutStockInventories(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;

                var query = @"
            WITH ProductsWithoutInventoryCTE AS (
                SELECT p.product_id_pkey AS ProductID,
                       p.product_name AS ProductName,
                       p.sku AS ProductSKU,
                       p.product_cost_price AS ProductPrice,
                       COUNT(*) OVER() AS TotalCount
                FROM product p
                LEFT JOIN inventory i ON p.product_id_pkey = i.product_id
                WHERE i.inventory_stock = 0
                  AND p.deleted = false
                  AND (@SearchQuery IS NULL OR p.product_name ILIKE '%' || @SearchQuery || '%'
                       OR p.sku ILIKE '%' || @SearchQuery || '%'
                       OR CAST(p.product_cost_price AS TEXT) ILIKE '%' || @SearchQuery || '%')
            )
            SELECT ProductID, ProductName, ProductSKU, ProductPrice, TotalCount
            FROM ProductsWithoutInventoryCTE
            ORDER BY ProductID DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    Offset = offset,
                    PageSize = paginationParams.PageSize,
                    SearchQuery = paginationParams.Search
                };

                var result = await connection.QueryAsync<ProductWithoutInventoryDTO, long, (ProductWithoutInventoryDTO, long)>(
                    query,
                    (product, totalCount) => (product, totalCount),
                    parameters,
                    splitOn: "TotalCount"
                );

                var products = result.Select(x => x.Item1).ToList();
                var totalCount = (int)result.FirstOrDefault().Item2;

                return (products, totalCount);
            }
        }

        public async Task<int> GetLowStockInventoriesCount(int minStockQuantity)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    SELECT COUNT(inventory_id_pkey) OVER()
                    FROM inventory i 
                    JOIN product p ON i.product_id = p.product_id_pkey
                    WHERE inventory_stock < @MinStockQuantity
                    AND inventory_stock > 0 
                    AND p.deleted = false;";

                var parameters = new { MinStockQuantity = minStockQuantity };

                int result = await connection.QueryFirstOrDefaultAsync<int>(query, parameters);

                return result;
            }
        }

        public async Task<int> GetOutStockInventoriesCount()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                    SELECT COUNT(*)
                    FROM product p
                    LEFT JOIN inventory i ON p.product_id_pkey = i.product_id
                    WHERE i.inventory_stock = 0
                    AND p.deleted = false;";

                int result = await connection.QueryFirstOrDefaultAsync<int>(query);

                return result;
            }
        }
    }
}

