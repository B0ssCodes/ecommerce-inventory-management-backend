using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

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
        public async Task CreateRoom(int floorID, WarehouseRoomRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                string createQuery = @"
                    INSERT INTO warehouse_room (room_name, warehouse_floor_id_pkey)
                    VALUES (@RoomName, @FloorID)
                    RETURNING warehouse_room_id_pkey;";

                var parameters = new { RoomName = requestDTO.RoomName, FloorID = floorID };

                var roomID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters);

                if (roomID == 0)
                {
                    throw new Exception("Failed to create room");
                }
                else
                {
                    if (requestDTO.Aisles != null && requestDTO.Aisles.Count > 0)
                    {
                        foreach (var aisle in requestDTO.Aisles)
                        {
                            await _aisleRepository.CreateAisle(roomID, aisle);
                        }
                    }
                }
            }

        }

        public async Task DeleteRoom(int? roomID, int? floorID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                try
                {
                    await _aisleRepository.DeleteAisle(null, roomID);
                    connection.Open();
                    string deleteQuery = @"
                        UPDATE warehouse_room
                        SET deleted = true
                        WHERE warehouse_room_id_pkey = @RoomID;";

                    var parameters = new { RoomID = roomID };

                    await connection.ExecuteAsync(deleteQuery, parameters);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to delete room", ex);
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
                        WHERE warehouse_room_id_pkey = @RoomID;";

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
                               room_name AS RoomName,
                        FROM warehouse_room
                        WHERE warehouse_floor_id_pkey = @FloorID;";

                var parameters = new { FloorID = floorID };

                var rooms = await connection.QueryAsync<WarehouseRoomResponseDTO>(query, parameters);

                foreach (var room in rooms)
                {
                    room.Aisles = await _aisleRepository.GetAisles(room.RoomID);
                }

                return rooms.AsList();
            }
        }

        public async Task UpdateRoom(int roomID, WarehouseRoomRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string updateQuery = @"
                    UPDATE warehouse_room
                    SET room_name = @RoomName
                    WHERE warehouse_room_id_pkey = @RoomID;";

                var parameters = new { RoomName = requestDTO.RoomName, RoomID = roomID };

                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }
    }
}
