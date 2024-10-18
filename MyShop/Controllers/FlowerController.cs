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
//[Authorize]
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




    // POST api/flowerinfo/Create
    [HttpPost("Create")]
    public async Task<IActionResult> CreateFlower([FromForm] FlowerDto flowerDto)
    {
        // Validate the input
        if (string.IsNullOrWhiteSpace(flowerDto.FlowerName) || flowerDto.Price <= 0 || flowerDto.AvailableQuantity < 0)
        {
            return BadRequest("Invalid input data.");
        }

        // Create a new flower object
        var newFlower = new FlowerInfo
        {
            FlowerName = flowerDto.FlowerName,
            FlowerDescription = flowerDto.FlowerDescription,
            Price = flowerDto.Price,
            CreatedAt = DateTime.UtcNow,
            CategoryId = flowerDto.CategoryId,
            AvailableQuantity = flowerDto.AvailableQuantity,
            SellerId = flowerDto.SellerId // Associate flower with the seller
        };

        // If an image is uploaded, handle the upload to S3
        if (flowerDto.Image != null && flowerDto.Image.Length > 0)
        {
            try
            {
                // Generate a unique file name for the image
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";

                using (var stream = flowerDto.Image.OpenReadStream())
                {
                    // Upload the file to S3 and get the image URL
                    var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
                    newFlower.ImageUrl = imageUrl; // Save the image URL in the flower object
                }
            }
            catch (Exception ex)
            {
                // Log the error and return a response
                _logger.LogError(ex, "Error uploading image to S3.");
                return StatusCode(500, "Error uploading image.");
            }
        }

        // Save the new flower to the database
        var createdFlower = await _flowerService.CreateFlower(newFlower); // Ensure CreateFlower returns Task<FlowerInfo>

        // Return the created flower information
        return Ok(new
        {
            FlowerID = createdFlower.FlowerId,
            FlowerName = createdFlower.FlowerName,
            FlowerDescription = createdFlower.FlowerDescription,
            Price = createdFlower.Price,
            AvailableQuantity = createdFlower.AvailableQuantity,
            CategoryID = createdFlower.CategoryId,
            ImageUrl = createdFlower.ImageUrl, // Include the image URL in the response
            SellerID = createdFlower.SellerId, // Include the SellerId in the response
            message = "Flower created successfully"
        });
    }

    //// POST api/flowerinfo/Create
    //[HttpPost("Create")]
    //[Authorize(Roles = "seller")] // Only sellers can create flowers
    //public async Task<IActionResult> CreateFlower([FromForm] FlowerDto flowerDto)
    //{
    //    // Validate the input
    //    if (string.IsNullOrWhiteSpace(flowerDto.FlowerName) || flowerDto.Price <= 0 || flowerDto.AvailableQuantity < 0)
    //    {
    //        return BadRequest("Invalid input data.");
    //    }

    //    // Get the seller's ID from the JWT claims
    //    var sellerIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
    //    if (sellerIdClaim == null || !int.TryParse(sellerIdClaim.Value, out var sellerId))
    //    {
    //        return Unauthorized("You must be logged in as a seller to create flowers.");
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
    //        SellerId = sellerId // Set the seller's ID from the JWT
    //    };

    //    // Handle image upload if provided
    //    if (flowerDto.Image != null && flowerDto.Image.Length > 0)
    //    {
    //        try
    //        {
    //            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";
    //            using (var stream = flowerDto.Image.OpenReadStream())
    //            {
    //                var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
    //                newFlower.ImageUrl = imageUrl; // Save the image URL in the flower object
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error uploading image to S3.");
    //            return StatusCode(500, "Error uploading image.");
    //        }
    //    }

    //    // Save the new flower to the database
    //    var createdFlower = await _flowerService.CreateFlower(newFlower);

    //    return Ok(new
    //    {
    //        FlowerID = createdFlower.FlowerId,
    //        FlowerName = createdFlower.FlowerName,
    //        FlowerDescription = createdFlower.FlowerDescription,
    //        Price = createdFlower.Price,
    //        AvailableQuantity = createdFlower.AvailableQuantity,
    //        CategoryID = createdFlower.CategoryId,
    //        ImageUrl = createdFlower.ImageUrl,
    //        SellerID = createdFlower.SellerId,
    //        message = "Flower created successfully"
    //    });
    //}



    [HttpGet("{id}")]
    public async Task<IActionResult> GetFlower(int id)
    {
        var flower = await _context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == id);

        if (flower == null)
        {
            return NotFound("Thông tin hoa không tồn tại.");
        }

        return Ok(new
        {
            flower.FlowerId,
            flower.FlowerName,
            flower.FlowerDescription,
            flower.Price,
            flower.AvailableQuantity,
            flower.ImageUrl,
            flower.SellerId
            //flower.CreatedDate,
            //flower.UpdatedDate
        });
    }





