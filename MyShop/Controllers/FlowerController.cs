using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using MyShop.Services.Flowers;

[ApiController]
[Route("api/[controller]")]
public class FlowerInfoController : ControllerBase
{
    private readonly FlowershopContext _context;
    private readonly IFlowerService _flowerService;
    private readonly S3StorageService _s3StorageService;
    private readonly ILogger<FlowerInfoController> _logger;

    // Constructor injection for both context and flower service
    public FlowerInfoController(FlowershopContext context, IFlowerService flowerService)
    {
        _context = context;
        _flowerService = flowerService;
    }

    // POST api/flowerinfo/Create
    [HttpPost("Create")]
    public IActionResult CreateFlower([FromForm] string flowername, [FromForm] string flowerdiscrpt, [FromForm] decimal price, [FromForm] int quantity, [FromForm] int category)
    {
        // Create a new flower
        var newFlower = new FlowerInfo
        {
            FlowerName = flowername,
            FlowerDescription = flowerdiscrpt,
            Price = price,
            CreatedAt = DateTime.UtcNow,
            CategoryId = category,
            AvailableQuantity = quantity,
        };

        // Save the new flower to the database
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

    // GET api/flowerinfo/Search/{id}
    //[HttpGet("Search/{id}")]
    //public IActionResult GetFlower(int id)
    //{
    //    // Fetch the flower by ID
    //    var flower = _flowerService.GetFlowerById(id);

    //    if (flower == null)
    //    {
    //        return NotFound(new { message = "Flower not found" });
    //    }

    //    // Return flower information
    //    return Ok(new
    //    {
    //        FlowerID = flower.FlowerId,
    //        FlowerName = flower.FlowerName,
    //        FlowerDescription = flower.FlowerDescription,
    //        Price = flower.Price,
    //        AvailableQuantity = flower.AvailableQuantity,
    //        CategoryID = flower.CategoryId
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
            //flower.CreatedDate,
            //flower.UpdatedDate
        });
    }


    // POST api/flowerinfo/Update/{id}
    //[HttpPost("Update/{id}")]
    //public IActionResult UpdateFlower(int id, [FromForm] string? flowername, [FromForm] string? flowerdiscrpt, [FromForm] decimal? price, [FromForm] int? quantity)
    //{
    //    // Fetch the flower by ID
    //    var flower = _flowerService.GetFlowerById(id);

    //    if (flower == null)
    //    {
    //        return NotFound(new { message = "Flower not found" });
    //    }

    //    // Update the flower properties with new values if provided, otherwise keep the old values
    //    flower.FlowerName = !string.IsNullOrEmpty(flowername) ? flowername : flower.FlowerName;
    //    flower.FlowerDescription = !string.IsNullOrEmpty(flowerdiscrpt) ? flowerdiscrpt : flower.FlowerDescription;
    //    flower.Price = price.HasValue ? price.Value : flower.Price;
    //    flower.AvailableQuantity = quantity.HasValue ? quantity.Value : flower.AvailableQuantity;

    //    // Save the updated flower in the database
    //    _flowerService.UpdateFlower(flower);

    //    // Return a success response
    //    return Ok(new { message = "Flower updated successfully" });
    //}

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFlower(int id, [FromForm] UpdateFlowerDto flowerDto)
    {
        // Tìm FlowerInfo dựa trên flower_id
        var flower = await _context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == id);

        if (flower == null)
        {
            return NotFound("Thông tin hoa không tồn tại.");
        }

        // Cập nhật các thông tin từ DTO vào model FlowerInfo
        flower.FlowerName = flowerDto.FlowerName;
        flower.FlowerDescription = flowerDto.FlowerDescription;
        flower.Price = (Decimal)flowerDto.Price;
        flower.AvailableQuantity = (int)flowerDto.AvailableQuantity;

        // Xử lý imageUrl nếu có file tải lên
        if (flowerDto.Image != null && flowerDto.Image.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(flowerDto.Image.FileName)}";

            try
            {
                using (var stream = flowerDto.Image.OpenReadStream())
                {
                    // Upload file lên S3 và lấy URL (có thể sử dụng dịch vụ lưu trữ khác)
                    var imageUrl = await _s3StorageService.UploadFileAsync(stream, fileName);
                    // Cập nhật đường dẫn ảnh vào cơ sở dữ liệu
                    flower.ImageUrl = imageUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Có lỗi xảy ra khi tải ảnh lên S3.");
                return StatusCode(500, "Có lỗi xảy ra khi tải ảnh lên.");
            }
        }

        // Cập nhật ngày chỉnh sửa
        flower.CreatedAt = DateTime.UtcNow;

        // Đánh dấu entity là đã thay đổi và lưu thay đổi vào cơ sở dữ liệu
        _context.Entry(flower).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.FlowerInfos.Any(f => f.FlowerId == flower.FlowerId))
            {
                return NotFound("Không tìm thấy hoa này.");
            }
            else
            {
                throw;
            }
        }

        return Ok("Thông tin hoa đã được cập nhật thành công.");
    }

}
