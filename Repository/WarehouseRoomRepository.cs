using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;
using System.Net;

namespace Inventory_Management_Backend.Repository
{
    public class WarehouseRoomRepository : IWarehouseRoomRepository
    {
        private readonly DapperContext _db;
        private readonly IWarehouseAisleRepository _aisleRepository;

        public WarehouseRoomRepository(DapperContext db, IWarehouseAisleRepository aisleRepository)
        {
            _db = db;
            _aisleRepository = aisleRepository;
        }

        // This method will be called from the floor repository to create a room
        public async Task CreateRoom(int floorID, WarehouseRoomRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction)
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
                // Create the room on the floor and get its ID
                string createQuery = @"
            INSERT INTO warehouse_room (room_name, warehouse_floor_id)
            VALUES (@RoomName, @FloorID)
            RETURNING warehouse_room_id_pkey;";

                var parameters = new { RoomName = requestDTO.RoomName, FloorID = floorID };

                var roomID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters, transaction);

                if (roomID == 0)
                {
                    throw new Exception("Failed to create room");
                }
                else
                {
                    // If the room has aisles, create them
                    if (requestDTO.Aisles != null && requestDTO.Aisles.Count > 0)
                    {
                        foreach (var aisle in requestDTO.Aisles)
                        {
                            await _aisleRepository.CreateAisle(roomID, aisle, connection, transaction);
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

        public async Task DeleteRoom(int? roomID, int? floorID, IDbConnection? connection, IDbTransaction? transaction)
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

                if (floorID.HasValue)
                {
                    // Fetch room IDs associated with the floor
                    string fetchRoomsQuery = @"
                        SELECT warehouse_room_id_pkey
                        FROM warehouse_room
                        WHERE warehouse_floor_id = @FloorID AND deleted = false;";

                    var roomIDs = await connection.QueryAsync<int>(fetchRoomsQuery, new { FloorID = floorID }, transaction);

                    foreach (var id in roomIDs)
                    {
                        await _aisleRepository.DeleteAisle(null, id, connection, transaction);
                    }
                }
                else if (roomID.HasValue)
                {
                    await _aisleRepository.DeleteAisle(null, roomID, connection, transaction);
                }
                else
                {
                    throw new Exception("Invalid parameters");
                }

                // Build the dynamic delete query
                string deleteQuery = @"
                    UPDATE warehouse_room
                    SET deleted = true";

                if (roomID.HasValue)
                {
                    deleteQuery += " WHERE warehouse_room_id_pkey = @RoomID";
                }
                else if (floorID.HasValue)
                {
                    deleteQuery += " WHERE warehouse_floor_id = @FloorID";
                }

                var parameters = new { RoomID = roomID, FloorID = floorID };

                await connection.ExecuteAsync(deleteQuery, parameters, transaction);

            }
            catch (Exception ex)
            {
                throw new Exception("Failed to delete room", ex);
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

        public async Task<WarehouseRoomResponseDTO> GetRoom(int roomID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                string query = @"
                        SELECT warehouse_room_id_pkey AS RoomID,
                               room_name AS RoomName,
                        FROM warehouse_room
                        WHERE warehouse_room_id_pkey = @RoomID AND deleted = false;";

                var parameters = new { RoomID = roomID };

                var room = await connection.QueryFirstOrDefaultAsync<WarehouseRoomResponseDTO>(query, parameters);

                if (room == null)
                {
                    throw new Exception("Room not found");
                }
                else
                {
                    room.Aisles = await _aisleRepository.GetAisles(roomID);
                }

                return room;
            }
        }

        public async Task<List<WarehouseRoomResponseDTO>> GetRooms(int floorID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string query = @"
                       SELECT warehouse_room_id_pkey AS RoomID,
                               room_name AS RoomName
                        FROM warehouse_room
                        WHERE warehouse_floor_id = @FloorID;";

                var parameters = new { FloorID = floorID };

                var rooms = await connection.QueryAsync<WarehouseRoomResponseDTO>(query, parameters);

                foreach (var room in rooms)
                {
                    room.Aisles = await _aisleRepository.GetAisles(room.RoomID);
                }

                return rooms.AsList();
            }
        }

        public async Task CreateOrUpdateRooms(int floorID, List<WarehouseRoomRequestDTO> requestDTOs, IDbConnection? connection, IDbTransaction? transaction)
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
                // Fetch existing room IDs for the floor
                string fetchRoomsQuery = @"
            SELECT warehouse_room_id_pkey
            FROM warehouse_room
            WHERE warehouse_floor_id = @FloorID AND deleted = false;";

                var existingRoomIDs = (await connection.QueryAsync<int>(fetchRoomsQuery, new { FloorID = floorID }, transaction)).ToList();

                // Process each room in the request
                foreach (var requestDTO in requestDTOs)
                {
                    if (requestDTO.RoomID.HasValue)
                    {
                        // Update existing room
                        await UpdateRoom(requestDTO, connection, transaction);
                        existingRoomIDs.Remove(requestDTO.RoomID.Value); // Remove from the list of existing IDs
                    }
                    else
                    {
                        // Create new room
                        string createQuery = @"
                    INSERT INTO warehouse_room (room_name, warehouse_floor_id)
                    VALUES (@RoomName, @FloorID)
                    RETURNING warehouse_room_id_pkey;";

                        var parameters = new { RoomName = requestDTO.RoomName, FloorID = floorID };
                        var roomID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters, transaction);

                        if (roomID == 0)
                        {
                            throw new Exception("Failed to create room");
                        }

                        // Add aisles if any
                        if (requestDTO.Aisles != null && requestDTO.Aisles.Count > 0)
                        {
                            foreach (var aisle in requestDTO.Aisles)
                            {
                                await _aisleRepository.CreateAisle(roomID, aisle, connection, transaction);
                            }
                        }
                    }
                }

                // Delete rooms that are not in the request
                foreach (var roomID in existingRoomIDs)
                {
                    await DeleteRoom(roomID, null, connection, transaction);
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

        public async Task UpdateRoom(WarehouseRoomRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction)
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
                    UPDATE warehouse_room
                    SET room_name = @RoomName
                    WHERE warehouse_room_id_pkey = @RoomID;";

                if (!requestDTO.RoomID.HasValue)
                {
                    throw new Exception("No Room ID Provided");
                }

                var parameters = new { RoomName = requestDTO.RoomName, RoomID = requestDTO.RoomID.Value };

                await connection.ExecuteAsync(updateQuery, parameters);

                if (requestDTO.Aisles != null && requestDTO.Aisles.Count > 0)
                {
                    await _aisleRepository.CreateOrUpdateAisles(requestDTO.RoomID.Value, requestDTO.Aisles, connection, transaction);
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

