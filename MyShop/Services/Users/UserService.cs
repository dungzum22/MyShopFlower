using MyShop.Entities;
using MyShop.DataContext;
using System.Linq;

namespace MyShop.Services.Users
{
    public class UserService : IUserService
    {
        private readonly FlowershopContext _context;

        public UserService(FlowershopContext context)
        {
            _context = context;
        }

        public User Authenticate(string username, string password)
        {
            // Tìm user theo username
            var user = _context.Users.FirstOrDefault(x => x.Username == username);

            // Nếu không tìm thấy user, trả về null
            if (user == null)
            {
                return null;
            }

            // Nếu user là admin, so sánh mật khẩu không mã hóa
            if (user.Type == "admin" && user.Password == password)
            {
                return user; // Đăng nhập thành công cho admin với mật khẩu không mã hóa
            }

            // Nếu không phải admin, kiểm tra bcrypt cho mật khẩu đã mã hóa
            if (user.Type != "admin" && !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return null; // Mật khẩu không khớp
            }

            return user;  // Trả về user nếu thông tin hợp lệ
        }

        public User Register(User user)
        {
            // Kiểm tra xem username đã tồn tại chưa
            if (CheckUsernameExists(user.Username))
            {
                return null;
            }

            // Kiểm tra xem email đã tồn tại chưa
            if (CheckEmailExists(user.Email))
            {
                return null;
            }

            // Mã hóa mật khẩu trước khi lưu
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            // Thêm user mới vào database
            _context.Users.Add(user);
            _context.SaveChanges();

            return user;  // Trả về thông tin người dùng sau khi đăng ký thành công
        }

        public bool CheckUsernameExists(string username)
        {
            return _context.Users.Any(x => x.Username == username);
        }

        public bool CheckEmailExists(string email)
        {
            return _context.Users.Any(x => x.Email == email);
        }

        public void Logout()
        {
            // Để trống hoặc có thể thêm logic nếu sử dụng session hoặc cookies
        }

        // Phương thức lấy toàn bộ người dùng mà không bao gồm mật khẩu
        public List<User> GetAllUsers()
        {
            return _context.Users
                .Select(u => new User
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Type = u.Type,
                    CreatedDate = u.CreatedDate,
                    Status = u.Status
                    // Không bao gồm Password
                }).ToList();
        }

        // Phương thức lấy thông tin chi tiết của một người dùng
        public User GetUserById(int userId)
        {
            return _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new User
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Type = u.Type,
                    CreatedDate = u.CreatedDate,
                    Status = u.Status
                }).FirstOrDefault();
        }

        // Phương thức cập nhật thông tin người dùng (ví dụ như trạng thái)
        public void UpdateUser(User user)
        {
            var existingUser = _context.Users.FirstOrDefault(u => u.UserId == user.UserId);
            if (existingUser != null)
            {
                existingUser.Status = user.Status;  // Cập nhật trạng thái hoặc các trường khác
                _context.SaveChanges();
            }
        }
    }
}
