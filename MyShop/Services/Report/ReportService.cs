using Microsoft.EntityFrameworkCore;
using MyShop.DataContext;
using MyShop.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly FlowershopContext _context;

        public ReportService(FlowershopContext context)
        {
            _context = context;
        }

        // Create a new report
        public async Task<Report> CreateReportAsync(Report report)
        {
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        // Get a report by ID
        public async Task<Report> GetReportByIdAsync(int reportId)
        {
            return await _context.Reports
                .Include(r => r.User)    // Include User details if needed
                .Include(r => r.Seller)  // Include Seller details if needed
                .Include(r => r.Flower)  // Include Flower details if needed
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<IEnumerable<Report>> GetReportsByUserIdAsync(int userId)
        {
            return await _context.Reports
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        // Get all reports
        public async Task<List<Report>> GetAllReportsAsync()
        {
            return await _context.Reports
                .Include(r => r.User)
                .Include(r => r.Seller)
                .Include(r => r.Flower)
                .ToListAsync();
        }

        // Update the report's status
        public async Task UpdateReportStatusAsync(Report report)
        {
            _context.Reports.Update(report);
            await _context.SaveChangesAsync();
        }
    }
}
