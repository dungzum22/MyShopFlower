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
        private readonly CategoryService _catagoryService;

        public CategoryController(FlowershopContext context, CategoryService categoryService)
        {
            _context = context;
            _catagoryService = categoryService;
        }

        // GET api/category/{categoryId}/flowers
        [HttpGet("{categoryId}/flowers")]
        public IActionResult GetFlowersByCategory(int categoryId)
        {
            // Fetch flowers that belong to the given categoryId
            var flowers = _catagoryService.GetFlowersByCategoryId(categoryId);

            if (flowers == null || !flowers.Any())
            {
                return NotFound(new { message = "No flowers found for this category." });
            }

            // Return the list of flowers
            return Ok(flowers);
        }
    }
}
