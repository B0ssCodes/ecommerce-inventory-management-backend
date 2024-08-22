using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class InventoryLocationRepository : IInventoryLocationRepository
    {
        private readonly DapperContext _db;
        private readonly IInventoryRepository _inventoryRepository;

        public InventoryLocationRepository(DapperContext db, IInventoryRepository inventoryRepository)
        {
            _db = db;
            _inventoryRepository = inventoryRepository;
        }

        public async Task CreateInventoryLocation(InventoryLocationRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                int inventoryID = await _inventoryRepository.InventoryExists(requestDTO.ProductID);

                if (inventoryID == 0)
                {
                    throw new Exception("Inventory not found");
                }

                string createQuery = @"
                        INSERT INTO inventory_location(warehouse_bin_id, inventory_id)
                        VALUES (@BinID, @InventoryID);";

                if (requestDTO.BinID == null)
                {
                    var nullParameters = new { BinID = (int?)null, InventoryID = inventoryID };
                    await connection.ExecuteAsync(createQuery, nullParameters);
                }
                else
                {
                    var parameters = new { BinID = requestDTO.BinID, InventoryID = inventoryID };
                    await connection.ExecuteAsync(createQuery, parameters);
                }

            }
        }

        public Task DeleteInventoryLocation(int locationID)
        {
            throw new NotImplementedException();
        }

        public async Task<InventoryLocationResponseDTO> GetInventoryLocation(int locationID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string selectQuery = @"
            SELECT  il.inventory_location_id_pkey AS InventoryLocationID,
                    il.inventory_id AS InventoryID, 
                    b.warehouse_bin_id_pkey AS BinID,
                    b.bin_name AS BinName,
                    s.warehouse_shelf_id_pkey AS ShelfID,
                    s.shelf_name AS ShelfName,
                    a.warehouse_aisle_id_pkey AS AisleID,
                    a.aisle_name AS AisleName,
                    r.warehouse_room_id_pkey AS RoomID,
                    r.room_name AS RoomName,
                    f.warehouse_floor_id_pkey AS FloorID,
                    f.floor_name AS FloorName,
                    w.warehouse_id_pkey AS WarehouseID,
                    w.warehouse_name AS WarehouseName
            FROM inventory_location il
            JOIN warehouse_bin b ON il.warehouse_bin_id = b.warehouse_bin_id_pkey
            JOIN warehouse_shelf s ON b.warehouse_shelf_id = s.warehouse_shelf_id_pkey
            JOIN warehouse_aisle a ON s.warehouse_aisle_id = a.warehouse_aisle_id_pkey
            JOIN warehouse_room r ON a.warehouse_room_id = r.warehouse_room_id_pkey
            JOIN warehouse_floor f ON r.warehouse_floor_id = f.warehouse_floor_id_pkey
            JOIN warehouse w ON f.warehouse_id = w.warehouse_id_pkey
            WHERE il.inventory_location_id_pkey = @LocationID;";

                var parameters = new { LocationID = locationID };

                var inventoryLocation = await connection.QueryFirstOrDefaultAsync<InventoryLocationResponseDTO>(selectQuery, parameters);

                if (inventoryLocation == null)
                {
                    throw new Exception("Inventory location not found");
                }

                return inventoryLocation;
            }
        }

        public async Task UpdateInventoryLocation(int locationID, InventoryLocationUpdateDTO updateDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string updateQuery = @"
                        UPDATE inventory_location
                        SET warehouse_bin_id = @BinID
                        WHERE inventory_location_id_pkey = @LocationID;";

                var parameters = new { BinID = updateDTO.BinID, LocationID = locationID};

                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }
    }
}
