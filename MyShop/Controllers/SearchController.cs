//using Microsoft.AspNetCore.Mvc;
using MyShop.DataContext;
using MyShop.Services.Flowers;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ISearchService _searchService;

        // Constructor injection for context and search service
        public SearchController(FlowershopContext context, ISearchService searchService) // Changed to interface
        {
            _context = context;
            _searchService = searchService;
        }



        // GET api/search/Search/{name}
        [HttpGet("Search/{name}")]
        public IActionResult Search(string name)
        {
            // Fetch flowers matching the search query (name)
            var flowers = _searchService.SearchFlowers(name);

            // Check if no flowers were found
            if (!flowers.Any())
            {
                return NotFound(new { message = "No flowers found matching the search criteria." });
            }

            // Return the list of flowers
            return Ok(flowers);
        }
    }
}