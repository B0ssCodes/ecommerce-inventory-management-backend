using Dapper;
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

        // This method will be called from the room repository
        public async Task CreateAisle(int roomID, WarehouseAisleRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction)
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
                // Create the aisle in the room and return the aisleID
                string createQuery = @"
            INSERT INTO warehouse_aisle (aisle_name, warehouse_room_id)
            VALUES (@AisleName, @RoomID)
            RETURNING warehouse_aisle_id_pkey;";

                var parameters = new { AisleName = requestDTO.AisleName, RoomID = roomID };

                var aisleID = await connection.QueryFirstOrDefaultAsync<int>(createQuery, parameters, transaction);

                if (aisleID == 0)
                {
                    throw new Exception("Failed to create aisle");
                }
                else
                {
                    // If the aisle has shelves, create them
                    if (requestDTO.Shelves != null && requestDTO.Shelves.Count > 0)
                    {
                        foreach (var shelf in requestDTO.Shelves)
                        {
                            await _shelfRepository.CreateShelf(aisleID, shelf, connection, transaction);
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

        public async Task DeleteAisle(int? aisleID, int? roomID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                try
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        if (roomID.HasValue)
                        {
                            // Fetch aisle IDs associated with the room
                            string fetchAislesQuery = @"
                        SELECT warehouse_aisle_id_pkey
                        FROM warehouse_aisle
                        WHERE warehouse_room_id = @RoomID AND deleted = false;";

                            var aisleIDs = await connection.QueryAsync<int>(fetchAislesQuery, new { RoomID = roomID }, transaction);

                            foreach (var id in aisleIDs)
                            {
                                await _shelfRepository.DeleteShelf(null, id);
                            }
                        }
                        else if (aisleID.HasValue)
                        {
                            await _shelfRepository.DeleteShelf(null, aisleID);
                        }
                        else
                        {
                            throw new Exception("Invalid parameters");
                        }

                        // Build the dynamic delete query
                        string deleteQuery = @"
                    UPDATE warehouse_aisle
                    SET deleted = true";

                        if (aisleID.HasValue)
                        {
                            deleteQuery += " WHERE warehouse_aisle_id_pkey = @AisleID";
                        }
                        else if (roomID.HasValue)
                        {
                            deleteQuery += " WHERE warehouse_room_id = @RoomID";
                        }

                        var parameters = new { AisleID = aisleID, RoomID = roomID };

                        await connection.ExecuteAsync(deleteQuery, parameters, transaction);

                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to delete aisle", ex);
                }
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
                        WHERE warehouse_aisle_id_pkey = @AisleID AND deleted = false;";

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
                        SELECT warehouse_aisle_id_pkey AS AisleID,
                                aisle_name AS AisleName
                        FROM warehouse_aisle
                        WHERE warehouse_room_id = @RoomID AND deleted = false;";

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
