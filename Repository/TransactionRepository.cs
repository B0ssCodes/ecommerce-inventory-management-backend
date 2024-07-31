using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class TransactionRepository: ITransactionRepository
    {
        private readonly DapperContext _db;

        public TransactionRepository(DapperContext db)
        {
            _db = db;
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
                       v.vendor_id_pkey AS VendorID,
                       v.vendor_name AS Name,
                       v.vendor_email AS Email,
                       v.vendor_phone_number AS Phone,
                       v.vendor_commercial_phone AS CommercialPhone,
                       v.vendor_address AS Address,
                       tt.type AS TransactionType,
                       ts.status AS TransactionStatus
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
                       ti.transaction_id AS TransactionID,
                       ti.product_id AS ProductID,
                       ti.transaction_item_quantity AS Quantity,
                       ti.transaction_item_price AS Price,
                       p.product_id_pkey AS ProductID,
                       p.product_sku AS SKU,
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


        public async Task<List<AllTransactionResponseDTO>> GetTransactions(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                using (var transactionScope = connection.BeginTransaction())
                {
                    var transactionsQuery = @"
                SELECT t.transaction_id_pkey AS TransactionID,
                       t.transaction_amount AS Amount,
                       t.transaction_date AS Date,
                       v.vendor_id_pkey AS VendorID,
                       v.vendor_name AS Name,
                       tt.type AS Type,
                       ts.status AS Status
                FROM transaction t
                JOIN vendor v ON t.vendor_id = v.vendor_id_pkey
                JOIN transaction_type tt ON t.transaction_type_id = tt.transaction_type_id_pkey
                JOIN transaction_status ts ON t.transaction_status_id = ts.transaction_status_id_pkey
                WHERE (@Search IS NULL OR 
                       t.transaction_amount::TEXT ILIKE '%' || @Search || '%' OR
                       t.transaction_date::TEXT ILIKE '%' || @Search || '%' OR
                       v.vendor_name ILIKE '%' || @Search || '%')
                ORDER BY t.transaction_date DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                    var parameters = new
                    {
                        Offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                        PageSize = paginationParams.PageSize,
                        Search = paginationParams.Search
                    };

                    var transactions = await connection.QueryAsync<AllTransactionResponseDTO, ShortVendorResponseDTO, AllTransactionResponseDTO>(
                        transactionsQuery,
                        (transaction, vendor) =>
                        {
                            transaction.Vendor = vendor;
                            return transaction;
                        },
                        parameters,
                        transactionScope,
                        splitOn: "VendorID"
                    );

                    transactionScope.Commit();
                    return transactions.ToList();
                }
            }
        }

        public async Task SubmitTransaction(TransactionSubmitDTO transactionDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                using (var transactionScope = connection.BeginTransaction())
                {
                    var transactionCheckQuery = @"
                SELECT transaction_status_id
                FROM transaction
                WHERE transaction_id_pkey = @TransactionID";

                    var transactionCheckParameters = new
                    {
                        TransactionID = transactionDTO.TransactionID
                    };

                    int transactionStatus = await connection.QueryFirstOrDefaultAsync<int>(transactionCheckQuery, transactionCheckParameters, transactionScope);
                    if (transactionStatus == 0)
                    {
                        throw new Exception("Transaction does not exist");
                    }

                    // 1 corresponds to the "Created" status
                    if (transactionStatus != 1)
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
                        Date = transactionDTO.Date,
                        TransactionID = transactionDTO.TransactionID
                    };

                    await connection.ExecuteAsync(transactionUpdateQuery, transactionUpdateParameters, transactionScope);

                    transactionScope.Commit();
                }
            }
        }
    }
}
