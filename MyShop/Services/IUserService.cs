using MyShop.Entities;

namespace MyShop.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Xác thực người dùng bằng username và password
        /// </summary>
        /// <param name="username">Tên đăng nhập của người dùng</param>
        /// <param name="password">Mật khẩu của người dùng</param>
        /// <returns>Trả về thông tin người dùng nếu hợp lệ, nếu không trả về null</returns>
        User Authenticate(string username, string password);

        /// <summary>
        /// Đăng ký người dùng mới
        /// </summary>
        /// <param name="user">Thông tin người dùng</param>
        /// <returns>Trả về thông tin người dùng sau khi đăng ký, nếu không thành công trả về null</returns>
        User Register(User user);

        bool CheckUsernameExists(string username);
        bool CheckEmailExists(string email);
        void Logout();
    }
}
