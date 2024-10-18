namespace MyShop.DTO
{
    public class OrderRequestDto
    {
        public string PhoneNumber { get; set; }
        public int AddressId { get; set; }
        public List<int> VoucherIds { get; set; } // Danh sách ID của voucher từ các shop
    }
}
