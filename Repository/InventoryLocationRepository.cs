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

                string checkIfLocationExistsQuery = @"
                        SELECT inventory_location_id_pkey
                        FROM inventory_location
                        WHERE inventory_id = @InventoryID;";

                var checkParameters = new { InventoryID = requestDTO.InventoryID };

                int? locationID = await connection.QueryFirstOrDefaultAsync<int?>(checkIfLocationExistsQuery, checkParameters);

                string getBinCapacityQuery = @"
                        SELECT bin_capacity
                        FROM warehouse_bin
                        WHERE warehouse_bin_id_pkey = @BinID;";

                int binCapacity = await connection.QueryFirstOrDefaultAsync<int>(getBinCapacityQuery, new { BinID = requestDTO.BinID });

                string getInventoryStockQuery = @"
                        SELECT inventory_stock
                        FROM inventory
                        WHERE inventory_id_pkey = @InventoryID;";

                int inventoryStock = await connection.QueryFirstOrDefaultAsync<int>(getInventoryStockQuery, new { InventoryID = requestDTO.InventoryID });

                if (inventoryStock > binCapacity)
                {
                    throw new Exception("Inventory Stock exceeds bin capacity!");
                }

                if (locationID != null)
                {
                    await UpdateInventoryLocation(requestDTO);
                }
                else
                {
                    string createQuery = @"
                    INSERT INTO inventory_location(warehouse_id, warehouse_floor_id, warehouse_room_id, warehouse_aisle, warehouse_shelf_id, warehouse_bin_id, inventory_id)
                    VALUES (@WarehouseID, @FloorID, @RoomID, @AisleID, @ShelfID, @BinID, @InventoryID);";

                    var parameters = new
                    {
                        WarehouseID = requestDTO.WarehouseID,
                        FloorID = requestDTO.FloorID,
                        RoomID = requestDTO.RoomID,
                        AisleID = requestDTO.AisleID,
                        ShelfID = requestDTO.ShelfID,
                        BinID = requestDTO.BinID,
                        InventoryID = requestDTO.InventoryID
                    };

                    await connection.ExecuteAsync(createQuery, parameters);
                }
            }
        }

        public Task DeleteInventoryLocation(int locationID)
        {
            throw new NotImplementedException();
        }

        public async Task<InventoryLocationResponseDTO> GetInventoryLocation(int? inventoryID, int? locationID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string selectQuery = @"
        SELECT  il.inventory_location_id_pkey AS InventoryLocationID,
                il.inventory_id AS InventoryID, 
                il.warehouse_bin_id AS BinID,
                b.bin_name AS BinName,
                il.warehouse_shelf_id AS ShelfID,
                s.shelf_name AS ShelfName,
                il.warehouse_aisle_id AS AisleID,
                a.aisle_name AS AisleName,
                il.warehouse_room_id AS RoomID,
                r.room_name AS RoomName,
                il.warehouse_floor_id AS FloorID,
                f.floor_name AS FloorName,
                il.warehouse_id AS WarehouseID,
                w.warehouse_name AS WarehouseName
        FROM inventory_location il
        JOIN warehouse_bin b ON il.warehouse_bin_id = b.warehouse_bin_id_pkey
        JOIN warehouse_shelf s ON il.warehouse_shelf_id = s.warehouse_shelf_id_pkey
        JOIN warehouse_aisle a ON il.warehouse_aisle_id = a.warehouse_aisle_id_pkey
        JOIN warehouse_room r ON il.warehouse_room_id = r.warehouse_room_id_pkey
        JOIN warehouse_floor f ON il.warehouse_floor_id = f.warehouse_floor_id_pkey
        JOIN warehouse w ON il.warehouse_id = w.warehouse_id_pkey";

                if (inventoryID != null)
                {
                    selectQuery += " WHERE il.inventory_id = @InventoryID;";
                }
                else if (locationID != null)
                {
                    selectQuery += " WHERE il.inventory_location_id_pkey = @LocationID";
                }

                var parameters = new { InventoryID = inventoryID, LocationID = locationID };

                var inventoryLocation = await connection.QueryFirstOrDefaultAsync<InventoryLocationResponseDTO>(selectQuery, parameters);

                if (inventoryLocation == null)
                {
                    throw new Exception("Inventory location not found");
                }

                return inventoryLocation;
            }
        }

        public async Task UpdateInventoryLocation(InventoryLocationRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string updateQuery = @"
                        UPDATE inventory_location
                        SET warehouse_id = @WarehouseID,
                            warehouse_floor_id = @FloorID,
                            warehouse_room_id = @RoomID,
                            warehouse_aisle_id = @AisleID,
                            warehouse_shelf_id = @ShelfID,
                            warehouse_bin_id = @BinID
                        WHERE inventory_id = @InventoryID;";

                var parameters = new
                {
                    WarehouseID = requestDTO.WarehouseID,
                    FloorID = requestDTO.FloorID,
                    RoomID = requestDTO.RoomID,
                    AisleID = requestDTO.AisleID,
                    ShelfID = requestDTO.ShelfID,
                    BinID = requestDTO.BinID,
                    InventoryID = requestDTO.InventoryID
                };

                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }
    }
}