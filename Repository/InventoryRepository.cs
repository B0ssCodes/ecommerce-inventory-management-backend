using Dapper;
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
                    SET inventory_stock = inventory_stock + @Quantity
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

        public async Task<int> CreateInventory(TransactionItemRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                    INSERT INTO inventory (inventory_stock, inventory_cost, product_id)
                    VALUES (@Quantity, @Price, @ProductID)
                    RETURNING inventory_id_pkey as InventoryID;";

                int result = await connection.QueryFirstOrDefaultAsync<int>(query, new
                {
                    Quantity = requestDTO.Quantity,
                    Price = requestDTO.Price,
                    ProductID = requestDTO.ProductID
                });

                return result;
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
                        await DeleteInventory(inventoryID);
                    }

                    // If the quantity in stock is greater than the quantity requested, decrease the inventory
                    else
                    {
                        var query = @"
                            UPDATE inventory
                            SET inventory_stock = inventory_stock - @Quantity
                            WHERE inventory_id_pkey = @InventoryID;";

                        var parameters = new
                        {
                            Quantity = requestDTO.Quantity,
                            InventoryID = inventoryID,
                        };

                        await connection.ExecuteAsync(query, parameters, transaction);

                        transaction.Commit();

                    }
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
                var query = @"
                 WITH InventoryCTE AS (
                    SELECT i.inventory_id_pkey AS InventoryID,
                           i.inventory_stock AS Quantity,
                           i.inventory_cost AS Price,
                           p.product_name AS ProductName,
                           p.sku AS ProductSKU,
                           p.product_cost_price AS ProductPrice,
                           COUNT (*) OVER() AS TotalCount
                    FROM inventory i
                    JOIN product p ON i.product_id = p.product_id_pkey
                    WHERE (@SearchQuery IS NULL OR p.product_name ILIKE '%' || @SearchQuery || '%'
                            OR p.sku ILIKE '%' || @SearchQuery || '%'
                            OR CAST(i.inventory_stock AS TEXT) ILIKE '%' || @SearchQuery || '%')
                    )
                    SELECT InventoryID, Quantity, Price, ProductName, ProductSKU, ProductPrice, TotalCount
                    FROM InventoryCTE
                    ORDER BY InventoryID DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
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

                return (inventories, (int)totalCount);
                           
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
    }
}

