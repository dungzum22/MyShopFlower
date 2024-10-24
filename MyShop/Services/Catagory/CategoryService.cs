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

    public IEnumerable<FlowerInfo> GetFlowersByCategoryName(string categoryName)
    {
        // Query to get flowers by category ID
        return _context.FlowerInfos.Where(f => f.Category.CategoryName == categoryName).ToList();
    }

    // Method to create a new category
    public async Task<Category> CreateCategoryAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }
}