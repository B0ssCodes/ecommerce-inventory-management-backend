using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class WarehouseShelfRepository : IWarehouseShelfRepository
    {
        private readonly DapperContext _db;
        private readonly IWarehouseBinRepository _warehouseBinRepository;

        public WarehouseShelfRepository(DapperContext db, IWarehouseBinRepository warehouseBinRepository)
        {
            _db = db;
            _warehouseBinRepository = warehouseBinRepository;
        }

        // This method will be called from the aisle repository
        public async Task CreateShelf(int aisleID, WarehouseShelfRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                // Create the shelf in the aisle and return the shelfID
                var query = @"
                    INSERT INTO warehouse_shelf (shelf_name, warehouse_aisle_id)
                    VALUES (@ShelfName, @AisleID)
                    RETURNING warehouse_shelf_id_pkey;";

                var paramters = new
                {
                    ShelfName = requestDTO.ShelfName,
                    AisleID = aisleID
                };

                var shelfID = await connection.QueryFirstOrDefaultAsync<int>(query, paramters);

                if (shelfID == 0)
                {
                    throw new Exception("Failed to create shelf");
                }
                else
                {
                    // If the shelf has bins, create them
                    if (requestDTO.Bins != null && requestDTO.Bins.Count > 0)
                    {
                        foreach(var bin in requestDTO.Bins)
                        {
                            await _warehouseBinRepository.CreateBin(shelfID, bin);
                        }
                    }
                }
            }
        }

        public Task DeleteShelf(int? shelfID, int? aisleID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                try
                {
                    connection.Open();
                    _warehouseBinRepository.DeleteBin(null, shelfID);
                    string query = @"
                           UPDATE warehouse_shelf
                           SET deleted = true";

                    if (shelfID != null)
                    {
                        query += " WHERE warehouse_shelf_id_pkey = @ShelfID;";
                    }
                    else if (aisleID != null)
                    {
                        query += " WHERE warehouse_aisle_id = @AisleID;";
                    }
                    else
                    {
                        throw new Exception("ShelfID or AisleID must be provided");
                    }
                    var parameters = new { ShelfID = shelfID };
                    return connection.ExecuteAsync(query, parameters);

                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to delete shelf", ex);
                }
            }
        }

        public async Task<WarehouseShelfResponseDTO> GetShelf(int shelfID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                string query = @"
                       SELECT     
                        warehouse_shelf_id_pkey AS ShelfID,
                        warehouse_shelf_name AS ShelfName,
                        warehouse_shelf_capacity AS ShelfCapacity
                        FROM warehouse_shelf
                        WHERE warehouse_shelf_id_pkey = @ShelfID;";

                var parameters = new { ShelfID = shelfID };

                var shelfResult = await connection.QueryFirstOrDefaultAsync<WarehouseShelfResponseDTO>(query, parameters);
                if (shelfResult == null)
                {
                    throw new Exception("Shelf does not exist");
                }
                shelfResult.Bins = await _warehouseBinRepository.GetBins(shelfID);
                return shelfResult;
            }
        }

        public async Task<List<WarehouseShelfResponseDTO>> GetShelves(int aisleID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                string query = @"
                       SELECT     
                        warehouse_shelf_id_pkey AS ShelfID,
                        warehouse_shelf_name AS ShelfName,
                        warehouse_shelf_capacity AS ShelfCapacity
                        FROM warehouse_shelf
                        WHERE warehouse_aisle_id = @AisleID;";

                var parameters = new { AisleID = aisleID };

                var shelfResult = await connection.QueryAsync<WarehouseShelfResponseDTO>(query, parameters);
                if (shelfResult == null)
                {
                    throw new Exception("Shelf does not exist");
                }

                foreach (var shelf in shelfResult)
                {
                    shelf.Bins = await _warehouseBinRepository.GetBins(shelf.ShelfID);
                }

                return shelfResult.ToList();
            }
        }

        public async Task UpdateShelf(int shelfID, int aisleID, WarehouseShelfRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string query = @"
                        UPDATE warehouse_shelf
                        SET shelf_name = @ShelfName,
                            warehouse_aisle_id = @AisleID
                        WHERE warehouse_shelf_id = @ShelfID;";

                var parameters = new
                {
                    ShelfName = requestDTO.ShelfName,
                    AisleID = aisleID,
                    ShelfID = shelfID
                };

                await connection.ExecuteAsync(query, parameters);
            }
        }
    }
}
