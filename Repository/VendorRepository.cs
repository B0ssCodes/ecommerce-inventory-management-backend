using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class VendorRepository : IVendorRepository
    {
        private readonly DapperContext _db;
        public VendorRepository(DapperContext db)
        {
            _db = db;
        }

        public async Task CreateVendor(VendorRequestDTO vendorRequestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                INSERT INTO vendor (vendor_name, vendor_email, vendor_phone_number, vendor_commercial_phone, vendor_address)
                VALUES (@Name, @Email, @Phone, @CommercialPhone, @Address)";

                var parameters = new
                {
                    Name = vendorRequestDTO.Name,
                    Email = vendorRequestDTO.Email,
                    Phone = vendorRequestDTO.Phone,
                    CommercialPhone = vendorRequestDTO.CommercialPhone,
                    Address = vendorRequestDTO.Address
                };

                await connection.ExecuteAsync(query, parameters);

            }
        }

        public async Task DeleteVendor(int vendorId)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                UPDATE vendor
                SET deleted = true
                WHERE vendor_id_pkey = @VendorID;";

                var parameters = new
                {
                    VendorID = vendorId
                };

                await connection.ExecuteAsync(query, parameters);
            }
        }

        public async Task<VendorResponseDTO> GetVendor(int vendorId)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                SELECT vendor_id_pkey AS VendorID, vendor_name AS Name, vendor_email AS Email,
                       vendor_phone_number AS Phone, vendor_commercial_phone AS CommercialPhone, vendor_address AS Address
                FROM vendor
                WHERE vendor_id_pkey = @VendorID AND deleted = false;";

                var parameters = new
                {
                    VendorID = vendorId
                };

                VendorResponseDTO vendorDTO = await connection.QueryFirstOrDefaultAsync<VendorResponseDTO>(query, parameters);

                if (vendorDTO == null)
                {
                    throw new Exception("Vendor not found");
                }

                return vendorDTO;
            }
        }

        public async Task<(List<VendorResponseDTO>, int)> GetVendors(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;
                var searchQuery = paginationParams.Search;

                var query = @"
        WITH VendorCTE AS (
            SELECT 
                vendor_id_pkey AS VendorID, 
                vendor_name AS Name, 
                vendor_email AS Email,
                vendor_phone_number AS Phone, 
                vendor_commercial_phone AS CommercialPhone, 
                vendor_address AS Address,
                COUNT(*) OVER() AS VendorCount
            FROM vendor
            WHERE (@SearchQuery IS NULL OR 
                   vendor_name ILIKE '%' || @SearchQuery || '%' OR 
                   vendor_email ILIKE '%' || @SearchQuery || '%' OR 
                   vendor_phone_number ILIKE '%' || @SearchQuery || '%' OR 
                   vendor_commercial_phone ILIKE '%' || @SearchQuery || '%' OR 
                   vendor_address ILIKE '%' || @SearchQuery || '%') 
            AND deleted = false    
        )
        SELECT VendorID, Name, Email, Phone, CommercialPhone, Address, VendorCount
        FROM VendorCTE
        ORDER BY VendorID DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    SearchQuery = searchQuery,
                    Offset = offset,
                    PageSize = paginationParams.PageSize
                };

                var result = await connection.QueryAsync<VendorResponseDTO, long, (VendorResponseDTO, long)>(
                    query,
                    (vendor, vendorCount) => (vendor, vendorCount),
                    parameters,
                    splitOn: "VendorCount"
                );

                var vendors = result.Select(r => r.Item1).ToList();
                int totalCount = result.Any() ? (int)result.First().Item2 : 0; // Explicitly cast to int

                return (vendors, totalCount);
            }
        }

        public async Task UpdateVendor(int vendorId, VendorRequestDTO vendorRequestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                UPDATE vendor
                SET vendor_name = @Name,
                    vendor_email = @Email,
                    vendor_phone_number = @Phone,
                    vendor_commercial_phone = @CommercialPhone,
                    vendor_address = @Address
                WHERE vendor_id_pkey = @VendorID;";

                var parameters = new
                {
                    VendorID = vendorId,
                    Name = vendorRequestDTO.Name,
                    Email = vendorRequestDTO.Email,
                    Phone = vendorRequestDTO.Phone,
                    CommercialPhone = vendorRequestDTO.CommercialPhone,
                    Address = vendorRequestDTO.Address
                };

                await connection.ExecuteAsync(query, parameters);
            }
        }
    }
}
