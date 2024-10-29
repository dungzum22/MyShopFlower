namespace MyShop.DTO
{
    public class UserInfoDto
    {
        public int UserInfoId { get; set; }
        public string Name { get; set; }
        public List<AddressDto> Addresses { get; set; }
    }

}
