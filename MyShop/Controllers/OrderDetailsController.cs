using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.Entities;
using System.Threading.Tasks.Dataflow;

namespace MyShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class OrderDetailsController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly FlowershopContext _context;
        public OrderDetailsController(ILogger<OrderController> logger, FlowershopContext context)
        {
            _logger = logger;
            _context = context;
        }


        [HttpGet("getOrderDetailList")]
        public async Task<IActionResult> GetOrderDetailListBySeller()
        {
            // Lấy userId từ token JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("UserId không hợp lệ.");
            }

            // Fetch sellerId for the given userId
            var seller = _context.Sellers.FirstOrDefault(s => s.UserId == userId);
            var sellerId = seller?.SellerId;

            var orderDetailList = (from od in _context.OrdersDetails
                                   join o in _context.Orders on od.OrderId equals o.OrderId
                                   join f in _context.FlowerInfos on od.FlowerId equals f.FlowerId
                                   join u in _context.Users on o.UserId equals u.UserId
                                   join ui in _context.UserInfos on u.UserId equals ui.UserId
                                   join a in _context.Addresses on ui.UserInfoId equals a.UserInfoId
                                   where od.SellerId == sellerId
                                   group new { od, u, ui, a, f } by od.OrderDetailId into grouped
                                   select new OrderDetailDto
                                   {
                                       OrderDetailId = grouped.Key,
                                       OrderId = grouped.First().od.OrderId,
                                       SellerId = sellerId,
                                       CustomerId = grouped.First().u.UserId,
                                       CustomerName = grouped.First().ui.FullName,
                                       AddressId = grouped.First().a.AddressId,
                                       AddressDescription = grouped.First().a.Description,
                                       FlowerId = grouped.First().od.FlowerId,
                                       FlowerName = grouped.First().od.Flower.FlowerName,
                                       FlowerImage = grouped.First().od.Flower.ImageUrl,
                                       Price = grouped.First().od.Price,
                                       Amount = grouped.First().od.Amount,
                                       Status = grouped.First().od.Status,
                                       CreatedAt = grouped.First().od.CreatedAt ?? DateTime.MinValue,
                                       DeliveryMethod = grouped.First().od.DeliveryMethod,
                                   })
                        .Distinct()
                        .ToList();

            // Check if the list is empty
            if (orderDetailList == null || !orderDetailList.Any())
            {
                return NotFound(new { message = "No flowers found for this seller." });
            }

            // Return the list of order details
            return Ok(orderDetailList);
        }

        [HttpGet("getOrderDetailListByCustomerId")]
        public async Task<IActionResult> GetOrderDetailListByCustomer([FromQuery] string? status)
        {
            // Lấy userId từ token JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không xác định được người dùng.");
            }
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("UserId không hợp lệ.");
            }

            // Fetch sellerId for the given userId
            var customer = _context.Users.FirstOrDefault(s => s.UserId == userId);
            var customerId = customer?.UserId;

            var orderDetailList = (from od in _context.OrdersDetails
                                   join o in _context.Orders on od.OrderId equals o.OrderId
                                   join f in _context.FlowerInfos on od.FlowerId equals f.FlowerId
                                   join u in _context.Users on o.UserId equals u.UserId
                                   join ui in _context.UserInfos on u.UserId equals ui.UserId
                                   join a in _context.Addresses on ui.UserInfoId equals a.UserInfoId
                                   where o.UserId == customerId && (status == null || od.Status == status)
                                   group new { od, u, ui, a, f } by od.OrderDetailId into grouped
                                   select new OrderDetailDto
                                   {
                                       OrderDetailId = grouped.Key,
                                       OrderId = grouped.First().od.OrderId,
                                       SellerId = grouped.First().od.SellerId,
                                       ShopName = grouped.First().od.Seller.ShopName,
                                       CustomerId = grouped.First().u.UserId,
                                       CustomerName = grouped.First().ui.FullName,
                                       AddressId = grouped.First().a.AddressId,
                                       AddressDescription = grouped.First().a.Description,
                                       FlowerId = grouped.First().od.FlowerId,
                                       FlowerName = grouped.First().od.Flower.FlowerName,
                                       FlowerImage = grouped.First().od.Flower.ImageUrl,
                                       Price = grouped.First().od.Price,
                                       Amount = grouped.First().od.Amount,
                                       Status = grouped.First().od.Status,
                                       CreatedAt = grouped.First().od.CreatedAt ?? DateTime.MinValue,
                                       DeliveryMethod = grouped.First().od.DeliveryMethod,
                                   })
                        .Distinct()
                        .ToList();

            // Check if the list is empty
            if (orderDetailList == null || !orderDetailList.Any())
            {
                return NotFound(new { message = "No flowers found for this seller." });
            }

            // Return the list of order details
            return Ok(orderDetailList);
        }



        [HttpPost("changeOrderDetailStatus")]
        public async Task<IActionResult> ChangeOrderDetailStatus([FromBody] UpdateOrderDetailDto updateModel)
        {
            // Tìm order detail cần cập nhật
            var orderDetail = await _context.OrdersDetails.FirstOrDefaultAsync(od => od.OrderDetailId == updateModel.OrderDetailId);

            if (orderDetail == null)
            {
                return NotFound("Order detail không tồn tại.");
            }

            // Cập nhật trạng thái
            orderDetail.Status = updateModel.Status;
            _context.Entry(orderDetail).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Lấy danh sách tất cả các đơn hàng có cùng trạng thái
            var sameStatusOrderDetails = (from od in _context.OrdersDetails
                                          join o in _context.Orders on od.OrderId equals o.OrderId
                                          join f in _context.FlowerInfos on od.FlowerId equals f.FlowerId
                                          join u in _context.Users on o.UserId equals u.UserId
                                          join ui in _context.UserInfos on u.UserId equals ui.UserId
                                          join a in _context.Addresses on ui.UserInfoId equals a.UserInfoId
                                          where od.Status == updateModel.Status
                                          group new { od, u, ui, a, f } by od.OrderDetailId into grouped
                                          select new OrderDetailDto
                                          {
                                              OrderDetailId = grouped.Key,
                                              OrderId = grouped.First().od.OrderId,
                                              SellerId = grouped.First().od.SellerId,
                                              ShopName = grouped.First().od.Seller.ShopName,
                                              CustomerId = grouped.First().u.UserId,
                                              CustomerName = grouped.First().ui.FullName,
                                              AddressId = grouped.First().a.AddressId,
                                              AddressDescription = grouped.First().a.Description,
                                              FlowerId = grouped.First().od.FlowerId,
                                              FlowerName = grouped.First().od.Flower.FlowerName,
                                              FlowerImage = grouped.First().od.Flower.ImageUrl,
                                              Price = grouped.First().od.Price,
                                              Amount = grouped.First().od.Amount,
                                              Status = grouped.First().od.Status,
                                              CreatedAt = grouped.First().od.CreatedAt ?? DateTime.MinValue,
                                              DeliveryMethod = grouped.First().od.DeliveryMethod,
                                          })
                                 .Distinct()
                                 .ToList();

            
            return Ok(sameStatusOrderDetails);
        }

    }
}
