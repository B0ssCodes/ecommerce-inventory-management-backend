﻿using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class WarehouseBinRepository : IWarehouseBinRepository
    {
        private readonly DapperContext _db;

        public WarehouseBinRepository(DapperContext db)
        {
            _db = db;
        }

        // This method will be called from the shelf repository
        public async Task CreateBin(int shelfID, WarehouseBinRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction)
        {
            bool isNewConnection = false;
            bool isNewTransaction = false;
            if (connection == null)
            {
                connection = _db.CreateConnection();
                isNewConnection = true;
            }
            if (transaction == null)
            {
                connection.Open();
                transaction = connection.BeginTransaction();
                isNewTransaction = true;
            }

            try
            {
                // Create the bin in the shelf
                string query = @"
                INSERT INTO warehouse_bin (bin_name, bin_capacity, warehouse_shelf_id)
                VALUES (@BinName, @BinCapacity, @ShelfID);";
                var parameters = new
                {
                    BinName = requestDTO.BinName,
                    BinCapacity = requestDTO.BinCapacity,
                    ShelfID = shelfID
                };

                await connection.ExecuteAsync(query, parameters, transaction);

            }
            catch (Exception ex)
            {
                // Rollback the transaction if any error occurs
                transaction.Rollback();
                throw new Exception("Transaction failed and rolled back", ex);
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

        public async Task DeleteBin(int? binID, int? shelfID, IDbConnection? connection, IDbTransaction? transaction)
        {
            bool isNewConnection = false;
            bool isNewTransaction = false;
            if (connection == null)
            {
                connection = _db.CreateConnection();
                isNewConnection = true;
            }
            if (transaction == null)
            {
                connection.Open();
                transaction = connection.BeginTransaction();
                isNewTransaction = true;
            }

            string checkIfBinHasProduct = @"
                        SELECT COUNT(*) AS ItemCount
                        FROM inventory i 
                        JOIN inventory_location il 
                        ON i.inventory_id_pkey = il.inventory_id
                        JOIN warehouse_bin b
                        ON il.warehouse_bin_id = b.warehouse_bin_id_pkey
                        WHERE i.inventory_stock > 0";

                if (binID != null)
                {
                    checkIfBinHasProduct += " AND il.warehouse_bin_id = @BinID";
                }
                else if (shelfID != null)
                {
                    checkIfBinHasProduct += " AND b.warehouse_shelf_id = @ShelfID";
                }
                else
                {
                    throw new Exception("Invalid Parameters");
                }

                var checkParameters = new { BinID = binID, ShelfID = shelfID };

                try
                {
                    int result = await connection.QueryFirstOrDefaultAsync<int>(checkIfBinHasProduct, checkParameters, transaction);
                    if (result > 0)
                    {
                        throw new Exception("One or More bins still contains items, remove all items to delete it");
                    }

                    string query = @"
                       UPDATE warehouse_bin
                       SET deleted = false";

                    if (binID != null)
                    {
                        query += " WHERE warehouse_bin_id_pkey = @BinID";
                    }
                    else if (shelfID != null)
                    {
                        query += " WHERE warehouse_shelf_id = @ShelfID";
                    }

                    var parameters = new { BinID = binID, ShelfID = shelfID};

                    await connection.ExecuteAsync(query, parameters, transaction);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
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

        public async Task<WarehouseBinResponseDTO> GetBin(int binID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string query = @"
            SELECT 
                b.warehouse_bin_id_pkey AS BinID,
                b.bin_name AS BinName,
                b.bin_capacity AS BinCapacity,
                COALESCE(SUM(i.inventory_stock), 0) AS BinCurrentStock
            FROM 
                warehouse_bin b
            LEFT JOIN 
                inventory_location il ON il.warehouse_bin_id = b.warehouse_bin_id_pkey
            LEFT JOIN 
                inventory i ON i.inventory_id = il.inventory_id
            WHERE 
                b.warehouse_bin_id_pkey = @BinID
            GROUP BY 
                b.warehouse_bin_id_pkey, b.bin_name, b.bin_capacity;";

                var parameters = new { BinID = binID };

                var bin = await connection.QueryFirstOrDefaultAsync<WarehouseBinResponseDTO>(query, parameters);

                if (bin == null)
                {
                    throw new Exception("Bin not found");
                }

                return bin;
            }
        }

        public async Task<List<WarehouseBinResponseDTO>> GetBins(int shelfID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string query = @"
            SELECT 
                b.warehouse_bin_id_pkey AS BinID,
                b.bin_name AS BinName,
                b.bin_capacity AS BinCapacity,
                COALESCE(SUM(i.inventory_stock), 0) AS BinCurrentStock
            FROM 
                warehouse_bin b
            LEFT JOIN 
                inventory_location il ON il.warehouse_bin_id = b.warehouse_bin_id_pkey
            LEFT JOIN 
                inventory i ON i.inventory_id_pkey = il.inventory_id
            WHERE   
                b.warehouse_shelf_id = @ShelfID
            GROUP BY 
                b.warehouse_bin_id_pkey, b.bin_name, b.bin_capacity;";

                var parameters = new { ShelfID = shelfID };

                var bins = await connection.QueryAsync<WarehouseBinResponseDTO>(query, parameters);

                return bins.ToList();
            }
        }

        public Task<List<AllProductResponseDTO>> GetProductsBin(int binID)
        {
            throw new NotImplementedException();
        }

        public async Task CreateOrUpdateBins(int shelfID, List<WarehouseBinRequestDTO> requestDTOs, IDbConnection? connection, IDbTransaction? transaction)
        {
            bool isNewConnection = false;
            bool isNewTransaction = false;
            if (connection == null)
            {
                connection = _db.CreateConnection();
                isNewConnection = true;
            }
            if (transaction == null)
            {
                connection.Open();
                transaction = connection.BeginTransaction();
                isNewTransaction = true;
            }

            try
            {
                // Fetch existing bin IDs for the shelf
                string fetchBinsQuery = @"
            SELECT warehouse_bin_id_pkey
            FROM warehouse_bin
            WHERE warehouse_shelf_id = @ShelfID AND deleted = false;";

                var existingBinIDs = (await connection.QueryAsync<int>(fetchBinsQuery, new { ShelfID = shelfID }, transaction)).ToList();

                // Process each bin in the request
                foreach (var requestDTO in requestDTOs)
                {
                    if (requestDTO.BinID.HasValue)
                    {
                        // Update existing bin
                        await UpdateBin(shelfID, requestDTO, connection, transaction);
                        existingBinIDs.Remove(requestDTO.BinID.Value); // Remove from the list of existing IDs
                    }
                    else
                    {
                        // Create new bin
                        string createQuery = @"
                    INSERT INTO warehouse_bin (bin_name, bin_capacity, warehouse_shelf_id)
                    VALUES (@BinName, @BinCapacity, @ShelfID)
                    RETURNING warehouse_bin_id_pkey;";

                        var parameters = new { BinName = requestDTO.BinName, BinCapacity = requestDTO.BinCapacity, ShelfID = shelfID };
                        var binID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters, transaction);

                        if (binID == 0)
                        {
                            throw new Exception("Failed to create bin");
                        }
                    }
                }

                // Delete bins that are not in the request
                foreach (var binID in existingBinIDs)
                {
                    await DeleteBin(binID, null, connection, transaction);
                }

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
            catch (Exception ex)
            {
                // Rollback the transaction if any error occurs
                transaction?.Rollback();
                throw new Exception("Transaction failed and rolled back", ex);
            }
            finally
            {
                if (isNewConnection)
                {
                    connection.Close();
                }
            }
        }
        public async Task UpdateBin(int shelfID, WarehouseBinRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction)
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
                isNewTransaction = true;
            }

            try
            {
                string query = @"
            UPDATE warehouse_bin
            SET 
                bin_name = @BinName, 
                bin_capacity = @BinCapacity,
                warehouse_shelf_id = @ShelfID
            WHERE 
                warehouse_bin_id_pkey = @BinID;";

                var parameters = new
                {
                    BinID = requestDTO.BinID,
                    BinName = requestDTO.BinName,
                    BinCapacity = requestDTO.BinCapacity,
                    ShelfID = shelfID
                };

                await connection.ExecuteAsync(query, parameters, transaction);
            }
                catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception("Transaction failed and rolled back", ex);
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
    }
}
