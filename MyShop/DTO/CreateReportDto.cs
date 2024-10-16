using System.ComponentModel.DataAnnotations;

namespace MyShop.DTOs
{
    public class CreateReportDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int FlowerId { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Required(ErrorMessage = "Report reason is required.")]
        [StringLength(500, ErrorMessage = "Report reason cannot be longer than 500 characters.")]
        public string ReportReason { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Report description cannot be longer than 1000 characters.")]
        public string? ReportDescription { get; set; }
    }
}
