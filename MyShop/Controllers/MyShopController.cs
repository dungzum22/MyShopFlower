//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authentication.Google;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using MyShop.Data;
//using MyShop.Models;
//using System;
//using System.Security.Claims;

//namespace MyShop.Controllers
//{
//    public class MyShopController : Controller
//    {
//        private readonly DataContext _context;
//        public MyShopController(DataContext context)
//        {
//            _context = context;
//        }

//        [HttpGet("google-login")]
//        public IActionResult GoogleLogin()
//        {
//            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
//            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
//        }


//        [HttpGet("google-response")]
//        public async Task<IActionResult> GoogleResponse()
//        {
//            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

//            if (!authenticateResult.Succeeded)
//                return BadRequest("Error authenticating with Google");

//            var claims = authenticateResult.Principal.Identities.FirstOrDefault().Claims;
//            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
//            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

//            if (email == null || name == null)
//                return BadRequest("Error retrieving email or name from Google");

//            // Kiểm tra xem người dùng đã tồn tại trong DB chưa
//            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
//            if (existingUser == null)
//            {
//                // Nếu người dùng chưa tồn tại, tạo tài khoản mới
//                var newUser = new User
//                {
//                    Username = name,
//                    Email = email,
//                    Type = "Google",
//                    CreatedDate = DateTime.Now,
//                    Status = "Active"
//                };

//                _context.Users.Add(newUser);
//                await _context.SaveChangesAsync();

//                return Ok(new { message = "User registered successfully via Google", user = newUser });
//            }

//            return Ok(new { message = "User logged in successfully", user = existingUser });
//        }
//    }
//}