////GetALL hoa của 1 sellerID
//[HttpGet("seller/{sellerId}")]
//public async Task<IActionResult> GetFlowersBySellerId(int sellerId)
//{
//    // Fetch all flowers associated with the specified seller ID
//    var flowers = await _context.FlowerInfos
//        .Where(f => f.SellerId == sellerId)
//        .ToListAsync();

//    // Check if any flowers were found for the seller
//    if (!flowers.Any())
//    {
//        return NotFound($"No flowers found for Seller ID: {sellerId}.");
//    }

//    // Return the list of flowers for the seller
//    return Ok(flowers.Select(f => new
//    {
//        f.FlowerId,
//        f.FlowerName,
//        f.FlowerDescription,
//        f.Price,
//        f.AvailableQuantity,
//        f.ImageUrl,
//        f.CategoryId // Include CategoryId if needed
//    }));
//}
    [HttpGet("seller/{sellerId}")]
    //[Authorize(Roles = "admin")] // Only admins can access this route
    public async Task<IActionResult> GetFlowersBySellerId(int sellerId)
    {
        //// Check if the user is an admin
        //var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        //if (userRole != "admin")
        //{
        //    return Forbid("Bạn không có quyền truy cập thông tin này."); // Forbidden response for non-admins
        //}

        // Fetch all flowers associated with the specified seller ID
        var flowers = await _context.FlowerInfos
            .Where(f => f.SellerId == sellerId)
            .ToListAsync();

        // Check if any flowers were found for the seller
        if (!flowers.Any())
        {
            return NotFound($"Không có hoa nào thuộc người bán có ID: {sellerId}.");
        }

        // Return the list of flowers for the seller
        return Ok(flowers.Select(f => new
        {
            f.FlowerId,
            f.FlowerName,
            f.FlowerDescription,
            f.Price,
            f.AvailableQuantity,
            f.ImageUrl,
            f.CategoryId
        }));
    }






    //[HttpPut("update{id}")]
    //[Authorize(Roles = "seller")] // Only sellers can create flowers
    //public async Task<IActionResult> UpdateFlower(int id, [FromForm] FlowerDto flowerDto)
    //{
    //    // Tìm FlowerInfo dựa trên flower_id
    //    var flower = await _context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == id);

    //    if (flower == null)
    //    {
    //        return NotFound("Thông tin hoa không tồn tại.");
    //    }

    //    // Cập nhật các thông tin từ DTO vào model FlowerInfo
    //    flower.FlowerName = flowerDto.FlowerName;
    //    flower.FlowerDescription = flowerDto.FlowerDescription;
    //    flower.Price = (Decimal)flowerDto.Price;
    //    flower.AvailableQuantity = (int)flowerDto.AvailableQuantity;

    //    // Xử lý imageUrl nếu có file tải lên
    //    if (flowerDto.Image != null && flowerDto.Image.Length > 0)
    //    {
    //        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";
    //        var filePath = $"flower-img/{fileName}";
    //        try
    //        {
    //            using (var stream = flowerDto.Image.OpenReadStream())
    //            {
    //                // Upload file lên S3 và lấy URL 
    //                var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
    //                // Cập nhật đường dẫn ảnh vào cơ sở dữ liệu
    //                flower.ImageUrl = imageUrl;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Có lỗi xảy ra khi tải ảnh lên S3.");
    //            return StatusCode(500, $"{ex}Có lỗi xảy ra khi tải ảnh lên.");
    //        }
    //    }

    //    // Cập nhật ngày chỉnh sửa
    //    flower.CreatedAt = DateTime.UtcNow;

    //    // Đánh dấu entity là đã thay đổi và lưu thay đổi vào cơ sở dữ liệu
    //    _context.Entry(flower).State = EntityState.Modified;

    //    try
    //    {
    //        await _context.SaveChangesAsync();
    //    }
    //    catch (DbUpdateConcurrencyException)
    //    {
    //        if (!_context.FlowerInfos.Any(f => f.FlowerId == flower.FlowerId))
    //        {
    //            return NotFound("Không tìm thấy hoa này.");
    //        }
    //        else
    //        {
    //            throw;
    //        }
    //    }

    //    return Ok("Thông tin hoa đã được cập nhật thành công.");
    //}

    [HttpPut("update{id}")]
    [Authorize(Roles = "seller")] // Only sellers can update flowers
    public async Task<IActionResult> UpdateFlower(int id, [FromForm] FlowerDto flowerDto)
    {
        // Get the seller's ID from the JWT claims
        var sellerIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (sellerIdClaim == null || !int.TryParse(sellerIdClaim.Value, out var sellerId))
        {
            return Unauthorized("You must be logged in as a seller to update flowers.");
        }

        // Find the flower by ID
        var flower = await _context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == id);

        if (flower == null)
        {
            return NotFound("Flower not found.");
        }

        // Check if the current seller owns the flower
        if (flower.SellerId != sellerId)
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

    [HttpGet("GetAllFlowers")]
    [AllowAnonymous] // Allow everyone to access this, even without authorization
    public async Task<IActionResult> GetAllFlowers()
    {
        var flowers = await _context.FlowerInfos
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
