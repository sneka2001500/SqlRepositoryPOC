using Microsoft.AspNetCore.Mvc;
using SqlRepositoryPOC.Models;
using SqlRepositoryPOC.Repositories;

namespace SqlRepositoryPOC.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<ProductController> _logger;

    public ProductController(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ILogger<ProductController> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }


    // ADD PRODUCT
    [HttpPost("addProduct")]
    public async Task<IActionResult> AddProduct(
        [FromBody] AddProductRequest body)
    {
        try
        {
            // Find category by name
            var category =
                await _categoryRepository
                    .GetByNameAsync(body.Category);

            if (category == null)
            {
                return BadRequest(new
                {
                    msg = "Category not found"
                });
            }

            // Find subcategory if provided
            Category? subcategory = null;

            if (!string.IsNullOrEmpty(body.Subcategory))
            {
                subcategory =
                    await _categoryRepository
                        .GetByNameAsync(body.Subcategory);

                if (subcategory == null)
                {
                    return BadRequest(new
                    {
                        msg = "Subcategory not found"
                    });
                }
            }

            var product = new Product
            {
                Name = body.Name,
                Sku = body.Sku,
                Description = body.Description,
                ShortDescription = body.ShortDescription,
                Colour = body.Colour,
                ColourCode = body.ColourCode,

                CategoryId = category.Id,

                SubCategoryId = subcategory?.Id,

                Price = Convert.ToDecimal(body.Price),

                Stock = body.Stock,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var imageUrls =
                body.Image ?? new List<string>();

            product =
                await _productRepository.AddAsync(
                    product,
                    imageUrls);

            return Ok(new
            {
                msg = "Product added",
                product
            });
        }
        catch (Exception error)
        {
            Console.WriteLine(
                $"Error adding product: {error.Message}");

            return StatusCode(500, new
            {
                msg = "Failed to add product",
                error = error.Message
            });
        }
    }


    // GET PRODUCTS BY CATEGORY
    [HttpGet("productsByCategory/{slug}")]
    public async Task<IActionResult> GetProductsByCategory(
        string slug)
    {
        try
        {
            _logger.LogInformation(
                "GetProductsByCategory API called");

            var products =
                await _productRepository
                    .GetByCategorySlugAsync(slug);

            return Ok(products);
        }
        catch (Exception error)
        {
            _logger.LogError(
                error,
                "Error fetching products by category");

            return StatusCode(500, new
            {
                msg = "Error fetching products",
                error = error.Message
            });
        }
    }


    // GET PRODUCT BY SKU
    [HttpGet("getProductBySku/{sku}")]
    public async Task<IActionResult> GetProductBySku(
        string sku)
    {
        try
        {
            var product =
                await _productRepository
                    .GetBySkuAsync(sku);

            if (product == null)
            {
                return NotFound(new
                {
                    msg = "Product not found"
                });
            }

            // Get images
            product.Image =
                await _productRepository
                    .GetImagesAsync(product.Id);

            // Get category
            Category? category = null;

            if (product.CategoryId.HasValue)
            {
                category =
                    await _categoryRepository
                        .GetByIdAsync(
                            product.CategoryId.Value);
            }

            // Get subcategory
            Category? subcategory = null;

            if (product.SubCategoryId.HasValue)
            {
                subcategory =
                    await _categoryRepository
                        .GetByIdAsync(
                            product.SubCategoryId.Value);
            }

            return Ok(new
            {
                product.Id,
                product.Name,
                product.Sku,
                product.Description,
                product.ShortDescription,
                product.Colour,
                product.ColourCode,
                product.Price,
                product.Image,
                product.Stock,
                product.CreatedAt,
                product.UpdatedAt,

                category,

                subCategory = subcategory
            });
        }
        catch (Exception error)
        {
            Console.WriteLine(
                $"Error fetching product by SKU: {error.Message}");

            return StatusCode(500, new
            {
                msg = "Error fetching product",
                error = error.Message
            });
        }
    }


    // GET ALL PRODUCTS
    [HttpGet("allProduct")]
    public async Task<IActionResult> GetAllProducts()
    {
        try
        {
            _logger.LogInformation(
                "GetAllProducts API called");

            var products =
                await _productRepository
                    .GetAllAsync();

            return Ok(products);
        }
        catch (Exception error)
        {
            _logger.LogError(
                error,
                "Error fetching all products");

            return StatusCode(500, new
            {
                msg = "Error fetching products",
                error = error.Message
            });
        }
    }


   [HttpPut("updateProduct")]
public async Task<IActionResult> UpdateProduct(
    [FromBody] UpdateProductRequest body)
{
    try
    {
        if (!int.TryParse(body.Id, out int productId))
        {
            return BadRequest(new
            {
                msg = "Invalid product ID"
            });
        }

        var existingProduct =
            await _productRepository
                .GetByIdAsync(productId);

        if (existingProduct == null)
        {
            return NotFound(new
            {
                msg = "Product updation failed"
            });
        }

        existingProduct.Name = body.Name;
        existingProduct.Description = body.Description;
        existingProduct.Price = Convert.ToDecimal(body.Price);
        existingProduct.Stock = body.Stock;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(body.SubCategory))
        {
            var subcategory =
                await _categoryRepository
                    .GetByNameAsync(body.SubCategory);

            if (subcategory == null)
            {
                return BadRequest(new
                {
                    msg = "Subcategory not found"
                });
            }

            existingProduct.SubCategoryId =
                subcategory.Id;
        }
        else
        {
            existingProduct.SubCategoryId = null;
        }

        var imageUrls =
            body.Image ?? new List<string>();

        var updatedProduct =
            await _productRepository.UpdateAsync(
                existingProduct,
                imageUrls);

        if (updatedProduct == null)
        {
            return NotFound(new
            {
                msg = "Product updation failed"
            });
        }

        updatedProduct.Image = imageUrls;

        return Ok(updatedProduct);
    }
    catch (Exception error)
    {
        Console.WriteLine(
            $"Error updating product: {error.Message}");

        return StatusCode(500, new
        {
            msg = "Error updating product",
            error = error.Message
        });
    }
}

