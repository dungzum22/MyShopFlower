using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyShop.DataContext;
using MyShop.DTO;
using MyShop.DTOs;
using MyShop.Entities;
using MyShop.Services.Reports;

namespace MyShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly FlowershopContext _context;
        private readonly IReportService _reportService;

        public ReportController(FlowershopContext context, IReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        // API POST: Create a new report
        [HttpPost("CreateReport")]
        public async Task<IActionResult> CreateReport([FromForm] CreateReportDto reportDto)
        {
            if (string.IsNullOrWhiteSpace(reportDto.ReportReason))
            {
                return BadRequest(new { message = "Report reason is required." });
            }

            var newReport = new Report
            {
                UserId = reportDto.UserId,
                FlowerId = reportDto.FlowerId,
                SellerId = reportDto.SellerId,
                ReportReason = reportDto.ReportReason,
                ReportDescription = reportDto.ReportDescription,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow // Use UTC for consistency
            };

            var createdReport = await _reportService.CreateReportAsync(newReport);

            // Adjusted to use reportId instead of id
            return CreatedAtAction(nameof(GetReportById), new { reportId = createdReport.ReportId }, new
            {
                ReportId = createdReport.ReportId,
                UserId = createdReport.UserId,
                FlowerId = createdReport.FlowerId,
                SellerId = createdReport.SellerId,
                ReportReason = createdReport.ReportReason,
                ReportDescription = createdReport.ReportDescription,
                Status = createdReport.Status,
                CreatedAt = createdReport.CreatedAt,
                message = "Report created successfully"
            });
        }

        // API GET: Get a report by ID
        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetReportById(int reportId)
        {
            var report = await _reportService.GetReportByIdAsync(reportId);
            if (report == null)
            {
                return NotFound(new { message = "Report not found." });
            }
            return Ok(report);
        }

        // API GET: Get reports by User ID
        [HttpGet("GetByUserId/{userId}")]
        public async Task<IActionResult> GetReportsByUserId(int userId)
        {
            var reports = await _reportService.GetReportsByUserIdAsync(userId);
            if (reports == null || !reports.Any())
            {
                return NotFound(new { message = "No reports found for this user." });
            }
            return Ok(reports);
        }

        // API GET: Get all reports
        [HttpGet("GetAllReports")]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportService.GetAllReportsAsync();
            return Ok(reports);
        }

        // API PUT: Update the status of a report
        [HttpPut("UpdateReportStatus/{reportId}")]
        public async Task<IActionResult> UpdateReportStatus(int reportId, [FromForm] UpdateReportStatusDto updateReportStatusDto)
        {
            // Get the report by ID
            var report = await _reportService.GetReportByIdAsync(reportId);
            if (report == null)
            {
                return NotFound(new { message = "Report not found." });
            }

            // Validate the status
            if (string.IsNullOrWhiteSpace(updateReportStatusDto.Status))
            {
                return BadRequest(new { message = "Status is required." });
            }

            // Check if the status is one of the allowed values
            if (updateReportStatusDto.Status != "Pending" &&
                updateReportStatusDto.Status != "Resolved" &&
                updateReportStatusDto.Status != "Dismissed")
            {
                return BadRequest(new { message = "Invalid status value." });
            }

            // Update the report status
            report.Status = updateReportStatusDto.Status;
            report.UpdatedAt = DateTime.UtcNow; // Use UTC for consistency

            // Save the updated report
            await _reportService.UpdateReportStatusAsync(report);

            // Return success response
            return Ok(new
            {
                message = "Report status updated successfully.",
                ReportId = report.ReportId,
                Status = report.Status,
                UpdatedAt = report.UpdatedAt
            });
        }

    }
}

