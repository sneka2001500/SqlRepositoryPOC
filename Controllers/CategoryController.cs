using Microsoft.AspNetCore.Mvc;
using SqlRepositoryPOC.Models;
using SqlRepositoryPOC.Repositories;

namespace SqlRepositoryPOC.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryController(
        ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }


    // ADD CATEGORY
    [HttpPost("addCategory")]
    public async Task<IActionResult> AddCategory(
        [FromBody] AddCategoryRequest request)
    {
        try
        {
            int? parentCategoryId = null;

            // Find parent category if ParentName is provided
            if (!string.IsNullOrWhiteSpace(request.ParentName))
            {
                var parentCategory =
                    await _categoryRepository
                        .GetByNameAsync(request.ParentName);

                if (parentCategory == null)
                {
                    return BadRequest(new
                    {
                        msg = "Parent category not found"
                    });
                }

                parentCategoryId = parentCategory.Id;
            }

            var category = new Category
            {
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                ParentId = parentCategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            category =
                await _categoryRepository
                    .AddAsync(category);

            return StatusCode(201, new
            {
                msg = "Category added successfully",
                category
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            return StatusCode(500, new
            {
                msg = "Internal server error",
                error = ex.Message
            });
        }
    }


    // GET CATEGORY BY ID
    [HttpGet("getCategoryById/{id}")]
    public async Task<IActionResult> GetCategoryById(
        int id)
    {
        try
        {
            var category =
                await _categoryRepository
                    .GetByIdAsync(id);

            if (category == null)
            {
                return NotFound(new
                {
                    msg = "Category not found"
                });
            }

            Category? parentCategory = null;

            if (category.ParentId.HasValue)
            {
                parentCategory =
                    await _categoryRepository
                        .GetByIdAsync(
                            category.ParentId.Value);
            }

            return Ok(new
            {
                category,
                parent = parentCategory
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error fetching category: {ex}"
            );

            return StatusCode(500, new
            {
                msg = "Server error",
                error = ex.Message
            });
        }
    }


    // GET ALL CATEGORIES
    [HttpGet("getAllCategory")]
    public async Task<IActionResult> GetAllCategory()
    {
        try
        {
            var allCategories =
                await _categoryRepository
                    .GetAllAsync();

            return Ok(allCategories);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "Error fetching Categories"
            );

            return StatusCode(500, new
            {
                msg = "Server error",
                error = ex.Message
            });
        }
    }
}


// REQUEST MODEL

public class AddCategoryRequest
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ParentName { get; set; }
}