using Azure.Storage.Blobs;
using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Inventory_Management_Backend.Services;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly DapperContext _db;
        private readonly IBlobService _blobService;

        public ProductRepository(DapperContext db, IBlobService blobService)
        {
            _db = db;
            _blobService = blobService;
        }
        public async Task<ProductResponseDTO> CreateProduct(ProductRequestDTO productRequestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
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

                        // Upload images to Azure Blob Storage and insert image URLs into the database
                        var imageQuery = @"
                    INSERT INTO image (product_id, image_url)
                    VALUES (@ProductID, @ImageUrl)";

                        foreach (var imageRequest in productRequestDTO.Images)
                        {
                            var image = imageRequest;

                            // Generate a unique name for the image
                            var uniqueFileName = $"{insertedProduct.ProductID}/{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";

                            // Upload the image to Azure Blob Storage using BlobService
                            var imageUrl = await _blobService.UploadBlob(uniqueFileName, "product-images", image);

                            // Insert image URL into the database
                            await connection.ExecuteAsync(imageQuery, new
                            {
                                ProductID = insertedProduct.ProductID,
                                ImageUrl = imageUrl
                            }, transaction);
                        }

                        // Fetch the inserted images
                        var fetchImagesQuery = @"
                    SELECT image_id_pkey AS ImageID, image_url AS Url
                    FROM image
                    WHERE product_id = @ProductID";

                        var images = (await connection.QueryAsync<ImageResponseDTO>(fetchImagesQuery, new
                        {
                            ProductID = insertedProduct.ProductID
                        }, transaction)).ToList();

                        // Get the category details based on the passed ID
                        var categoryQuery = @"
                    SELECT category_id_pkey AS CategoryID, category_name AS Name
                    FROM Category
                    WHERE category_id_pkey = @CategoryID";

                        var category = await connection.QuerySingleOrDefaultAsync<CategoryResponseDTO>(categoryQuery, new
                        {
                            CategoryID = productRequestDTO.CategoryID
                        }, transaction);

                        if (category == null)
                        {
                            throw new Exception("Category not found");
                        }

                        // Commit the transaction
                        transaction.Commit();

                        // Set the category and images in the response DTO
                        insertedProduct.Category = category;
                        insertedProduct.Images = images;

                        return insertedProduct;
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

        public async Task<bool> DeleteProduct(int productID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if the product exists before deleting it
                        var checkProductQuery = @"
                    SELECT EXISTS (
                        SELECT 1
                        FROM product
                        WHERE product_id_pkey = @ProductID
                    )";

                        var productExists = await connection.ExecuteScalarAsync<bool>(checkProductQuery, new { ProductID = productID }, transaction);
                        if (!productExists)
                        {
                            throw new Exception("Product not found");
                        }

                        // Fetch existing images to delete them from Azure Blob Storage
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

                        // Delete existing images from Azure Blob Storage
                        foreach (var imageUrl in existingImages)
                        {
                            await _blobService.DeleteBlob(imageUrl, "product-images");
                        }

                        // Delete the product
                        var deleteProductQuery = @"
                DELETE FROM product
                WHERE product_id_pkey = @ProductID";

                        await connection.ExecuteAsync(deleteProductQuery, new { ProductID = productID }, transaction);

                        // Commit the transaction when done
                        transaction.Commit();
                        return true;
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
                    return null; // Or handle the case where the product is not found
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

        public async Task<List<ProductResponseDTO>> GetProducts(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;

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
            ORDER BY product_id_pkey
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

                var products = (await connection.QueryAsync<ProductResponseDTO>(query, new { Offset = offset, PageSize = paginationParams.PageSize })).ToList();

                if (products == null || products.Count == 0)
                {
                    return new List<ProductResponseDTO>(); // Return an empty list if no products are found
                }

                // Fetch the images and category details related to each product
                var fetchImagesQuery = @"
            SELECT image_id_pkey AS ImageID, image_url AS Url, product_id AS ProductID
            FROM image
            WHERE product_id = @ProductID";

                var fetchCategoryQuery = @"
            SELECT category_id_pkey AS CategoryID, category_name AS Name
            FROM category
            WHERE category_id_pkey = @CategoryID";

                foreach (var product in products)
                {
                    // Fetch images
                    var images = (await connection.QueryAsync<ImageResponseDTO>(fetchImagesQuery, new { ProductID = product.ProductID })).ToList();
                    product.Images = images;

                    // Fetch category
                    var category = await connection.QuerySingleOrDefaultAsync<CategoryResponseDTO>(fetchCategoryQuery, new { CategoryID = product.CategoryID });

                    if (category == null)
                    {
                        throw new Exception($"Category not found for product ID {product.ProductID}");
                    }

                    product.Category = category;
                }

                return products;
            }
        }

        public async Task<ProductResponseDTO> UpdateProduct(int productID, ProductRequestDTO productRequestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Fetch existing images to delete them from Azure Blob Storage
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

                        // Delete existing images from Azure Blob Storage
                        foreach (var imageUrl in existingImages)
                        {
                            await _blobService.DeleteBlob(imageUrl, "product-images");
                        }

                        // Insert new images
                        var insertImageQuery = @"
                INSERT INTO image (product_id, image_url)
                VALUES (@ProductID, @ImageUrl)";

                        foreach (var imageRequest in productRequestDTO.Images)
                        {
                            var image = imageRequest;

                            // Generate a unique name for the image
                            var uniqueFileName = $"{productID}/{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";

                            // Upload the image to Azure Blob Storage using BlobService
                            var imageUrl = await _blobService.UploadBlob(uniqueFileName, "product-images", image);

                            // Insert image URL into the database
                            await connection.ExecuteAsync(insertImageQuery, new
                            {
                                ProductID = productID,
                                ImageUrl = imageUrl
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

                        var updatedProduct = await connection.QuerySingleOrDefaultAsync<ProductResponseDTO>(updateProductQuery, new
                        {
                            productRequestDTO.SKU,
                            productRequestDTO.Name,
                            productRequestDTO.Description,
                            productRequestDTO.Price,
                            productRequestDTO.Cost,
                            productRequestDTO.CategoryID,
                            ProductID = productID
                        }, transaction);

                        if (updatedProduct == null)
                        {
                            throw new Exception("Product update failed");
                        }

                        // Fetch the updated images
                        var fetchImagesQuery = @"
                SELECT image_id_pkey AS ImageID, image_url AS Url
                FROM image
                WHERE product_id = @ProductID";

                        var images = (await connection.QueryAsync<ImageResponseDTO>(fetchImagesQuery, new { ProductID = productID }, transaction)).ToList();

                        // Get the category details based on the passed ID
                        var categoryQuery = @"
                SELECT category_id_pkey AS CategoryID, category_name AS Name
                FROM Category
                WHERE category_id_pkey = @CategoryID";

                        var category = await connection.QuerySingleOrDefaultAsync<CategoryResponseDTO>(categoryQuery, new
                        {
                            CategoryID = productRequestDTO.CategoryID
                        }, transaction);

                        if (category == null)
                        {
                            throw new Exception("Category not found");
                        }

                        // Commit the transaction
                        transaction.Commit();

                        // Set the category and images in the response DTO
                        updatedProduct.Category = category;
                        updatedProduct.Images = images;

                        return updatedProduct;
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
