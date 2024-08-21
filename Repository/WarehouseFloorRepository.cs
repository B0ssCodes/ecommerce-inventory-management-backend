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

        public async Task CreateFloor(int warehouseID, WarehouseFloorRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                string createQuery = @"
                    INSERT INTO warehouse_floor (floor_name, warehouse_id)
                    VALUES (@FloorName, @WarehouseID)
                    RETURNING warehouse_floor_id_pkey;";

                var parameters = new { FloorName = requestDTO.FloorName, WarehouseID = warehouseID };

                var floorID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters);

                if (floorID == 0)
                {
                    throw new Exception("Floor not found");
                }
                else
                {
                    if (requestDTO.Rooms != null && requestDTO.Rooms.Count > 0)
                    {
                        foreach (var room in requestDTO.Rooms)
                        {
                            await _roomRepository.CreateRoom(floorID, room);
                        }
                    }
                }
            }
        }

        public async Task DeleteFloor(int? floorID, int? warehouseID)
        {
            try
            {
                using (IDbConnection connection = _db.CreateConnection())
                {
                    await _roomRepository.DeleteRoom(null, floorID);
                    connection.Open();

                    string deleteQuery = @"
                        UPDATE warehouse_floor
                        SET deleted = true
                        WHERE warehouse_floor_id_pkey = @FloorID;";

                    var parameters = new { FloorID = floorID };

                    await connection.ExecuteAsync(deleteQuery, parameters);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to delete floor", ex);  
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
                    WHERE warehouse_floor_id_pkey = @FloorID;";

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
                    WHERE warehouse_id = @WarehouseID;";

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
        public async Task UpdateFloor(int floorID, WarehouseFloorRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string updateQuery = @"
                    UPDATE warehouse_floor
                    SET floor_name = @FloorName
                    WHERE warehouse_floor_id_pkey = @FloorID;";

                var parameters = new { FloorName = requestDTO.FloorName, FloorID = floorID };

                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }
    }
}
