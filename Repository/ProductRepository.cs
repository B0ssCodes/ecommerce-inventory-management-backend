﻿using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly DapperContext _db;
        private readonly IConfiguration _configuration;

        public ProductRepository(DapperContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }
        public async Task<ProductResponseDTO> CreateProduct(ProductRequestDTO productRequestDTO)
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

                        // Delete the product
                        var deleteProductQuery = @"
                DELETE FROM product
                WHERE product_id_pkey = @ProductID";

                        await connection.ExecuteAsync(deleteProductQuery, new { ProductID = productID }, transaction);

                        // Commit the transaction when done
                        transaction.Commit();

                        // Get the images directory from the configuration
                        var imagesDirectory = _configuration["ImagesDirectory"];
                        if (string.IsNullOrEmpty(imagesDirectory))
                        {
                            throw new Exception("Images directory is not configured.");
                        }

                        // Construct the product directory using the productID
                        var productDirectory = Path.Combine(imagesDirectory, productID.ToString());

                        // Delete the product directory and all its contents
                        if (Directory.Exists(productDirectory))
                        {
                            Directory.Delete(productDirectory, true);
                        }

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

                // Fetch the images related to the product and serve them as static files
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


        public async Task<List<AllProductResponseDTO>> GetProducts(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open(); // Explicitly open the connection

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;

                var query = @"
        SELECT 
            p.product_id_pkey AS ProductID, 
            p.sku AS SKU, 
            p.product_name AS Name, 
            p.product_description AS Description, 
            p.product_price AS Price, 
            p.product_cost_price AS Cost, 
            p.category_id AS CategoryID,
            (SELECT COUNT(*) FROM image WHERE product_id = p.product_id_pkey) AS ImageCount,
            (SELECT COUNT(*) FROM product) AS ProductCount
        FROM product p
        WHERE (@SearchQuery IS NULL OR p.product_name ILIKE '%' || @SearchQuery || '%' 
               OR p.product_description ILIKE '%' || @SearchQuery || '%' 
               OR p.sku ILIKE '%' || @SearchQuery || '%')
        ORDER BY p.product_id_pkey
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

                var products = (await connection.QueryAsync<AllProductResponseDTO>(query, new { Offset = offset, PageSize = paginationParams.PageSize, SearchQuery = paginationParams.Search })).ToList();

                if (products == null)
                {
                    return new List<AllProductResponseDTO>(); // Return an empty list if no products are found
                }

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
