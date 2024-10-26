using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto reportDto)
        {
            // Lấy userId từ JWT token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            // Chuyển đổi userId từ token sang kiểu số nguyên
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("UserId không hợp lệ.");
            }

            // Kiểm tra xem userId có tồn tại trong bảng Users không
            var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                return BadRequest(new { message = "User không tồn tại." });
            }

            // Kiểm tra lý do báo cáo
            if (string.IsNullOrWhiteSpace(reportDto.ReportReason))
            {
                return BadRequest(new { message = "Report reason is required." });
            }

            // Tạo báo cáo mới
            var newReport = new Report
            {
                UserId = userId, // Sử dụng userId từ token
                FlowerId = reportDto.FlowerId,
                SellerId = reportDto.SellerId,
                ReportReason = reportDto.ReportReason,
                ReportDescription = reportDto.ReportDescription,
                Status = "Pending", // Trạng thái mặc định
                CreatedAt = DateTime.UtcNow // Sử dụng UTC để đồng bộ
            };

            // Lưu báo cáo qua service
            var createdReport = await _reportService.CreateReportAsync(newReport);

            // Trả về phản hồi
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

        //Get all Report
        [HttpGet("GetAllReports")]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportService.GetAllReportsAsync();

            // Ánh xạ các trường cần thiết vào DTO
            var reportDtos = reports.Select(r => new ReportDto
            {
                ReportId = r.ReportId,
                UserId = r.UserId,
                FlowerId = r.FlowerId,
                SellerId = r.SellerId,
                ReportReason = r.ReportReason,
                ReportDescription = r.ReportDescription,
                Status = r.Status ?? "Pending" // Đặt giá trị mặc định là "Pending" nếu Status là null
            }).ToList();

            return Ok(reportDtos);
        }


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

            // If the status is "Resolved", reduce the seller's points by 5 in UserInfo
            if (updateReportStatusDto.Status == "Resolved")
            {
                var seller = await _context.Sellers.Include(s => s.User).ThenInclude(u => u.UserInfos)
                    .FirstOrDefaultAsync(s => s.SellerId == report.SellerId);

                if (seller != null)
                {
                    // Assuming UserInfos contains a property for points, e.g., Points
                    var userInfo = seller.User.UserInfos.FirstOrDefault(); // Adjust this to get the relevant UserInfo if necessary

                    if (userInfo != null)
                    {
                        userInfo.Points -= 5; // Deduct 5 points
                        if (userInfo.Points < 0)
                            userInfo.Points = 0; // Prevent negative points

                        _context.Entry(userInfo).State = EntityState.Modified; // Mark UserInfo as modified
                    }
                    else
                    {
                        return NotFound(new { message = "UserInfo not found for the seller." });
                    }
                }
                else
                {
                    return NotFound(new { message = "Seller not found." });
                }
            }

            // Update the report status
            report.Status = updateReportStatusDto.Status;
            report.UpdatedAt = DateTime.UtcNow; // Use UTC for consistency

            // Save the updated report and the user info
            await _reportService.UpdateReportStatusAsync(report);
            await _context.SaveChangesAsync();

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

