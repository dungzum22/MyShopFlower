using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using MyShop.Services.Flowers;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class FlowerInfoController : ControllerBase
{
    private readonly FlowershopContext _context;
    private readonly IFlowerService _flowerService;
    private readonly S3StorageService _s3StorageService;
    private readonly ILogger<FlowerInfoController> _logger;

    public FlowerInfoController(
        FlowershopContext context,
        IFlowerService flowerService,
        S3StorageService s3StorageService, 
        ILogger<FlowerInfoController> logger)
    {
        _context = context;
        _flowerService = flowerService;
        _s3StorageService = s3StorageService;
        _logger = logger;
    }

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
            Status = "available",
            message = "Flower created successfully"
        });
    }

    //    [HttpPut("Update/{id}")]
    //[Authorize(Roles = "seller")] // Only sellers can update flowers
    //public async Task<IActionResult> UpdateFlower(int id, [FromBody] FlowerDto flowerDto)
    //{
    //    // Lấy userId từ token JWT
    //    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
    //    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
    //    {
    //        return Unauthorized("You must be logged in as a seller to update flowers.");
    //    }

    //    // Kiểm tra xem user hiện tại có phải là seller không
    //    var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
    //    if (seller == null)
    //    {
    //        return Unauthorized("You must be a seller to update flowers.");
    //    }

    //    // Find the flower by ID
    //    var flower = await _context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == id);
    //    if (flower == null)
    //    {
    //        return NotFound("Flower not found.");
    //    }

    //    // Check if the current seller owns the flower
    //    if (flower.SellerId != seller.SellerId)
    //    {
    //        return Forbid("You do not have permission to update this flower.");
    //    }

    //    // Update the flower details
    //    flower.FlowerName = flowerDto.FlowerName;
    //    flower.FlowerDescription = flowerDto.FlowerDescription;
    //    flower.Price = (decimal)flowerDto.Price;
    //    flower.AvailableQuantity = (int)flowerDto.AvailableQuantity;

    //    // Handle image upload if a new image is provided
    //    if (flowerDto.Image != null && flowerDto.Image.Length > 0)
    //    {
    //        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";
    //        try
    //        {
    //            using (var stream = flowerDto.Image.OpenReadStream())
    //            {
    //                var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
    //                flower.ImageUrl = imageUrl; // Update the image URL
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error uploading image to S3.");
    //            return StatusCode(500, "Error uploading image.");
    //        }
    //    }

    //    // Mark the entity as modified
    //    _context.Entry(flower).State = EntityState.Modified;

    //    try
    //    {
    //        await _context.SaveChangesAsync();
    //    }
    //    catch (DbUpdateConcurrencyException)
    //    {
    //        if (!_context.FlowerInfos.Any(f => f.FlowerId == flower.FlowerId))
    //        {
    //            return NotFound("Flower not found.");
    //        }
    //        else
    //        {
    //            throw;
    //        }
    //    }

    //    // Map updated flower entity to FlowerDto to return
    //    var updatedFlowerDto = new FlowerDto
    //    {
    //        FlowerName = flower.FlowerName,
    //        FlowerDescription = flower.FlowerDescription,
    //        Price = flower.Price,
    //        AvailableQuantity = flower.AvailableQuantity,

    //    };

    //    return Ok(updatedFlowerDto);
    //}

    [HttpPut("Update/{id}")]
    [Authorize(Roles = "seller")] // Only sellers can update flowers
    public async Task<IActionResult> UpdateFlower(int id, [FromBody] FlowerDto flowerDto)
    {
        // Retrieve userId from JWT token
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("You must be logged in as a seller to update flowers.");
        }

        // Check if the current user is a seller
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
        flower.Price = flowerDto.Price; // assuming this is already a decimal
        flower.AvailableQuantity = flowerDto.AvailableQuantity;

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

        // Get all flowers by the current seller's ID
        var flowersBySeller = await _context.FlowerInfos
            .Where(f => f.SellerId == seller.SellerId)
            .Select(f => new
            {
                FlowerId = f.FlowerId,
                FlowerName = f.FlowerName,
                FlowerDescription = f.FlowerDescription,
                Price = f.Price,
                AvailableQuantity = f.AvailableQuantity,
                ImageUrl = f.ImageUrl,
                CategoryId = f.CategoryId,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            Message = "Flower updated successfully.",
            UpdatedFlower = new
            {
                FlowerId = flower.FlowerId,
                FlowerName = flower.FlowerName,
                FlowerDescription = flower.FlowerDescription,
                Price = flower.Price,
                AvailableQuantity = flower.AvailableQuantity,
                ImageUrl = flower.ImageUrl,
                CategoryId = flower.CategoryId,
            },
            SellerFlowers = flowersBySeller
        });
    }

    [HttpGet("GetAllFlowers")]
    [AllowAnonymous] // Cho phép mọi người có thể truy cập vào để xem danh sách hoa
    public async Task<IActionResult> GetAllFlowers()
    {
        try
        {
        // Lấy tất cả thông tin hoa từ cơ sở dữ liệu
        var flowers = await _context.FlowerInfos.ToListAsync();

        // Nếu không có hoa nào trong cơ sở dữ liệu
        if (flowers == null || !flowers.Any())
        {
            return NotFound(new { message = "No flowers found." });
        }

        // Trả về danh sách các hoa
        return Ok(flowers.Select(f => new
        {
            FlowerId = f.FlowerId,
            FlowerName = f.FlowerName,
            FlowerDescription = f.FlowerDescription,
            Price = f.Price,
            AvailableQuantity = f.AvailableQuantity,
            ImageUrl = f.ImageUrl,
            CategoryId = f.CategoryId,
            SellerId = f.SellerId,
            CreatedAt = f.CreatedAt
        }));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving flowers.");
        return StatusCode(500, "Internal server error while retrieving flowers.");
    }
}

    [HttpGet("{flowerId}")]
    public async Task<IActionResult> GetFlowerInfoById(int flowerId)
    {
        var flower = await _flowerService.GetFlowerById(flowerId);
        if(flower == null)
        {
            return NotFound("Flower not found.");
        }
        else
        {
            return Ok(flower);
        }
    }

    [HttpGet("flowers/{sellerId}")]
    public async Task<IActionResult> GetFlowersBySellerId(int sellerId)
    {
        var flowerList = await _flowerService.GetAllFlowersBySellerId(sellerId);
        if(flowerList == null)
        {
            return NotFound("Flower not found");
        }
        else
        {
            return Ok(flowerList);
        }
    }

    [HttpGet("GetFlowersByPrice")]
    public async Task<IActionResult> GetFlowerByPrice(decimal price1, decimal price2)
    {
        var flowers = await _context.FlowerInfos
            .Where(f => f.Price >= price1 && f.Price <= price2)
            .Select(f => new
            {
                FlowerId = f.FlowerId,
                FlowerName = f.FlowerName,
                FlowerDescription = f.FlowerDescription,
                Price = f.Price,
                AvailableQuantity = f.AvailableQuantity,
                CategoryId = f.CategoryId,
                ImageUrl = f.ImageUrl,
                SellerId = f.SellerId,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        if (!flowers.Any())
        {
            return NotFound(new { message = "No flowers found." });
        }

        return Ok(flowers);
    }
}
