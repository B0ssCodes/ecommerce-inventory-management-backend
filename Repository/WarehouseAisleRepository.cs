﻿using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class WarehouseAisleRepository : IWarehouseAisleRepository
    {
        private readonly DapperContext _db;
        private readonly IWarehouseShelfRepository _shelfRepository;
        public WarehouseAisleRepository(DapperContext db, IWarehouseShelfRepository shelfRepository)
        {
            _db = db;
            _shelfRepository = shelfRepository;
        }
        public async Task CreateAisle(int roomID, WarehouseAisleRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                string createQuery = @"
                        INSERT INTO warehouse_aisle (aisle_name, warehouse_room_id)
                        VALUES (@AisleName, @RoomID)
                        RETURNING warehouse_aisle_id_pkey;";

                var parameters = new { AisleName = requestDTO.AisleName, RoomID = roomID };

                var aisleID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters);

                if (aisleID == 0)
                {
                    throw new Exception("Failed to create aisle");
                }
                else
                {
                    if (requestDTO.Shelves != null && requestDTO.Shelves.Count > 0)
                    {
                        foreach (var shelf in requestDTO.Shelves)
                        {
                            await _shelfRepository.CreateShelf(aisleID, shelf);
                        }
                    }
                }
            }
        }

        public async Task DeleteAisle(int? aisleID, int? roomID)
        {
            try
            {
                using (IDbConnection connection = _db.CreateConnection())
                { 
                    await _shelfRepository.DeleteShelf(null, aisleID);

                    connection.Open();

                    string deleteQuery = @"
                        UPDATE warehouse_aisle
                        SET deleted = true";

                    if (roomID != null)
                    {
                        deleteQuery += " WHERE warehouse_room_id = @RoomID";
                    }
                    else if (aisleID != null)
                    {
                        deleteQuery += " WHERE warehouse_aisle_id_pkey = @AisleID";
                    }
                    else
                    {
                        throw new Exception("Invalid Parameters");
                    }

                    var parameters = new { RoomID = roomID, AisleID = aisleID };

                    await connection.ExecuteAsync(deleteQuery, parameters);
                }
            }
            catch (Exception ex)
            {
                throw new Exception ("Failed to delete aisle", ex) ;
            }
        }

        public async Task<WarehouseAisleResponseDTO> GetAisle(int aisleID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string query = @"
                        SELECT warehouse_aisle_id_pkey AS AisleID, aisle_name AS AisleName
                        FROM warehouse_aisle
                        WHERE warehouse_aisle_id_pkey = @AisleID";

                var parameters = new { AisleID = aisleID };

                var aisleResult = await connection.QueryFirstOrDefaultAsync<WarehouseAisleResponseDTO>(query, parameters);

                if (aisleResult == null)
                {
                    throw new Exception("Aisle not found");
                }

                aisleResult.Shelves = await _shelfRepository.GetShelves(aisleID);

                return aisleResult;
            }
        }

        public async Task<List<WarehouseAisleResponseDTO>> GetAisles(int roomID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string query = @"
                        SELECT warehouse_aisle_id_pkey AS AisleID, aisle_name AS AisleName
                        FROM warehouse_aisle
                        WHERE warehouse_room_id = @RoomID";

                var parameters = new { RoomID = roomID };

                var aisles = await connection.QueryAsync<WarehouseAisleResponseDTO>(query, parameters);

                if (aisles == null)
                {
                    throw new Exception("No Aisles were found");
                }
                foreach (var aisle in aisles)
                {
                    aisle.Shelves = await _shelfRepository.GetShelves(aisle.AisleID);
                }

                return aisles.ToList();
            }
        }

        public async Task UpdateAisle(int aisleID, WarehouseAisleRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                string query = @"
                        UPDATE warehouse_aisle
                        SET aisle_name = @AisleName
                        WHERE warehouse_aisle_id_pkey = @AisleID;";

                var parameters = new { AisleName = requestDTO.AisleName, AisleID = aisleID };

                await connection.ExecuteAsync(query, parameters);
            }
        }
    }
}
