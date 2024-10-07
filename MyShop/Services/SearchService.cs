using MyShop.DataContext;
using MyShop.Entities;

namespace MyShop.Services
{
    public class SearchService
    {
        private readonly FlowershopContext _context;

        public SearchService(FlowershopContext context)
        {
            _context = context;
        }

        public IEnumerable<FlowerInfo> SearchFlowers(string searchQuery)
        {
            // Query to search flowers by name or description
            return _context.FlowerInfos.Where(f => f.FlowerName.Contains(searchQuery) || f.FlowerDescription.Contains(searchQuery)).ToList();
        }
    }
}
