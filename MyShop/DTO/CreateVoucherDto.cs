namespace MyShop.DTO
{
    public class CreateVoucherDto
    {
        public string VoucherCode { get; set; }
        public float Discount { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UsageLimit { get; set; }
    }

}