[HttpPost("bulkInsert")]
public async Task<IActionResult> BulkInsert(
    [FromBody] List<Product> products)
{
    try
    {
        if (products == null || products.Count == 0)
        {
            return BadRequest(new
            {
                message = "Product list cannot be empty"
            });
        }

        var insertedCount =
            await _productRepository.BulkInsertAsync(products);

        return Ok(new
        {
            message = "Products inserted successfully",
            insertedCount
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = "Bulk insert failed. Transaction rolled back.",
            error = ex.Message
        });
    }
}
    // DELETE PRODUCT
    [HttpDelete("deleteProduct")]
    public async Task<IActionResult> DeleteProduct(
        [FromBody] DeleteProductRequest body)
    {
        try
        {
            if (!int.TryParse(
                    body.Id,
                    out int productId))
            {
                return BadRequest(new
                {
                    msg = "Invalid product ID"
                });
            }

            var deleted =
                await _productRepository
                    .DeleteAsync(productId);

            if (!deleted)
            {
                return NotFound(new
                {
                    msg = "Product deletion failed"
                });
            }

            return Ok(new
            {
                msg = "Product deleted"
            });
        }
        catch (Exception error)
        {
            Console.WriteLine(
                $"Error deleting product: {error.Message}");

            return StatusCode(500, new
            {
                msg = "Error deleting product",
                error = error.Message
            });
        }
    }


    // DELETE ALL PRODUCTS
    [HttpDelete("deleteAll")]
    public async Task<IActionResult> DeleteAllProducts()
    {
        try
        {
            var deletedCount =
                await _productRepository
                    .DeleteAllAsync();

            return Ok(new
            {
                msg = "deleted",
                deletedCount
            });
        }
        catch (Exception error)
        {
            Console.WriteLine(
                $"Error deleting all products: {error.Message}");

            return StatusCode(500, new
            {
                msg = "Product deletion failed",
                error = error.Message
            });
        }
    }
}


// REQUEST MODELS

public class AddProductRequest
{
    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string Colour { get; set; } = string.Empty;

    public string ColourCode { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? Subcategory { get; set; }

    public double Price { get; set; }

    public List<string>? Image { get; set; }

    public string Stock { get; set; } = string.Empty;
}


public class UpdateProductRequest
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string>? Image { get; set; }

    public double Price { get; set; }

    public string Stock { get; set; } = string.Empty;

    public string? SubCategory { get; set; }
}


public class DeleteProductRequest
{
    public string Id { get; set; } = string.Empty;
}