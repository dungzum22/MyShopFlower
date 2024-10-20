using MyShop.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShop.Services.Reports
{
    public interface IReportService
    {
        Task<Report> CreateReportAsync(Report report);
        Task<Report> GetReportByIdAsync(int reportId);
        Task<List<Report>> GetAllReportsAsync();
        Task<IEnumerable<Report>> GetReportsByUserIdAsync(int userId);

        Task UpdateReportStatusAsync(Report report);
    }
}
