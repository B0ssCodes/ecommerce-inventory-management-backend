using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly DapperContext _db;
        private readonly IWarehouseFloorRepository _floorRepository;

        public WarehouseRepository(DapperContext db, IWarehouseFloorRepository floorRepository)
        {
            _db = db;
            _floorRepository = floorRepository;
        }

        public async Task CreateWarehouse(WarehouseRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string createQuery = @"
                    INSERT INTO warehouse (warehouse_name)
                    VALUES (@WarehouseName)
                    RETURNING warehouse_id_pkey;";

                var parameters = new { WarehouseName = requestDTO.Name };

                int warehouseID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters);

                if (warehouseID == 0)
                {
                    throw new Exception("Failed to create warehouse");
                }

                if (requestDTO.Floors != null && requestDTO.Floors.Count > 0)
                {
                    foreach (var floor in requestDTO.Floors)
                    {
                        await _floorRepository.CreateFloor(warehouseID, floor);
                    }
                }
            }
        }

        public async Task DeleteWarehouse(int warehouseID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                await _floorRepository.DeleteFloor(null, warehouseID);
                connection.Open();

                string deleteQuery = @"
                    UPDATE warehouse
                    set deleted = true
                    WHERE warehouse_id_pkey = @WarehouseID";

                var parameters = new { WarehouseID = warehouseID };

                await connection.ExecuteAsync(deleteQuery, parameters);
            }
        }

        public async Task<WarehouseResponseDTO> GetWarehouse(int warehouseID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string getQuery = @"
                    SELECT warehouse_id_pkey as WarehouseID, 
                           warehouse_name as Name
                    FROM warehouse
                    WHERE warehouse_id_pkey = @WarehouseID";

                var parameters = new { WarehouseID = warehouseID };

                var warehouseResult = await connection.QueryFirstOrDefaultAsync<WarehouseResponseDTO>(getQuery, parameters);

                if (warehouseResult == null)
                {
                    throw new Exception("Warehouse not found");
                }
                else
                {
                    warehouseResult.Floors = await _floorRepository.GetFloors(warehouseID);
                }

                return warehouseResult;
            }
        }

        public async Task<List<AllWarehouseResponseDTO>> GetWarehouses(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string getQuery = @"
                    SELECT warehouse_id_pkey as WarehouseID, 
                           warehouse_name as WarehouseName,
                           warehouse_address as WarehouseAddress
                    FROM warehouse
                    WHERE deleted = false
                    ORDER BY warehouse_id_pkey
                    OFFSET @Offset ROWS
                    FETCH NEXT @Limit ROWS ONLY";

                int offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;

                var parameters = new { Offset = offset, Limit = paginationParams.PageSize };

                var warehouses = await connection.QueryAsync<AllWarehouseResponseDTO>(getQuery, parameters);

                return warehouses.AsList();
            }
        }

        public async Task UpdateWarehouse(int warehouseID, WarehouseRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string updateQuery = @"
                    UPDATE warehouse
                    SET warehouse_name = @WarehouseName,
                        warehouse_address = @WarehouseAddress
                    WHERE warehouse_id_pkey = @WarehouseID";

                var parameters = new { WarehouseName = requestDTO.Name, WarehouseAddress = requestDTO.Address, WarehouseID = warehouseID };

                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }
    }
}
