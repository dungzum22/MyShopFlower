using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.DTO;
using Org.BouncyCastle.Utilities;
using System.IO;
using System.Linq;
using MyShop.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.S3;
using MyShop.Services;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // Chỉ cho phép người dùng đã xác thực qua JWT
    public class UserInfoController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly ILogger<UserInfoController> _logger;
        private readonly S3StorageService _s3StorageService;

        public UserInfoController(FlowershopContext context, ILogger<UserInfoController> logger, S3StorageService s3StorageService)
        {
            _context = context;
            _logger = logger;
            _s3StorageService = s3StorageService;
        }

        // API GET UserInfo: Lấy thông tin người dùng từ JWT
        [HttpGet("info")]
        public async Task<IActionResult> GetUserInfo()
        {
            // (rest of the code)
        }

        // API PUT Update UserInfo: Cập nhật thông tin người dùng và upload hình ảnh
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUserInfo([FromForm] UpdateUserInfoDto userInfoDto)
        {
            // (rest of the code)
        }

        // API POST Create UserInfo: Tạo mới thông tin người dùng
        [HttpPost("create")]
        public async Task<IActionResult> CreateUserInfo([FromForm] CreateUserInfoDto createUserInfoDto)
        {
            // (rest of the code)
        }
    }
}
