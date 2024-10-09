using MyShop.DataContext;
using MyShop.Entities;


public class CategoryService : ICategoryService 
{
    private readonly FlowershopContext _context;

    public CategoryService(FlowershopContext context)
    {
        _context = context;
    }

    // Method to get flowers by category ID
    public IEnumerable<FlowerInfo> GetFlowersByCategoryId(int categoryId)
    {
        // Query to get flowers by category ID
        return _context.FlowerInfos.Where(f => f.CategoryId == categoryId).ToList();
    }
}
