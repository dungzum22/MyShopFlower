using System.ComponentModel.DataAnnotations;

public class UpdateReportStatusDto
{
    [Required]
    [RegularExpression(@"^(Pending|Resolved|Dismissed)$", ErrorMessage = "Status must be either 'Pending', 'Resolved', or 'Dismissed'.")]
    public string Status { get; set; }
}
