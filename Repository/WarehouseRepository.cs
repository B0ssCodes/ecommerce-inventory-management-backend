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
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert the warehouse's name and address first then return the new warehouse's ID
                        string createQuery = @"
                    INSERT INTO warehouse (warehouse_name, warehouse_address)
                    VALUES (@WarehouseName, @WarehouseAddress)
                    RETURNING warehouse_id_pkey;";

                        var parameters = new { WarehouseName = requestDTO.WarehouseName, WarehouseAddress = requestDTO.WarehouseAddress };

                        int warehouseID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters, transaction);

                        if (warehouseID == 0)
                        {
                            throw new Exception("Failed to create warehouse");
                        }

                        // If the warehouse has floors, create them
                        if (requestDTO.Floors != null && requestDTO.Floors.Count > 0)
                        {
                            foreach (var floor in requestDTO.Floors)
                            {
                                await _floorRepository.CreateFloor(warehouseID, floor, connection, transaction);
                            }
                        }

                        // Commit the transaction if everything is successful
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction if any error occurs
                        transaction.Rollback();
                        throw new Exception("Transaction failed and rolled back", ex);
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
                           warehouse_name as WarehouseName,
                           warehouse_address as WarehouseAddress   
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

        public async Task<(List<AllWarehouseResponseDTO>, int)> GetWarehouses(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string getQuery = @"
            SELECT w.warehouse_id_pkey as WarehouseID, 
                   w.warehouse_name as WarehouseName,
                   w.warehouse_address as WarehouseAddress,
                   COUNT(f.warehouse_floor_id_pkey) as FloorCount,
                   COUNT(*) OVER() AS TotalCount
            FROM warehouse w
            LEFT JOIN warehouse_floor f ON w.warehouse_id_pkey = f.warehouse_id
            WHERE w.deleted = false
            GROUP BY w.warehouse_id_pkey, w.warehouse_name, w.warehouse_address
            ORDER BY w.warehouse_id_pkey
            OFFSET @Offset ROWS
            FETCH NEXT @Limit ROWS ONLY";

                int offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;

                var parameters = new { Offset = offset, Limit = paginationParams.PageSize };

                var result = await connection.QueryAsync<AllWarehouseResponseDTO, long, (AllWarehouseResponseDTO, long)>(
                    getQuery,
                    (warehouse, totalCount) => (warehouse, totalCount),
                    parameters,
                    splitOn: "TotalCount"
                );

                var warehouses = result.Select(r => r.Item1).ToList();
                int totalCount = (int)result.FirstOrDefault().Item2;

                return (warehouses, totalCount);
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

                var parameters = new { WarehouseName = requestDTO.WarehouseName, WarehouseAddress = requestDTO.WarehouseAddress, WarehouseID = warehouseID };

                await connection.ExecuteAsync(updateQuery, parameters);
            }
        }
    }
}
