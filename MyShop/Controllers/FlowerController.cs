using Microsoft.AspNetCore.Mvc;
using MyShop.DataContext;
using MyShop.Entities;
using MyShop.Services.Flowers;

[ApiController]
[Route("api/[controller]")]
public class FlowerInfoController : ControllerBase
{
    private readonly FlowershopContext _context;
    private readonly IFlowerService _flowerService;

    // Constructor injection for both context and flower service
    public FlowerInfoController(FlowershopContext context, IFlowerService flowerService)
    {
        _context = context;
        _flowerService = flowerService;
    }

    // POST api/flowerinfo
    [HttpPost("Create")]
    public IActionResult CreateFlower([FromForm] string flowername, [FromForm] string flowerdiscrpt, [FromForm] decimal price, [FromForm] int quantity, [FromForm] int catagory)
    {
        // Create a new flower
        var newFlower = new FlowerInfo
        {
            FlowerName = flowername,
            FlowerDescription = flowerdiscrpt,
            Price = price,
            CreatedAt = DateTime.UtcNow,
            CategoryId = catagory,
            AvailableQuantity = quantity,
        };

        var createdFlower = _flowerService.CreateFlower(newFlower);

        // Return the created flower information
        return Ok(new
        {
            FlowerID = createdFlower.FlowerId,
            FlowerName = createdFlower.FlowerName,
            FlowerDescription = createdFlower.FlowerDescription,
            Price = createdFlower.Price,
            AvailableQuantity = createdFlower.AvailableQuantity,
            CategoryID = createdFlower.CategoryId,
            message = "Flower created successfully"
        });
    }

    // GET api/flowerinfo/{id}
    [HttpGet("Search/{id}")]
    public IActionResult GetFlower(int id)
    {
        // Fetch the flower by ID
        var flower = _flowerService.GetFlowerById(id);

        if (flower == null)
        {
            return NotFound(new { message = "Flower not found" });
        }

        // Return flower information
        return Ok(new
        {
            FlowerID = flower.FlowerId,
            FlowerName = flower.FlowerName,
            FlowerDescription = flower.FlowerDescription,
            Price = flower.Price,
            AvailableQuantity = flower.AvailableQuantity,
            CategoryID = flower.CategoryId
        });
    }

    //[HttpPost("Update/{id}")]
    //public IActionResult UpdateFlower(int id, [FromForm] string flowername, [FromForm] string flowerdiscrpt, [FromForm] decimal price, [FromForm] int quantity)
    //{
    //    // Fetch the flower by ID
    //    var flower = _flowerService.GetFlowerById(id);

    //    if (flower == null)
    //    {
    //        return NotFound(new { message = "Flower not found" });
    //    }

    //    // Update the flower properties with new values
    //    flower.FlowerName = flowername;
    //    flower.FlowerDescription = flowerdiscrpt;
    //    flower.Price = price;
    //    flower.AvailableQuantity = quantity;

    //    // Save the updated flower in the database (this method should be in your service/repository layer)
    //    _flowerService.UpdateFlower(flower);

    //    // Return a success response
    //    return Ok(new { message = "Flower updated successfully" });
    //}

    [HttpPost("Update/{id}")]
    public IActionResult UpdateFlower(int id, [FromForm] string? flowername, [FromForm] string? flowerdiscrpt, [FromForm] decimal? price, [FromForm] int? quantity)
    {
        // Fetch the flower by ID
        var flower = _flowerService.GetFlowerById(id);

        if (flower == null)
        {
            return NotFound(new { message = "Flower not found" });
        }

        // Update the flower properties with new values if provided, otherwise keep the old values
        flower.FlowerName = !string.IsNullOrEmpty(flowername) ? flowername : flower.FlowerName;
        flower.FlowerDescription = !string.IsNullOrEmpty(flowerdiscrpt) ? flowerdiscrpt : flower.FlowerDescription;
        flower.Price = price.HasValue ? price.Value : flower.Price;
        flower.AvailableQuantity = quantity.HasValue ? quantity.Value : flower.AvailableQuantity;

        // Save the updated flower in the database (this method should be in your service/repository layer)
        _flowerService.UpdateFlower(flower);

        // Return a success response
        return Ok(new { message = "Flower updated successfully" });
    }





}
