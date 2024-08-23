using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class WarehouseFloorRepository : IWarehouseFloorRepository
    {
        private readonly DapperContext _db;
        private readonly IWarehouseRoomRepository _roomRepository;

        public WarehouseFloorRepository(DapperContext db, IWarehouseRoomRepository roomRepository)
        {
            _db = db;
            _roomRepository = roomRepository;
        }

        // This method will be called from the warehouse repository
        public async Task CreateFloor(int warehouseID, WarehouseFloorRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction)
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
                // Create a new floor in the warehouse
                string createQuery = @"
            INSERT INTO warehouse_floor (floor_name, warehouse_id)
            VALUES (@FloorName, @WarehouseID)
            RETURNING warehouse_floor_id_pkey;";

                var parameters = new { FloorName = requestDTO.FloorName, WarehouseID = warehouseID };

                var floorID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters, transaction);

                if (floorID == 0)
                {
                    throw new Exception("Failed to create floor");
                }
                else
                {
                    // If the floor has rooms, add them
                    if (requestDTO.Rooms != null && requestDTO.Rooms.Count > 0)
                    {
                        foreach (var room in requestDTO.Rooms)
                        {
                            await _roomRepository.CreateRoom(floorID, room, connection, transaction);
                        }
                    }
                }
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

        public async Task DeleteFloor(int? floorID, int? warehouseID, IDbConnection? connection, IDbTransaction? transaction)
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
                transaction = connection.BeginTransaction();
                isNewTransaction = true;
            }

            try
            {
                if (warehouseID.HasValue)
                {
                    // Fetch floor IDs associated with the warehouse
                    string fetchFloorsQuery = @"
                        SELECT warehouse_floor_id_pkey
                        FROM warehouse_floor
                        WHERE warehouse_id = @WarehouseID AND deleted = false;";

                    var floorIDs = await connection.QueryAsync<int>(fetchFloorsQuery, new { WarehouseID = warehouseID }, transaction);

                    foreach (var id in floorIDs)
                    {
                        await _roomRepository.DeleteRoom(null, id, connection, transaction);
                    }
                }
                else if (floorID.HasValue)
                {
                    await _roomRepository.DeleteRoom(null, floorID, connection, transaction);
                }
                else
                {
                    throw new Exception("Invalid parameters");
                }

                // Build the dynamic delete query
                string deleteQuery = @"
                    UPDATE warehouse_floor
                    SET deleted = true";

                if (floorID.HasValue)
                {
                    deleteQuery += " WHERE warehouse_floor_id_pkey = @FloorID";
                }
                else if (warehouseID.HasValue)
                {
                    deleteQuery += " WHERE warehouse_id = @WarehouseID";
                }

                var parameters = new { FloorID = floorID, WarehouseID = warehouseID };

                await connection.ExecuteAsync(deleteQuery, parameters, transaction);

                transaction.Commit();
            }

            catch (Exception ex)
            {
                if (isNewTransaction)
                {
                    transaction.Rollback();
                }
                throw new Exception("Failed to delete floor", ex);
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

        public async Task<WarehouseFloorResponseDTO> GetFloor(int floorID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string selectQuery = @"
                    SELECT warehouse_floor_id_pkey AS FloorID, 
                           floor_name AS FloorName
                    FROM warehouse_floor
                    WHERE warehouse_floor_id_pkey = @FloorID AND deleted = false;";

                var parameters = new { FloorID = floorID };

                var floorResult = await connection.QueryFirstOrDefaultAsync<WarehouseFloorResponseDTO>(selectQuery, parameters);

                if (floorResult == null)
                {
                    throw new Exception("Floor not found");
                }
                else
                {
                    floorResult.Rooms = await _roomRepository.GetRooms(floorID);
                }

                return floorResult;
            }
        }

        public async Task<List<WarehouseFloorResponseDTO>> GetFloors(int warehouseID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string selectQuery = @"
                    SELECT warehouse_floor_id_pkey AS FloorID, 
                           floor_name AS FloorName
                    FROM warehouse_floor
                    WHERE warehouse_id = @WarehouseID AND deleted = false;";

                var parameters = new { WarehouseID = warehouseID };

                var floorResults = await connection.QueryAsync<WarehouseFloorResponseDTO>(selectQuery, parameters);

                if (floorResults == null)
                {
                    throw new Exception("Floors not found");
                }
                else
                {
                    foreach (var floor in floorResults)
                    {
                        floor.Rooms = await _roomRepository.GetRooms(floor.FloorID);
                    }
                }

                return floorResults.AsList();
            }
        }

        public async Task CreateOrUpdateFloors(int warehouseID, List<WarehouseFloorRequestDTO> requestDTOs, IDbConnection? connection, IDbTransaction? transaction)
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
                // Fetch existing floor IDs for the warehouse
                string fetchFloorsQuery = @"
                SELECT warehouse_floor_id_pkey
                FROM warehouse_floor
                WHERE warehouse_id = @WarehouseID AND deleted = false;";

                var existingFloorIDs = (await connection.QueryAsync<int>(fetchFloorsQuery, new { WarehouseID = warehouseID }, transaction)).ToList();

                // Process each floor in the request
                foreach (var requestDTO in requestDTOs)
                {
                    if (requestDTO.FloorID.HasValue)
                    {
                        // Update existing floor
                        await UpdateFloor(requestDTO, connection, transaction);
                        existingFloorIDs.Remove(requestDTO.FloorID.Value); // Remove from the list of existing IDs
                    }
                    else
                    {
                        // Create new floor
                        string createQuery = @"
                    INSERT INTO warehouse_floor (floor_name, warehouse_id)
                    VALUES (@FloorName, @WarehouseID)
                    RETURNING warehouse_floor_id_pkey;";

                        var parameters = new { FloorName = requestDTO.FloorName, WarehouseID = warehouseID };
                        var floorID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters, transaction);

                        if (floorID == 0)
                        {
                            throw new Exception("Failed to create floor");
                        }

                        // Add rooms if any
                        if (requestDTO.Rooms != null && requestDTO.Rooms.Count > 0)
                        {
                            foreach (var room in requestDTO.Rooms)
                            {
                                await _roomRepository.CreateRoom(floorID, room, connection, transaction);
                            }
                        }
                    }
                }

                // Delete floors that are not in the request
                foreach (var floorID in existingFloorIDs)
                {
                    await DeleteFloor(floorID, null, connection, transaction);
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
                if (isNewTransaction)
                {
                    transaction.Rollback();
                }
                throw new Exception("Transaction failed and rolled back", ex);
            }
        }

        public async Task UpdateFloor(WarehouseFloorRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction)
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
                string updateQuery = @"
                    UPDATE warehouse_floor
                    SET floor_name = @FloorName
                    WHERE warehouse_floor_id_pkey = @FloorID;";

                if (!requestDTO.FloorID.HasValue)
                {
                    throw new Exception("No Floor ID Provided");
                }

                var parameters = new { FloorName = requestDTO.FloorName, FloorID = requestDTO.FloorID.Value };

                await connection.ExecuteAsync(updateQuery, parameters);

                if (requestDTO.Rooms != null && requestDTO.Rooms.Count > 0)
                {
                    await _roomRepository.CreateOrUpdateRooms(requestDTO.FloorID.Value, requestDTO.Rooms, connection, transaction);
                }
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
    }
}
