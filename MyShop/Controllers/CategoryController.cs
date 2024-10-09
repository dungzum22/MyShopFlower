using Microsoft.AspNetCore.Mvc;
using MyShop.DataContext;
using MyShop.Services.Flowers;
using System.Linq;

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
    }
}
