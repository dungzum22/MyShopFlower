
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using MyShop.Services.Flowers;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FlowerInfoController : ControllerBase
{
    private readonly FlowershopContext _context;
    private readonly IFlowerService _flowerService;
    private readonly S3StorageService _s3StorageService;
    private readonly ILogger<FlowerInfoController> _logger;


    public FlowerInfoController(
    FlowershopContext context,
    IFlowerService flowerService,
    S3StorageService s3StorageService, // Add this line
    ILogger<FlowerInfoController> logger // Add logger to the constructor
)
    {
        _context = context;
        _flowerService = flowerService;
        _s3StorageService = s3StorageService; // Initialize the injected service
        _logger = logger; // Initialize logger
    }




    //// POST api/flowerinfo/Create
    //[HttpPost("Create")]
    //public async Task<IActionResult> CreateFlower([FromForm] FlowerDto flowerDto)
    //{
    //    // Validate the input
    //    if (string.IsNullOrWhiteSpace(flowerDto.FlowerName) || flowerDto.Price <= 0 || flowerDto.AvailableQuantity < 0)
    //    {
    //        return BadRequest("Invalid input data.");
    //    }

    //    // Create a new flower object
    //    var newFlower = new FlowerInfo
    //    {
    //        FlowerName = flowerDto.FlowerName,
    //        FlowerDescription = flowerDto.FlowerDescription,
    //        Price = flowerDto.Price,
    //        CreatedAt = DateTime.UtcNow,
    //        CategoryId = flowerDto.CategoryId,
    //        AvailableQuantity = flowerDto.AvailableQuantity,
    //        SellerId = flowerDto.SellerId // Associate flower with the seller
    //    };

    //    // If an image is uploaded, handle the upload to S3
    //    if (flowerDto.Image != null && flowerDto.Image.Length > 0)
    //    {
    //        try
    //        {
    //            // Generate a unique file name for the image
    //            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";

    //            using (var stream = flowerDto.Image.OpenReadStream())
    //            {
    //                // Upload the file to S3 and get the image URL
    //                var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
    //                newFlower.ImageUrl = imageUrl; // Save the image URL in the flower object
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            // Log the error and return a response
    //            _logger.LogError(ex, "Error uploading image to S3.");
    //            return StatusCode(500, "Error uploading image.");
    //        }
    //    }

    //    // Save the new flower to the database
    //    var createdFlower = await _flowerService.CreateFlower(newFlower); // Ensure CreateFlower returns Task<FlowerInfo>

    //    // Return the created flower information
    //    return Ok(new
    //    {
    //        FlowerID = createdFlower.FlowerId,
    //        FlowerName = createdFlower.FlowerName,
    //        FlowerDescription = createdFlower.FlowerDescription,
    //        Price = createdFlower.Price,
    //        AvailableQuantity = createdFlower.AvailableQuantity,
    //        CategoryID = createdFlower.CategoryId,
    //        ImageUrl = createdFlower.ImageUrl, // Include the image URL in the response
    //        SellerID = createdFlower.SellerId, // Include the SellerId in the response
    //        message = "Flower created successfully"
    //    });
    //}

    // POST api/flowerinfo/Create
    [HttpPost("Create")]
    [Authorize(Roles = "seller")] // Only sellers can create flowers
    public async Task<IActionResult> CreateFlower([FromForm] FlowerDto flowerDto)
    {
        // Validate the input
        if (string.IsNullOrWhiteSpace(flowerDto.FlowerName) || flowerDto.Price <= 0 || flowerDto.AvailableQuantity < 0)
        {
            return BadRequest("Invalid input data.");
        }

        // Lấy userId từ token JWT
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("You must be logged in to create flowers.");
        }

        // Kiểm tra xem user hiện tại có phải là seller không
        var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        if (seller == null)
        {
            return Unauthorized("You must be a seller to create flowers.");
        }

        // Tạo một đối tượng flower mới
        var newFlower = new FlowerInfo
        {
            FlowerName = flowerDto.FlowerName,
            FlowerDescription = flowerDto.FlowerDescription,
            Price = flowerDto.Price,
            CreatedAt = DateTime.UtcNow,
            CategoryId = flowerDto.CategoryId,
            AvailableQuantity = flowerDto.AvailableQuantity,
            SellerId = seller.SellerId // Lấy sellerId từ bảng Seller
        };

        // Handle image upload if provided
        if (flowerDto.Image != null && flowerDto.Image.Length > 0)
        {
            try
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";
                using (var stream = flowerDto.Image.OpenReadStream())
                {
                    var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
                    newFlower.ImageUrl = imageUrl; // Save the image URL in the flower object
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to S3.");
                return StatusCode(500, "Error uploading image.");
            }
        }

        // Save the new flower to the database
        var createdFlower = await _flowerService.CreateFlower(newFlower);

        return Ok(new
        {
            FlowerID = createdFlower.FlowerId,
            FlowerName = createdFlower.FlowerName,
            FlowerDescription = createdFlower.FlowerDescription,
            Price = createdFlower.Price,
            AvailableQuantity = createdFlower.AvailableQuantity,
            CategoryID = createdFlower.CategoryId,
            ImageUrl = createdFlower.ImageUrl,
            SellerID = createdFlower.SellerId,
            message = "Flower created successfully"
        });
    }

    [HttpPut("update{id}")]
    [Authorize(Roles = "seller")] // Only sellers can update flowers
    public async Task<IActionResult> UpdateFlower(int id, [FromForm] FlowerDto flowerDto)
    {
        // Lấy userId từ token JWT
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("You must be logged in as a seller to update flowers.");
        }

        // Kiểm tra xem user hiện tại có phải là seller không
        var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        if (seller == null)
        {
            return Unauthorized("You must be a seller to update flowers.");
        }

        // Find the flower by ID
        var flower = await _context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == id);

        if (flower == null)
        {
            return NotFound("Flower not found.");
        }

        // Check if the current seller owns the flower
        if (flower.SellerId != seller.SellerId)
        {
            return Forbid("You do not have permission to update this flower.");
        }

        // Update the flower details
        flower.FlowerName = flowerDto.FlowerName;
        flower.FlowerDescription = flowerDto.FlowerDescription;
        flower.Price = (decimal)flowerDto.Price;
        flower.AvailableQuantity = (int)flowerDto.AvailableQuantity;

        // Handle image upload if a new image is provided
        if (flowerDto.Image != null && flowerDto.Image.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";
            try
            {
                using (var stream = flowerDto.Image.OpenReadStream())
                {
                    var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
                    flower.ImageUrl = imageUrl; // Update the image URL
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to S3.");
                return StatusCode(500, "Error uploading image.");
            }
        }

        // Mark the entity as modified
        _context.Entry(flower).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.FlowerInfos.Any(f => f.FlowerId == flower.FlowerId))
            {
                return NotFound("Flower not found.");
            }
            else
            {
                throw;
            }
        }

        return Ok("Flower updated successfully.");
    }

}
