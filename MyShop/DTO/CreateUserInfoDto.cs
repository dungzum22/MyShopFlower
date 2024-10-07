namespace MyShop.DTO
{
    public class CreateUserInfoDto
    {
        public string FullName { get; set; }
        public string Address { get; set; }
        public DateOnly BirthDate { get; set; }
        public string Sex { get; set; }
        public IFormFile Avatar { get; set; } // Để tải file ảnh đại diện
    }
}
