using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using MyShop.Services.Flowers;
using System.Linq;
using System.Security.Claims;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class CategoryController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ICategoryService _categoryService; // Correct spelling and added interface

        public CategoryController(FlowershopContext context, ICategoryService categoryService) // Use interface here
        {
            _context = context;
            _categoryService = categoryService;
        }


        [HttpPost("CreateCategory")]
        [Authorize(Roles = "admin")] // Only admins can access this route
        public async Task<IActionResult> CreateCategory([FromForm] CreateCategoryDto categoryDto)
        {
            // Check if the user is an admin
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (userRole != "admin")
            {
                return Forbid("Bạn không có quyền truy cập thông tin này."); // Forbidden response for non-admins
            }

            if (string.IsNullOrWhiteSpace(categoryDto.CategoryName))
            {
                return BadRequest(new { message = "Category name is required." });
            }

            var newCategory = new Category
            {
                CategoryName = categoryDto.CategoryName
            };

            var createdCategory = await _categoryService.CreateCategoryAsync(newCategory);

            return CreatedAtAction(nameof(GetFlowersByCategory), new { categoryId = createdCategory.CategoryId }, new
            {
                CategoryId = createdCategory.CategoryId,
                CategoryName = createdCategory.CategoryName,
                message = "Category created successfully"
            });
        }

        // GET api/category/{categoryId}/flowers
        [HttpGet("{categoryId}/flowers")]
        public IActionResult GetFlowersByCategory(int categoryId)
        {
            // Fetch flowers that belong to the given categoryId
            var flowers = _categoryService.GetFlowersByCategoryId(categoryId);

            if (flowers == null || !flowers.Any())
            {
                return NotFound(new { message = "No flowers found for this category." });
            }

            // Return the list of flowers
            return Ok(flowers);
        }

        [HttpGet("GetAllCategories")]
        public IActionResult GetAllCategories()
        {
            // Lấy tất cả danh mục từ service
            var categories = _categoryService.GetAllCategories();

            // Nếu không có danh mục nào, trả về 404
            if (categories == null || !categories.Any())
            {
                return NotFound(new { message = "No categories found." });
            }

            // Trả về danh sách các danh mục
            return Ok(categories);
        }

        [HttpPut("UpdateCategory/{categoryId}")]
        [Authorize(Roles = "admin")] // Chỉ admin có thể cập nhật danh mục
        public async Task<IActionResult> UpdateCategory(int categoryId, [FromForm] UpdateCategoryDto categoryDto)
        {
            // Kiểm tra xem người dùng có phải admin không
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (userRole != "admin")
            {
                return Forbid("Bạn không có quyền truy cập thông tin này."); // Trả về Forbidden cho những ai không phải admin
            }

            if (string.IsNullOrWhiteSpace(categoryDto.CategoryName))
            {
                return BadRequest(new { message = "Category name is required." });
            }

            // Tìm category trong cơ sở dữ liệu
            var existingCategory = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (existingCategory == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            // Cập nhật thông tin category
            existingCategory.CategoryName = categoryDto.CategoryName;

            // Lưu thay đổi
            await _categoryService.UpdateCategoryAsync(existingCategory);

            return Ok(new
            {
                CategoryId = existingCategory.CategoryId,
                CategoryName = existingCategory.CategoryName,
                message = "Category updated successfully"
            });
        }

        [HttpDelete("DeleteCategory/{categoryId}")]
        [Authorize(Roles = "admin")] // Chỉ admin mới có thể xóa danh mục
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            // Kiểm tra xem người dùng có phải admin không
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (userRole != "admin")
            {
                return Forbid("Bạn không có quyền truy cập thông tin này."); // Trả về Forbidden nếu không phải admin
            }

            // Tìm danh mục trong cơ sở dữ liệu
            var category = await _categoryService.GetCategoryByIdAsync(categoryId);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            // Xóa danh mục
            await _categoryService.DeleteCategoryAsync(categoryId);

            return Ok(new { message = "Category deleted successfully" });
        }





    }
}