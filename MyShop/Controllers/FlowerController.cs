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
        S3StorageService s3StorageService, 
        ILogger<FlowerInfoController> logger)
    {
        _context = context;
        _flowerService = flowerService;
        _s3StorageService = s3StorageService;
        _logger = logger;
    }

    [HttpPost("Create")]
    [Authorize(Roles = "seller")] // Chỉ seller có thể tạo hoa
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

        // Tạo một đối tượng flower mới và sử dụng SellerId từ seller được lấy từ DB
        var newFlower = new FlowerInfo
        {
            FlowerName = flowerDto.FlowerName,
            FlowerDescription = flowerDto.FlowerDescription,
            Price = flowerDto.Price,
            CreatedAt = DateTime.UtcNow,
            CategoryId = flowerDto.CategoryId,
            AvailableQuantity = flowerDto.AvailableQuantity,
            SellerId = seller.SellerId // Tự động lấy SellerId từ JWT token
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

        // Lưu bông hoa mới vào cơ sở dữ liệu
        var createdFlower = await _flowerService.CreateFlower(newFlower);

        // Cập nhật bảng seller - tăng total_product và quantity
        seller.TotalProduct += 1; // Tăng 1 cho mỗi bông hoa mới
        seller.Quantity += newFlower.AvailableQuantity; // Tăng số lượng hoa mới
        _context.Sellers.Update(seller);
        await _context.SaveChangesAsync();

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


    [HttpPut("Update/{id}")]
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

        // Tìm hoa dựa vào ID
        var flower = await _context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == id);
        if (flower == null)
        {
            return NotFound("Flower not found.");
        }

        // Kiểm tra xem seller hiện tại có phải là chủ sở hữu của bông hoa không
        if (flower.SellerId != seller.SellerId)
        {
            return Forbid("You do not have permission to update this flower.");
        }

        // Tính sự chênh lệch của số lượng hoa
        int quantityDifference = flowerDto.AvailableQuantity - flower.AvailableQuantity;

        // Cập nhật thông tin của bông hoa
        flower.FlowerName = flowerDto.FlowerName;
        flower.FlowerDescription = flowerDto.FlowerDescription;
        flower.Price = flowerDto.Price;
        flower.AvailableQuantity = flowerDto.AvailableQuantity;

        // Xử lý upload hình ảnh nếu có cung cấp hình ảnh mới
        if (flowerDto.Image != null && flowerDto.Image.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";
            try
            {
                using (var stream = flowerDto.Image.OpenReadStream())
                {
                    var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
                    flower.ImageUrl = imageUrl; // Cập nhật lại URL của hình ảnh
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to S3.");
                return StatusCode(500, "Error uploading image.");
            }
        }

        // Đánh dấu đối tượng là đã được thay đổi
        _context.Entry(flower).State = EntityState.Modified;

        try
        {
            // Cập nhật bảng seller - chỉ cập nhật quantity (không tăng total_product vì đây là cập nhật, không phải tạo mới)
            seller.Quantity += quantityDifference; // Thay đổi số lượng dựa trên sự chênh lệch
            _context.Sellers.Update(seller);
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


}
