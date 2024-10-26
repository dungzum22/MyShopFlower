namespace MyShop.DTOs
{
    public class ReportDto
    {
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public int FlowerId { get; set; }
        public int SellerId { get; set; }
        public string ReportReason { get; set; } = string.Empty;
        public string? ReportDescription { get; set; }
        public string Status { get; set; } = "Pending";
    }
}