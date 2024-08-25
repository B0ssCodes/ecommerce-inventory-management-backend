using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Diagnostics;

namespace Inventory_Management_Backend.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly DapperContext _db;
        private readonly IConfiguration _configuration;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IInventoryLocationRepository _inventoryLocationRepository;

        public ProductRepository(DapperContext db, IConfiguration configuration, IInventoryRepository inventoryRepository, IInventoryLocationRepository inventoryLocationRepository)
        {
            _db = db;
            _configuration = configuration;
            _inventoryRepository = inventoryRepository;
            _inventoryLocationRepository = inventoryLocationRepository;
        }
        public async Task CreateProduct(ProductRequestDTO productRequestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection, didnt work without this

                // Start a transaction to ensure data consistency in dapper
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if a product with the same SKU already exists
                        var checkSkuQuery = @"
                    SELECT EXISTS (
                        SELECT 1
                        FROM Product
                        WHERE sku = @SKU
                    )";

                        var skuExists = await connection.ExecuteScalarAsync<bool>(checkSkuQuery, new
                        {
                            productRequestDTO.SKU
                        }, transaction);

                        // If a product with the same SKU exists, throw an exception
                        if (skuExists)
                        {
                            throw new Exception("A product with the same SKU already exists.");
                        }

                        // Insert the product and return all available details
                        var query = @"
                INSERT INTO Product (sku, product_name, product_description, product_price, product_cost_price, category_id)
                VALUES (@SKU, @Name, @Description, @Price, @Cost, @CategoryID)
                RETURNING product_id_pkey AS ProductID, sku AS SKU, product_name as Name, product_description as Description, product_price AS Price, product_cost_price as Cost, category_id AS CategoryID";

                        var insertedProduct = await connection.QuerySingleOrDefaultAsync<ProductResponseDTO>(query, new
                        {
                            productRequestDTO.SKU,
                            productRequestDTO.Name,
                            productRequestDTO.Description,
                            productRequestDTO.Price,
                            productRequestDTO.Cost,
                            productRequestDTO.CategoryID
                        }, transaction);

                        if (insertedProduct == null)
                        {
                            throw new Exception("Product creation failed");
                        }

                        // Get the images directory from the configuration file (appsettings.json)
                        var imagesDirectory = _configuration["ImagesDirectory"];
                        if (string.IsNullOrEmpty(imagesDirectory))
                        {
                            throw new Exception("Images directory is not configured.");
                        }

                        // Create a directory for the product if it doesn't exist
                        // it should exist as program.cs will create it but just to make sure
                        // The directory create will have for a name the productID
                        var productDirectory = Path.Combine(imagesDirectory, insertedProduct.ProductID.ToString());
                        if (!Directory.Exists(productDirectory))
                        {
                            Directory.CreateDirectory(productDirectory);
                        }

                        // Save images locally and insert realtive image paths into the database
                        var imageQuery = @"
                INSERT INTO image (product_id, image_url)
                VALUES (@ProductID, @ImageUrl)";

                        foreach (var imageRequest in productRequestDTO.Images)
                        {
                            var image = imageRequest;

                            // Generate a unique name for the image
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";

                            // Construct the file path with the directory and the new file name
                            var filePath = Path.Combine(productDirectory, uniqueFileName);

                            // Save the image locally
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            // Create a relative URL for the image, replacing backslashes with forward slashes
                            var relativeImageUrl = Path.Combine("/images", insertedProduct.ProductID.ToString(), uniqueFileName).Replace("\\", "/");

                            // Insert relative image path into the database
                            await connection.ExecuteAsync(imageQuery, new
                            {
                                ProductID = insertedProduct.ProductID,
                                ImageUrl = relativeImageUrl
                            }, transaction);
                        }

                            await _inventoryRepository.CreateInventory(insertedProduct.ProductID, connection, transaction);

                        // Commit the transaction
                        transaction.Commit();
                    }
                    catch
                    {
                        // Rollback the transaction if any error occurs
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task DeleteProduct(int productID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var checkInventoryQuery = @"
            SELECT EXISTS (
                SELECT 1
                FROM inventory
                WHERE product_id = @ProductID
                AND inventory_stock > 0
            )";

                var inventoryExists = await connection.ExecuteScalarAsync<bool>(checkInventoryQuery, new { ProductID = productID });

                if (inventoryExists)
                {
                    throw new Exception("This product is still in stock, please empty the stock to delete it!");
                }

                var deleteQuery = @"
            UPDATE product
            SET deleted = true
            WHERE product_id_pkey = @ProductID;";

                var parameters = new { ProductID = productID };

                await connection.QueryFirstOrDefaultAsync(deleteQuery, parameters);
            }
        }


        public async Task<ProductResponseDTO> GetProduct(int productID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                var query = @"
        SELECT 
            product_id_pkey AS ProductID, 
            sku AS SKU, 
            product_name AS Name, 
            product_description AS Description, 
            product_price AS Price, 
            product_cost_price AS Cost, 
            category_id AS CategoryID
        FROM product
        WHERE product_id_pkey = @ProductID";

                var product = await connection.QuerySingleOrDefaultAsync<ProductResponseDTO>(query, new { ProductID = productID });

                if (product == null)
                {
                    throw new Exception("Product not found");
                }

                // Fetch the images related to the product
                var fetchImagesQuery = @"
        SELECT image_id_pkey AS ImageID, image_url AS Url
        FROM image
        WHERE product_id = @ProductID";

                var images = (await connection.QueryAsync<ImageResponseDTO>(fetchImagesQuery, new { ProductID = productID })).ToList();
                product.Images = images;

                // Fetch the category details
                var fetchCategoryQuery = @"
        SELECT category_id_pkey AS CategoryID, category_name AS Name
        FROM category
        WHERE category_id_pkey = @CategoryID";

                var category = await connection.QuerySingleOrDefaultAsync<CategoryResponseDTO>(fetchCategoryQuery, new { CategoryID = product.CategoryID });

                if (category == null)
                {
                    throw new Exception("Category not found");
                }

                product.Category = category;

                return product;
            }
        }


        public async Task<(List<AllProductResponseDTO>, int)> GetProducts(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;
                var searchQuery = paginationParams.Search;

                // Define the mapping between frontend field names and database field names
                var fieldMapping = new Dictionary<string, string>
        {
            { "name", "p.product_name" },
            { "sku", "p.sku" },
            { "description", "p.product_description" },
            { "price", "p.product_price" },
            { "cost", "p.product_cost_price" }
        };

                var baseQuery = @"
        WITH ProductCTE AS (
            SELECT 
                p.product_id_pkey AS ProductID, 
                p.sku AS SKU, 
                p.product_name AS Name, 
                p.product_description AS Description, 
                p.product_price AS Price, 
                p.product_cost_price AS Cost, 
                p.category_id AS CategoryID,
                (SELECT COUNT(*) FROM image WHERE product_id = p.product_id_pkey) AS ImageCount,
                COUNT(*) OVER() AS TotalCount
            FROM product p
            WHERE (@SearchQuery IS NULL OR p.product_name ILIKE '%' || @SearchQuery || '%' 
                   OR p.product_description ILIKE '%' || @SearchQuery || '%' 
                   OR p.sku ILIKE '%' || @SearchQuery || '%')
            AND deleted = false
        ";

                // Add dynamic filters
                if (paginationParams.Filters != null && paginationParams.Filters.Count > 0)
                {
                    foreach (Filter filter in paginationParams.Filters)
                    {
                        string field = "";
                        if (fieldMapping.ContainsKey(filter.Field.ToLower()))
                        {
                            field = fieldMapping[filter.Field.ToLower()];
                        }

                        // Use parameterized queries to safely include the value
                        baseQuery += $" AND {field} {filter.Operator} '{filter.Value}'";
                    }
                }

                baseQuery += @")
        SELECT ProductID, SKU, Name, Description, Price, Cost, CategoryID, ImageCount, TotalCount
        FROM ProductCTE";

                if (paginationParams.SortBy != null)
                {
                    string sortBy = char.ToUpper(paginationParams.SortBy[0]) + paginationParams.SortBy.Substring(1);
                    baseQuery += " ORDER BY " + sortBy + " " + (paginationParams.SortOrder == "asc" ? "ASC" : "DESC");
                }
                else
                {
                    baseQuery += " ORDER BY ProductID DESC";
                }

                baseQuery += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    Offset = offset,
                    PageSize = paginationParams.PageSize,
                    SearchQuery = searchQuery
                };

                var result = await connection.QueryAsync<AllProductResponseDTO, long, (AllProductResponseDTO, long)>(
                    baseQuery,
                    (product, totalCount) => (product, totalCount),
                    parameters,
                    splitOn: "TotalCount"
                );

                var products = result.Select(r => r.Item1).ToList();
                int totalCount = result.Any() ? (int)result.First().Item2 : 0; // Explicitly cast to int

                var fetchCategoryQuery = @"
        SELECT category_id_pkey AS CategoryID, category_name AS Name
        FROM category
        WHERE category_id_pkey = @CategoryID";

                foreach (var product in products)
                {
                    // Fetch category
                    var category = await connection.QuerySingleOrDefaultAsync<CategoryResponseDTO>(fetchCategoryQuery, new { CategoryID = product.CategoryID });

                    if (category == null)
                    {
                        throw new Exception($"Category not found for product ID {product.ProductID}");
                    }

                    product.Category = category;
                }

                return (products, totalCount);
            }
        }

        public async Task<(List<ProductSelectResponseDTO>, int itemCount)> GetProductsSelect(int transactionTypeID, PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;

                var query = @"
        WITH ProductCTE AS (
            SELECT 
                p.product_id_pkey AS ProductID, 
                p.sku AS SKU, 
                p.product_name AS Name,  
                CASE 
                    WHEN @TransactionTypeID = 1 THEN p.product_cost_price 
                    WHEN @TransactionTypeID = 2 THEN p.product_price 
                END AS Cost,
                COALESCE(i.inventory_stock, 0) AS Quantity,
                c.category_name AS Category,
                COUNT(*) OVER() AS TotalCount
            FROM product p
            JOIN category c ON p.category_id = c.category_id_pkey
            LEFT JOIN inventory i ON p.product_id_pkey = i.product_id
            WHERE (@SearchQuery IS NULL OR p.product_name ILIKE '%' || @SearchQuery || '%' 
                   OR p.product_description ILIKE '%' || @SearchQuery || '%' 
                   OR p.sku ILIKE '%' || @SearchQuery || '%')
               AND p.deleted = false
        )
        SELECT ProductID, SKU, Name, Cost, Quantity, Category, TotalCount
        FROM ProductCTE
        ORDER BY ProductID DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    Offset = offset,
                    PageSize = paginationParams.PageSize,
                    SearchQuery = paginationParams.Search,
                    TransactionTypeID = transactionTypeID
                };

                var result = await connection.QueryAsync<ProductSelectResponseDTO, long, (ProductSelectResponseDTO, long)>(
                    query,
                    (product, totalCount) => (product, totalCount),
                    parameters,
                    splitOn: "TotalCount"
                );

                var products = result.Select(r => r.Item1).ToList();
                int totalCount = result.Any() ? (int)result.First().Item2 : 0; // Explicitly cast to int

                return (products, totalCount);
            }
        }


        public async Task UpdateProduct(int productID, ProductRequestDTO productRequestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Fetch existing images to delete them from the local directory
                        var fetchExistingImagesQuery = @"
                            SELECT image_url AS Url
                            FROM image
                            WHERE product_id = @ProductID";

                        var existingImages = (await connection.QueryAsync<string>(fetchExistingImagesQuery, new { ProductID = productID }, transaction)).ToList();

                        // Delete existing images from the database
                        var deleteImagesQuery = @"
                            DELETE FROM image
                            WHERE product_id = @ProductID";

                        await connection.ExecuteAsync(deleteImagesQuery, new { ProductID = productID }, transaction);

                        // Get the images directory from the configuration
                        var imagesDirectory = _configuration["ImagesDirectory"];
                        if (string.IsNullOrEmpty(imagesDirectory))
                        {
                            throw new Exception("Images directory is not configured.");
                        }

                        // Construct the product directory path
                        var productDirectory = Path.Combine(imagesDirectory, productID.ToString());

                        // Delete the product directory and all its contents
                        if (Directory.Exists(productDirectory))
                        {
                            Directory.Delete(productDirectory, true);
                        }

                        // Create a new directory for the product
                        Directory.CreateDirectory(productDirectory);

                        // Insert new images
                        var insertImageQuery = @"
                INSERT INTO image (product_id, image_url)
                VALUES (@ProductID, @ImageUrl)";

                        foreach (var imageRequest in productRequestDTO.Images)
                        {
                            var image = imageRequest;

                            // Generate a unique name for the image
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                            var filePath = Path.Combine(productDirectory, uniqueFileName);

                            // Save the image locally
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            // Create a relative URL for the image
                            var relativeImageUrl = Path.Combine("/images", productID.ToString(), uniqueFileName).Replace("\\", "/");

                            // Insert image path into the database
                            await connection.ExecuteAsync(insertImageQuery, new
                            {
                                ProductID = productID,
                                ImageUrl = relativeImageUrl
                            }, transaction);
                        }

                        // Update the product details
                        var updateProductQuery = @"
                UPDATE product
                SET sku = @SKU, 
                    product_name = @Name, 
                    product_description = @Description, 
                    product_price = @Price, 
                    product_cost_price = @Cost, 
                    category_id = @CategoryID
                WHERE product_id_pkey = @ProductID
                RETURNING product_id_pkey AS ProductID, 
                          sku AS SKU, 
                          product_name AS Name, 
                          product_description AS Description, 
                          product_price AS Price, 
                          product_cost_price AS Cost, 
                          category_id AS CategoryID";

                        await connection.QuerySingleOrDefaultAsync<ProductResponseDTO>(updateProductQuery, new
                        {
                            productRequestDTO.SKU,
                            productRequestDTO.Name,
                            productRequestDTO.Description,
                            productRequestDTO.Price,
                            productRequestDTO.Cost,
                            productRequestDTO.CategoryID,
                            ProductID = productID
                        }, transaction);

                        // Commit the transaction
                        transaction.Commit();
                    }
                    catch
                    {
                        // Rollback the transaction if any error occurs
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
