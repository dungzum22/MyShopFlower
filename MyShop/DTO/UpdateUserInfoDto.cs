namespace MyShop.DTO
{
    public class UpdateUserInfoDto
    {
        public string FullName { get; set; }
        public string Address { get; set; }
        public string Sex { get; set; }
        public DateOnly BirthDate { get; set; }
        public IFormFile? Avatar { get; set; }  
    }

}
