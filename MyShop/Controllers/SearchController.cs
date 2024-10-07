using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyShop.DataContext;
using MyShop.Entities;
using MyShop.Services;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly SearchService _searchService;

        public SearchController(FlowershopContext context, SearchService searchService)
        {
            _context = context;
            _searchService = searchService;
        }


        //[HttpGet("{categoryId}/flowers")]
        //public IActionResult GetFlowersByCategory(int categoryId)
        //{
        //    // Fetch flowers that belong to the given categoryId
        //    var flowers = _catagoryService.GetFlowersByCategoryId(categoryId);

        //    if (flowers == null || !flowers.Any())
        //    {
        //        return NotFound(new { message = "No flowers found for this category." });
        //    }

        //    // Return the list of flowers
        //    return Ok(flowers);
        //}

        [HttpGet("Search/{name}")]
        public IActionResult Seach(string name)
        {
            // Fetch the flower by ID
            var flower = _searchService.SearchFlowers(name);

            if (flower == null)
            {
                return NotFound(new { message = "Flower not found" });
            }

            // Return flower information
            return Ok(new
            {
               flower
            });
        }

    }
}
