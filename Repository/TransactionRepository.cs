using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly DapperContext _db;
        private readonly IInventoryRepository _inventoryRepository;

        public TransactionRepository(DapperContext db, IInventoryRepository inventoryRepository)
        {
            _db = db;
            _inventoryRepository = inventoryRepository;
        }

        public async Task<int> CreateTransaction(TransactionCreateDTO createDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                    INSERT INTO transaction (transaction_type_id, vendor_id, transaction_status_id)
                    VALUES (@TransactionTypeID, @VendorID, 1)
                    RETURNING transaction_id_pkey;";

                var parameters = new
                {
                    TransactionTypeID = createDTO.TransactionTypeID,
                    VendorID = createDTO.VendorID
                };

                int transactionID = await connection.QueryFirstOrDefaultAsync<int>(query, parameters);

                if (transactionID == 0)
                {
                    throw new Exception("Failed to create a transaction");
                }

                return transactionID;
            }
        }

        public async Task<TransactionResponseDTO> GetTransaction(int transactionID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                using (var transactionScope = connection.BeginTransaction())
                {
                    var transactionQuery = @"
                SELECT t.transaction_id_pkey AS TransactionID,
                       t.transaction_amount AS Amount,
                       t.transaction_date AS Date,
                       tt.type AS TransactionType,
                       ts.status AS TransactionStatus,
                       v.vendor_id_pkey AS VendorID,
                       v.vendor_name AS Name,
                       v.vendor_email AS Email,
                       v.vendor_phone_number AS Phone,
                       v.vendor_commercial_phone AS CommercialPhone,
                       v.vendor_address AS Address
                FROM transaction t
                JOIN vendor v ON t.vendor_id = v.vendor_id_pkey
                JOIN transaction_type tt ON t.transaction_type_id = tt.transaction_type_id_pkey
                JOIN transaction_status ts ON t.transaction_status_id = ts.transaction_status_id_pkey
                WHERE t.transaction_id_pkey = @TransactionID;";

                    var transactionParameters = new
                    {
                        TransactionID = transactionID
                    };

                    var transaction = await connection.QueryAsync<TransactionResponseDTO, VendorResponseDTO, TransactionResponseDTO>(
                        transactionQuery,
                        (transaction, vendor) =>
                        {
                            transaction.Vendor = vendor;
                            return transaction;
                        },
                        transactionParameters,
                        transactionScope,
                        splitOn: "VendorID"
                    );

                    var transactionResponse = transaction.FirstOrDefault();

                    if (transactionResponse == null)
                    {
                        throw new Exception("Transaction not found");
                    }

                    var transactionItemsQuery = @"
                SELECT ti.transaction_item_id_pkey AS TransactionItemID,
                       ti.product_id AS ProductID,
                       ti.transaction_item_quantity AS Quantity,
                       ti.transaction_item_price AS Price,
                       p.product_id_pkey AS ProductID,
                       p.sku AS SKU,
                       p.product_name AS Name
                FROM transaction_item ti
                JOIN product p ON ti.product_id = p.product_id_pkey
                WHERE ti.transaction_id = @TransactionID;";

                    var transactionItems = await connection.QueryAsync<TransactionItemResponseDTO, ShortProductResponseDTO, TransactionItemResponseDTO>(
                        transactionItemsQuery,
                        (item, product) =>
                        {
                            item.Product = product;
                            return item;
                        },
                        new { TransactionID = transactionID },
                        transactionScope,
                        splitOn: "ProductID"
                    );

                    transactionResponse.TransactionItems = transactionItems.ToList();

                    transactionScope.Commit();
                    return transactionResponse;
                }
            }
        }


        public async Task<(List<AllTransactionResponseDTO>, int)> GetTransactions(string? vendorEmail, PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                // Base query
                var baseQuery = @"
        WITH TransactionCTE AS (
            SELECT 
                t.transaction_id_pkey AS TransactionID,
                t.transaction_amount AS Amount,
                t.transaction_date AS Date,
                tt.type AS Type,
                ts.status AS Status,
                v.vendor_id_pkey AS VendorID,
                v.vendor_name AS Name,
                COUNT(*) OVER() AS TotalCount
            FROM transaction t
            JOIN vendor v ON t.vendor_id = v.vendor_id_pkey
            JOIN transaction_type tt ON t.transaction_type_id = tt.transaction_type_id_pkey
            JOIN transaction_status ts ON t.transaction_status_id = ts.transaction_status_id_pkey
            WHERE (@Search IS NULL OR 
                   t.transaction_amount::TEXT ILIKE '%' || @Search || '%' OR
                   t.transaction_date::TEXT ILIKE '%' || @Search || '%' OR
                   tt.type ILIKE '%' || @Search || '%' OR
                   ts.status ILIKE '%' || @Search || '%' OR
                   v.vendor_name ILIKE '%' || @Search || '%')";

                // Add vendor ID conditionally
                if (vendorEmail != null)
                {
                    baseQuery += " AND v.vendor_email = @VendorEmail";
                }

                // Complete the query
                var transactionsQuery = baseQuery + @"
        )
        SELECT TransactionID, Amount, Date, Type, Status, VendorID, Name, TotalCount
        FROM TransactionCTE
        ORDER BY Date DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    VendorEmail = vendorEmail,
                    Offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                    PageSize = paginationParams.PageSize,
                    Search = paginationParams.Search
                };

                var result = await connection.QueryAsync<AllTransactionResponseDTO, ShortVendorResponseDTO, long, (AllTransactionResponseDTO, long)>(
                    transactionsQuery,
                    (transaction, vendor, totalCount) =>
                    {
                        transaction.Vendor = vendor;
                        return (transaction, totalCount);
                    },
                    parameters,
                    splitOn: "VendorID, TotalCount"
                );

                var transactions = result.Select(r => r.Item1).ToList();
                int totalCount = result.Any() ? (int)result.First().Item2 : 0; // Explicitly cast to int, had an int64 error before

                return (transactions, totalCount);
            }
        }

        public async Task SubmitTransaction(TransactionSubmitDTO transactionDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                using (var transactionScope = connection.BeginTransaction())
                {
                    try
                    {
                        var transactionCheckQuery = @"
                    SELECT transaction_status_id AS TransactionStatusID, transaction_type_id AS TransactionTypeID
                    FROM transaction
                    WHERE transaction_id_pkey = @TransactionID";

                        var transactionCheckParameters = new
                        {
                            TransactionID = transactionDTO.TransactionID
                        };

                        TransactionTypeStatusDTO transactionTypeStatus = await connection.QueryFirstOrDefaultAsync<TransactionTypeStatusDTO>(transactionCheckQuery, transactionCheckParameters, transactionScope);
                        if (transactionTypeStatus.TransactionStatusID == 0)
                        {
                            throw new Exception("Transaction does not exist");
                        }

                        // 1 corresponds to the "created" status
                        if (transactionTypeStatus.TransactionStatusID != 1)
                        {
                            throw new Exception("Transaction already submitted");
                        }

                        var transactionItemsQuery = @"
                    INSERT INTO transaction_item (product_id, transaction_item_quantity, transaction_item_price, transaction_id)
                    VALUES (@ProductID, @Quantity, @Price, @TransactionID);";

                        foreach (var item in transactionDTO.TransactionItems)
                        {
                            var parameters = new
                            {
                                ProductID = item.ProductID,
                                Quantity = item.Quantity,
                                Price = item.Price,
                                TransactionID = transactionDTO.TransactionID
                            };

                            // The product items are now added in db
                            await connection.ExecuteAsync(transactionItemsQuery, parameters, transactionScope);

                            // Update the inventory
                            // Check the transaction type
                            int transactionTypeID = transactionTypeStatus.TransactionTypeID;

                            // Check if there already is an inventory for the product
                            int inventoryID = await _inventoryRepository.InventoryExists(item.ProductID);

                            // If there is:
                            // If the type is Inbound, call the AddInventory method and pass the item.
                            // If the type is Outbound, call the DecreaseInventory method and pass the item.
                            if (inventoryID > 0)
                            {
                                // If inbound
                                if (transactionTypeID == 1)
                                {
                                    await _inventoryRepository.IncreaseInventory(inventoryID, item);
                                }
                                else if (transactionTypeID == 2)
                                {
                                    await _inventoryRepository.DecreaseInventory(inventoryID, item);
                                }
                                else
                                {
                                    throw new Exception("Invalid transaction type");
                                }
                            }

                            // If there isn't.
                            // if the type is Inbound, call the CreateInventory method and pass the item.
                            // If the type is Outbound, throw an exception.
                            if (inventoryID == 0)
                            {
                                if (transactionTypeID == 1)
                                {
                                    await _inventoryRepository.CreateInventory(item);
                                }
                                else if (transactionTypeID == 2)
                                {
                                    throw new Exception("Inventory does not exist");
                                }
                            }

                        }

                        var transactionUpdateQuery = @"
                        UPDATE transaction
                        SET transaction_status_id = 2,
                            transaction_amount = @Amount,
                            transaction_date = @Date
                        WHERE transaction_id_pkey = @TransactionID;";

                        var transactionUpdateParameters = new
                        {
                            Amount = transactionDTO.Amount,
                            Date = DateTime.UtcNow,
                            TransactionID = transactionDTO.TransactionID
                        };

                        await connection.ExecuteAsync(transactionUpdateQuery, transactionUpdateParameters, transactionScope);

                        transactionScope.Commit();
                    }
                    catch (Exception ex)
                    {
                        transactionScope.Rollback();
                        throw ex;
                    }
                }
            }
        }
    }
}
